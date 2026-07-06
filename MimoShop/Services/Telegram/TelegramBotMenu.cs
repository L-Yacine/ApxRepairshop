using Telegram.Bot.Types.ReplyMarkups;

namespace MimoShop.Services.Telegram;

public static class TelegramBotCallback
{
    public const string Main = "main";
    public const string StatusPrompt = "status-prompt";
    public const string StatusCancel = "status-cancel";
    public const string StatusTryAgain = "status-try-again";
    public const string Contact = "contact";

    public const string BrandsPrefix = "brands";
    public const string BrandPrefix = "brand";

    public const string ModelsPrefix = "models";
    public const string ModelPrefix = "model";

    public const string TypesPrefix = "types";
    public const string TypePrefix = "type";

    public const string VariantsPrefix = "variants";

    public static string Brands(int page) => $"{BrandsPrefix}:{page}";
    public static string Brand(int brandId) => $"{BrandPrefix}:{brandId}";

    public static string Models(int brandId, int page) => $"{ModelsPrefix}:{brandId}:{page}";
    public static string Model(int brandId, int modelId) => $"{ModelPrefix}:{brandId}:{modelId}";

    public static string Types(int brandId, int modelId, int page) => $"{TypesPrefix}:{brandId}:{modelId}:{page}";
    public static string Type(int brandId, int modelId, int partTypeId) => $"{TypePrefix}:{brandId}:{modelId}:{partTypeId}";

    public static string Variants(int brandId, int modelId, int partTypeId, int page) => $"{VariantsPrefix}:{brandId}:{modelId}:{partTypeId}:{page}";
}

public static class TelegramBotMenuBuilder
{
    public static InlineKeyboardMarkup MainMenu()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(TelegramBotText.BrowseParts, TelegramBotCallback.Brands(1))
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(TelegramBotText.CheckRepairStatus, TelegramBotCallback.StatusPrompt)
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(TelegramBotText.Contact, TelegramBotCallback.Contact)
            }
        });
    }

    public static InlineKeyboardMarkup StatusPrompt()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(TelegramBotText.StatusCancel, TelegramBotCallback.StatusCancel)
            }
        });
    }

    public static InlineKeyboardMarkup StatusResult()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(TelegramBotText.StatusTryAgain, TelegramBotCallback.StatusTryAgain)
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(TelegramBotText.Home, TelegramBotCallback.Main)
            }
        });
    }

    public static InlineKeyboardMarkup BrandsPage(PaginatedResult<LookupItem> page)
    {
        var rows = page.Items
            .Select(item => new[]
            {
                InlineKeyboardButton.WithCallbackData(item.Name, TelegramBotCallback.Brand(item.Id))
            })
            .ToList();

        AppendPagination(rows, page.HasPrevious, page.HasNext,
            prev: TelegramBotCallback.Brands(page.Page - 1),
            next: TelegramBotCallback.Brands(page.Page + 1));
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(TelegramBotText.Home, TelegramBotCallback.Main)
        });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup ModelsPage(int brandId, PaginatedResult<ModelLookupItem> page)
    {
        var rows = page.Items
            .Select(item => new[]
            {
                InlineKeyboardButton.WithCallbackData(item.Name, TelegramBotCallback.Model(brandId, item.Id))
            })
            .ToList();

        AppendPagination(rows, page.HasPrevious, page.HasNext,
            prev: TelegramBotCallback.Models(brandId, page.Page - 1),
            next: TelegramBotCallback.Models(brandId, page.Page + 1));
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(TelegramBotText.BackToBrands, TelegramBotCallback.Brands(1))
        });
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(TelegramBotText.Home, TelegramBotCallback.Main)
        });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup PartTypesPage(int brandId, int modelId, PaginatedResult<PartTypeLookupItem> page)
    {
        var rows = page.Items
            .Select(item => new[]
            {
                InlineKeyboardButton.WithCallbackData(item.Name, TelegramBotCallback.Type(brandId, modelId, item.Id))
            })
            .ToList();

        AppendPagination(rows, page.HasPrevious, page.HasNext,
            prev: TelegramBotCallback.Types(brandId, modelId, page.Page - 1),
            next: TelegramBotCallback.Types(brandId, modelId, page.Page + 1));
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(TelegramBotText.BackToModels, TelegramBotCallback.Models(brandId, 1))
        });
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(TelegramBotText.Home, TelegramBotCallback.Main)
        });
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup VariantsPage(int brandId, int modelId, int partTypeId, PaginatedResult<VariantItem> page)
    {
        var rows = new List<InlineKeyboardButton[]>();

        AppendPagination(rows, page.HasPrevious, page.HasNext,
            prev: TelegramBotCallback.Variants(brandId, modelId, partTypeId, page.Page - 1),
            next: TelegramBotCallback.Variants(brandId, modelId, partTypeId, page.Page + 1));

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(TelegramBotText.BackToTypes, TelegramBotCallback.Types(brandId, modelId, 1))
        });
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(TelegramBotText.Home, TelegramBotCallback.Main)
        });
        return new InlineKeyboardMarkup(rows);
    }

    private static void AppendPagination(
        List<InlineKeyboardButton[]> rows,
        bool hasPrevious,
        bool hasNext,
        string prev,
        string next)
    {
        if (!hasPrevious && !hasNext)
        {
            return;
        }

        var nav = new List<InlineKeyboardButton>();
        if (hasPrevious)
        {
            nav.Add(InlineKeyboardButton.WithCallbackData(TelegramBotText.Previous, prev));
        }
        if (hasNext)
        {
            nav.Add(InlineKeyboardButton.WithCallbackData(TelegramBotText.Next, next));
        }
        rows.Add(nav.ToArray());
    }
}