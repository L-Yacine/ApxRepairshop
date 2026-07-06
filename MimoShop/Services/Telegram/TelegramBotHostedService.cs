using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MimoShop.Services.Telegram;

public sealed class TelegramBotHostedService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly TelegramBotOptions options;
    private readonly ILogger<TelegramBotHostedService> logger;

    public TelegramBotHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramBotHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options.Value;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.IsConfigured)
        {
            logger.LogWarning(
                "Telegram bot token is not configured. Set Telegram:BotToken in appsettings.json to enable the bot.");
            return;
        }

        ITelegramBotClient client = new TelegramBotClient(options.BotToken);

        try
        {
            User me = await client.GetMe(stoppingToken);
            logger.LogInformation("Telegram bot started: @{Username} ({Id})", me.Username, me.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to call Telegram GetMe. Bot token may be invalid.");
            return;
        }

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            DropPendingUpdates = true
        };

        using IServiceScope scope = scopeFactory.CreateScope();
        TelegramBotUpdateHandler handler = scope.ServiceProvider.GetRequiredService<TelegramBotUpdateHandler>();

        try
        {
            await client.ReceiveAsync(
                updateHandler: handler.HandleUpdateAsync,
                errorHandler: handler.HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Telegram bot polling stopped.");
        }
    }
}
