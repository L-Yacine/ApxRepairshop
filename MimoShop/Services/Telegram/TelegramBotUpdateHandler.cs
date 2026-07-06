using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MimoShop.Models;
using MimoShop.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MimoShop.Services.Telegram;

public sealed class TelegramBotUpdateHandler
{
    private readonly PartsCatalogQueryService catalog;
    private readonly RepairStatusQueryService status;
    private readonly RepairStatusInputState statusInput;
    private readonly ChatMenuState chatMenu;
    private readonly ShopSettingsService shopSettings;
    private readonly IWebHostEnvironment environment;
    private readonly ILogger<TelegramBotUpdateHandler> logger;

    public TelegramBotUpdateHandler(
        PartsCatalogQueryService catalog,
        RepairStatusQueryService status,
        RepairStatusInputState statusInput,
        ChatMenuState chatMenu,
        ShopSettingsService shopSettings,
        IWebHostEnvironment environment,
        ILogger<TelegramBotUpdateHandler> logger)
    {
        this.catalog = catalog;
        this.status = status;
        this.statusInput = statusInput;
        this.chatMenu = chatMenu;
        this.shopSettings = shopSettings;
        this.environment = environment;
        this.logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        this.botClient = botClient;
        long chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? 0;
        SemaphoreSlim? lockHandle = chatId != 0 ? chatMenu.GetLock(chatId) : null;
        bool acquired = false;

        if (lockHandle is not null)
        {
            try
            {
                await lockHandle.WaitAsync(cancellationToken);
                acquired = true;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        try
        {
            switch (update.Type)
            {
                case UpdateType.Message when update.Message is { } message:
                    await HandleMessageAsync(message, cancellationToken);
                    break;
                case UpdateType.CallbackQuery when update.CallbackQuery is { } callback:
                    await HandleCallbackAsync(callback, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle update {UpdateId}", update.Id);
        }
        finally
        {
            if (acquired)
            {
                lockHandle!.Release();
            }
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ApiRequestException apiEx)
        {
            logger.LogError("Telegram API error: {ErrorCode} {Message}", apiEx.ErrorCode, apiEx.Message);
        }
        else
        {
            logger.LogError(exception, "Telegram bot polling error");
        }
        return Task.CompletedTask;
    }

    private ITelegramBotClient botClient = null!;

    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Type != MessageType.Text || message.Text is null)
        {
            return;
        }

        if (string.Equals(message.Text.Trim(), "/start", StringComparison.OrdinalIgnoreCase))
        {
            statusInput.Clear(message.Chat.Id);
            chatMenu.ClearHint(message.Chat.Id);
            await DeleteActiveMenuAsync(message.Chat.Id, cancellationToken);
            await SendMainMenuAsync(message.Chat.Id, cancellationToken);
            return;
        }

        if (statusInput.IsPending(message.Chat.Id))
        {
            chatMenu.ClearHint(message.Chat.Id);
            await HandleStatusInputAsync(message.Chat.Id, message.MessageId, message.Text, cancellationToken);
            return;
        }

        await TryDeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken);

        if (!chatMenu.IsHinted(message.Chat.Id))
        {
            try
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: TelegramBotText.UseButtonsHint,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
                chatMenu.MarkHinted(message.Chat.Id);
            }
            catch (ApiRequestException ex)
            {
                logger.LogDebug(ex, "Could not send stray-text hint to chat {ChatId}", message.Chat.Id);
            }
        }
    }

    private async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken)
    {
        if (callback.Data is null || callback.Message is null)
        {
            await AnswerAsync(callback.Id, TelegramBotText.PleaseUseButtons, showAlert: false, cancellationToken);
            return;
        }

        long chatId = callback.Message.Chat.Id;
        int messageId = callback.Message.MessageId;
        string data = callback.Data;
        string shopName = await GetShopNameAsync(cancellationToken);
        string shopPhone = await GetShopPhoneAsync(cancellationToken);

        try
        {
            chatMenu.ClearHint(chatId);

            if (data == TelegramBotCallback.Main)
            {
                statusInput.Clear(chatId);
                await SendMainMenuAsync(chatId, cancellationToken, messageId);
            }
            else if (data == TelegramBotCallback.StatusPrompt)
            {
                await SendStatusPromptAsync(chatId, cancellationToken, messageId);
            }
            else if (data == TelegramBotCallback.StatusCancel)
            {
                statusInput.Clear(chatId);
                await SendMainMenuAsync(chatId, cancellationToken, messageId);
            }
            else if (data == TelegramBotCallback.StatusTryAgain)
            {
                await SendStatusPromptAsync(chatId, cancellationToken, messageId);
            }
            else if (data == TelegramBotCallback.Contact)
            {
                string contactText = await BuildContactTextAsync(cancellationToken);
                await AnswerAsync(callback.Id, contactText, showAlert: true, cancellationToken);
                return;
            }
            else if (data.StartsWith(TelegramBotCallback.BrandsPrefix + ":", StringComparison.Ordinal))
            {
                int page = ParsePage(data, TelegramBotCallback.BrandsPrefix);
                await SendBrandsAsync(chatId, page, shopName, cancellationToken, messageId);
            }
            else if (data.StartsWith(TelegramBotCallback.BrandPrefix + ":", StringComparison.Ordinal))
            {
                int brandId = ParseId(data, TelegramBotCallback.BrandPrefix);
                await SendModelsAsync(chatId, brandId, page: 1, shopName, cancellationToken, messageId);
            }
            else if (data.StartsWith(TelegramBotCallback.ModelsPrefix + ":", StringComparison.Ordinal))
            {
                (int brandId, int page) = ParseTwoIds(data, TelegramBotCallback.ModelsPrefix);
                await SendModelsAsync(chatId, brandId, page, shopName, cancellationToken, messageId);
            }
            else if (data.StartsWith(TelegramBotCallback.ModelPrefix + ":", StringComparison.Ordinal))
            {
                (int brandId, int modelId) = ParseTwoIds(data, TelegramBotCallback.ModelPrefix);
                await SendPartTypesAsync(chatId, brandId, modelId, page: 1, shopName, cancellationToken, messageId);
            }
            else if (data.StartsWith(TelegramBotCallback.TypesPrefix + ":", StringComparison.Ordinal))
            {
                (int brandId, int modelId, int page) = ParseThreePlus(data, TelegramBotCallback.TypesPrefix);
                await SendPartTypesAsync(chatId, brandId, modelId, page, shopName, cancellationToken, messageId);
            }
            else if (data.StartsWith(TelegramBotCallback.TypePrefix + ":", StringComparison.Ordinal))
            {
                (int brandId, int modelId, int partTypeId) = ParseThreePlus(data, TelegramBotCallback.TypePrefix);
                await SendVariantsAsync(chatId, brandId, modelId, partTypeId, page: 1, shopName, shopPhone, cancellationToken, messageId);
            }
            else if (data.StartsWith(TelegramBotCallback.VariantsPrefix + ":", StringComparison.Ordinal))
            {
                (int brandId, int modelId, int partTypeId, int page) = ParseFour(data, TelegramBotCallback.VariantsPrefix);
                await SendVariantsAsync(chatId, brandId, modelId, partTypeId, page, shopName, shopPhone, cancellationToken, messageId);
            }
            else
            {
                await AnswerAsync(callback.Id, TelegramBotText.PleaseUseButtons, showAlert: false, cancellationToken);
                return;
            }

            await AnswerAsync(callback.Id, string.Empty, showAlert: false, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle callback {Data}", data);
            await AnswerAsync(callback.Id, TelegramBotText.PleaseUseButtons, showAlert: false, cancellationToken);
        }
    }

    private async Task SendMainMenuAsync(long chatId, CancellationToken cancellationToken, int? replaceMessageId = null)
    {
        string shopName = await GetShopNameAsync(cancellationToken);
        string text = string.Format(TelegramBotText.WelcomeHeroFormat, WebUtility.HtmlEncode(shopName));
        Resolution? logo = await ResolveShopLogoAsync(cancellationToken);
        await SendMenuAsync(
            chatId,
            text,
            TelegramBotMenuBuilder.MainMenu(),
            logo,
            replaceMessageId,
            cancellationToken);
    }

    private async Task<Resolution?> ResolveShopLogoAsync(CancellationToken cancellationToken)
    {
        string? logoUrl;
        try
        {
            ShopSetting settings = await shopSettings.GetSettingsAsync();
            logoUrl = settings.LogoUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load shop settings for main menu logo.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(logoUrl) || !logoUrl.StartsWith("/images/", StringComparison.Ordinal))
        {
            return null;
        }

        string relative = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.Combine(environment.WebRootPath, relative);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            FileStream stream = File.OpenRead(fullPath);
            string fileName = Path.GetFileName(fullPath);
            return new Resolution(InputFile.FromStream(stream, fileName), stream);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to open shop logo {Path}", fullPath);
            return null;
        }
    }

    private async Task SendBrandsAsync(long chatId, int page, string shopName, CancellationToken cancellationToken, int? replaceMessageId = null)
    {
        PaginatedResult<LookupItem> result = await catalog.GetBrandsWithStockAsync(page);

        string text;
        if (result.Items.Count == 0)
        {
            text = TelegramBotText.NoStockedParts;
        }
        else
        {
            text = $"{BreadcrumbPrefix(shopName)}{TelegramBotText.BrandsTitle}\n\n{TelegramBotText.BrandsHeader}";
            if (result.TotalPages > 1)
            {
                text += $" — صفحة {result.Page} من {result.TotalPages}";
            }
            text += "\n" + TelegramBotText.Separator;
        }

        await SendMenuAsync(
            chatId,
            text,
            TelegramBotMenuBuilder.BrandsPage(result),
            photo: null,
            replaceMessageId,
            cancellationToken);
    }

    private async Task SendModelsAsync(long chatId, int brandId, int page, string shopName, CancellationToken cancellationToken, int? replaceMessageId = null)
    {
        PaginatedResult<ModelLookupItem> result = await catalog.GetModelsWithStockAsync(brandId, page);

        string text;
        if (result.Items.Count == 0)
        {
            text = TelegramBotText.NoStockedParts;
        }
        else
        {
            ModelLookupItem first = result.Items[0];
            text = $"{BreadcrumbPrefix(shopName)}{WebUtility.HtmlEncode(first.BrandName)}\n\n{TelegramBotText.ModelsHeader}";
            if (result.TotalPages > 1)
            {
                text += $" — صفحة {result.Page} من {result.TotalPages}";
            }
            text += "\n" + TelegramBotText.Separator;
        }

        await SendMenuAsync(
            chatId,
            text,
            TelegramBotMenuBuilder.ModelsPage(brandId, result),
            null,
            replaceMessageId,
            cancellationToken);
    }

    private async Task SendPartTypesAsync(long chatId, int brandId, int modelId, int page, string shopName, CancellationToken cancellationToken, int? replaceMessageId = null)
    {
        PaginatedResult<PartTypeLookupItem> result = await catalog.GetPartTypesWithStockAsync(brandId, modelId, page);

        string text;
        if (result.Items.Count == 0)
        {
            text = TelegramBotText.NoStockedParts;
        }
        else
        {
            PartTypeLookupItem first = result.Items[0];
            text = $"{BreadcrumbPrefix(shopName)}{WebUtility.HtmlEncode(first.BrandName)}{TelegramBotText.BreadcrumbSeparator}{WebUtility.HtmlEncode(first.ModelName)}\n\n{TelegramBotText.PartTypesHeader}";
            if (result.TotalPages > 1)
            {
                text += $" — صفحة {result.Page} من {result.TotalPages}";
            }
            text += "\n" + TelegramBotText.Separator;
        }

        await SendMenuAsync(
            chatId,
            text,
            TelegramBotMenuBuilder.PartTypesPage(brandId, modelId, result),
            null,
            replaceMessageId,
            cancellationToken);
    }

    private async Task SendVariantsAsync(long chatId, int brandId, int modelId, int partTypeId, int page, string shopName, string shopPhone, CancellationToken cancellationToken, int? replaceMessageId = null)
    {
        PaginatedResult<VariantItem> result = await catalog.GetVariantsAsync(brandId, modelId, partTypeId, page);

        string text;
        if (result.Items.Count == 0)
        {
            text = TelegramBotText.NoStockedParts;
        }
        else
        {
            VariantItem first = result.Items[0];
            text = $"{BreadcrumbPrefix(shopName)}{WebUtility.HtmlEncode(first.BrandName)}{TelegramBotText.BreadcrumbSeparator}{WebUtility.HtmlEncode(first.ModelName)}{TelegramBotText.BreadcrumbSeparator}{WebUtility.HtmlEncode(first.PartTypeName)}\n\n{TelegramBotText.VariantsHeader}\n\n";
            foreach (VariantItem variant in result.Items)
            {
                text += string.Format(TelegramBotText.VariantInlineItemFormat, WebUtility.HtmlEncode(variant.Name), variant.SalePrice) + "\n";
            }
            text += "\n" + string.Format(TelegramBotText.PriceNote, WebUtility.HtmlEncode(shopPhone));
            if (result.TotalPages > 1)
            {
                text += $"\n\n— صفحة {result.Page} من {result.TotalPages}";
            }
        }

        await SendMenuAsync(
            chatId,
            text,
            TelegramBotMenuBuilder.VariantsPage(brandId, modelId, partTypeId, result),
            null,
            replaceMessageId,
            cancellationToken);
    }

    private async Task SendStatusPromptAsync(long chatId, CancellationToken cancellationToken, int? replaceMessageId = null)
    {
        statusInput.Clear(chatId);

        Message sent = await SendMenuAsync(
            chatId,
            TelegramBotText.StatusPrompt,
            TelegramBotMenuBuilder.StatusPrompt(),
            photo: null,
            replaceMessageId,
            cancellationToken);

        statusInput.Begin(chatId, sent.MessageId);
    }

    private async Task HandleStatusInputAsync(long chatId, int userMessageId, string rawText, CancellationToken cancellationToken)
    {
        await TryDeleteMessageAsync(chatId, userMessageId, cancellationToken);

        if (!statusInput.TryConsume(chatId, out int promptMessageId))
        {
            return;
        }

        string normalized = RepairStatusQueryService.NormalizeInput(rawText);
        if (!RepairStatusQueryService.IsWellFormed(normalized))
        {
            await EditStatusResultAsync(chatId, promptMessageId, TelegramBotText.StatusFormatError, cancellationToken);
            return;
        }

        RepairStatusSummary? summary;
        try
        {
            summary = await status.FindByCodeAsync(normalized);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to look up repair status for code {Code}", normalized);
            await EditStatusResultAsync(chatId, promptMessageId, TelegramBotText.StatusNotFound, cancellationToken);
            return;
        }

        if (summary is null)
        {
            await EditStatusResultAsync(chatId, promptMessageId, TelegramBotText.StatusNotFound, cancellationToken);
            return;
        }

        string successText = string.Format(
            TelegramBotText.StatusSuccessFormat,
            WebUtility.HtmlEncode(summary.DeviceName),
            WebUtility.HtmlEncode(summary.StatusLabel),
            WebUtility.HtmlEncode(summary.AssignedWorkerName));

        await EditStatusResultAsync(chatId, promptMessageId, successText, cancellationToken);
    }

    private async Task EditStatusResultAsync(long chatId, int promptMessageId, string text, CancellationToken cancellationToken)
    {
        if (promptMessageId > 0)
        {
            try
            {
                await botClient.EditMessageText(
                    chatId: chatId,
                    messageId: promptMessageId,
                    text: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: TelegramBotMenuBuilder.StatusResult(),
                    cancellationToken: cancellationToken);
                chatMenu.Set(chatId, promptMessageId);
                return;
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("not modified", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        Message sent = await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: TelegramBotMenuBuilder.StatusResult(),
            cancellationToken: cancellationToken);
        chatMenu.Set(chatId, sent.MessageId);
    }

    private async Task<Message> SendMenuAsync(
        long chatId,
        string text,
        InlineKeyboardMarkup markup,
        Resolution? photo,
        int? replaceMessageId,
        CancellationToken cancellationToken)
    {
        int? previousMenuId = replaceMessageId;
        if (previousMenuId is null && chatMenu.TryGet(chatId, out int trackedId))
        {
            previousMenuId = trackedId;
        }

        if (previousMenuId is not null)
        {
            await TryDeleteMessageAsync(chatId, previousMenuId.Value, cancellationToken);
        }
        chatMenu.Clear(chatId);

        if (photo is not null)
        {
            try
            {
                Message sent = await botClient.SendPhoto(
                    chatId: chatId,
                    photo: photo.File,
                    caption: text,
                    parseMode: ParseMode.Html,
                    replyMarkup: markup,
                    cancellationToken: cancellationToken);
                chatMenu.Set(chatId, sent.MessageId);
                return sent;
            }
            catch (ApiRequestException ex)
            {
                logger.LogWarning(ex, "Telegram could not send catalog photo, falling back to text.");
            }
            finally
            {
                await photo.Stream.DisposeAsync();
            }
        }

        Message textSent = await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            cancellationToken: cancellationToken);
        chatMenu.Set(chatId, textSent.MessageId);
        return textSent;
    }

    private async Task DeleteActiveMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        if (chatMenu.TryGet(chatId, out int messageId))
        {
            await TryDeleteMessageAsync(chatId, messageId, cancellationToken);
            chatMenu.Clear(chatId);
        }
    }

    private async Task TryDeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.DeleteMessage(chatId, messageId, cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            logger.LogDebug(ex, "Telegram could not delete message {MessageId} in chat {ChatId}", messageId, chatId);
        }
    }

    private async Task AnswerAsync(string callbackQueryId, string text, bool showAlert, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQueryId,
                text: string.IsNullOrEmpty(text) ? null : text,
                showAlert: showAlert,
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            logger.LogDebug(ex, "Telegram could not answer callback query {Id}", callbackQueryId);
        }
    }

    private async Task<string> GetShopNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            ShopSetting settings = await shopSettings.GetSettingsAsync();
            return settings.Name;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load shop settings; falling back to default shop name.");
            return "ميمو شوب";
        }
    }

    private async Task<string> GetShopPhoneAsync(CancellationToken cancellationToken)
    {
        try
        {
            ShopSetting settings = await shopSettings.GetSettingsAsync();
            return settings.Phone;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load shop settings; falling back to default phone.");
            return "—";
        }
    }

    private async Task<string> BuildContactTextAsync(CancellationToken cancellationToken)
    {
        string phone = "—";
        string whatsappLine = string.Empty;
        string address = "—";

        try
        {
            ShopSetting settings = await shopSettings.GetSettingsAsync();
            phone = settings.Phone;
            address = settings.Address;
            if (!string.IsNullOrWhiteSpace(settings.WhatsApp))
            {
                whatsappLine = string.Format(TelegramBotText.ContactWhatsAppLineFormat, settings.WhatsApp);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load shop settings for contact popup.");
        }

        return string.Format(
            TelegramBotText.ContactFormat,
            WebUtility.HtmlEncode(phone),
            whatsappLine,
            WebUtility.HtmlEncode(address));
    }

    private static string BreadcrumbPrefix(string shopName)
    {
        return $"📍 <b>{WebUtility.HtmlEncode(shopName)}</b>{TelegramBotText.BreadcrumbSeparator}";
    }

    private sealed record Resolution(InputFile File, FileStream Stream);

    private static int ParsePage(string data, string prefix)
    {
        string rest = data[(prefix.Length + 1)..];
        return int.TryParse(rest, out int page) && page >= 1 ? page : 1;
    }

    private static int ParseId(string data, string prefix)
    {
        string rest = data[(prefix.Length + 1)..];
        return int.TryParse(rest, out int id) ? id : 0;
    }

    private static (int First, int Second) ParseTwoIds(string data, string prefix)
    {
        string rest = data[(prefix.Length + 1)..];
        string[] parts = rest.Split(':');
        int first = parts.Length > 0 && int.TryParse(parts[0], out int a) ? a : 0;
        int second = parts.Length > 1 && int.TryParse(parts[1], out int b) ? b : 0;
        return (first, second);
    }

    private static (int First, int Second, int Third) ParseThreePlus(string data, string prefix)
    {
        string rest = data[(prefix.Length + 1)..];
        string[] parts = rest.Split(':');
        int first = parts.Length > 0 && int.TryParse(parts[0], out int a) ? a : 0;
        int second = parts.Length > 1 && int.TryParse(parts[1], out int b) ? b : 0;
        int third = parts.Length > 2 && int.TryParse(parts[2], out int c) ? c : 0;
        return (first, second, third);
    }

    private static (int First, int Second, int Third, int Fourth) ParseFour(string data, string prefix)
    {
        string rest = data[(prefix.Length + 1)..];
        string[] parts = rest.Split(':');
        int first = parts.Length > 0 && int.TryParse(parts[0], out int a) ? a : 0;
        int second = parts.Length > 1 && int.TryParse(parts[1], out int b) ? b : 0;
        int third = parts.Length > 2 && int.TryParse(parts[2], out int c) ? c : 0;
        int fourth = parts.Length > 3 && int.TryParse(parts[3], out int d) ? d : 0;
        return (first, second, third, fourth);
    }
}