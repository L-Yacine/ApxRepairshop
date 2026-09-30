using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using MimoShop.Data;
using MimoShop.Localization;
using MimoShop.Models;
using MimoShop.Resources;

namespace MimoShop.Services;

public sealed class ShopOrderService
{
    private readonly MimoShopDbContext dbContext;
    private readonly CartService cart;
    private readonly IStringLocalizer<PublicResources> localizer;

    public ShopOrderService(
        MimoShopDbContext dbContext,
        CartService cart,
        IStringLocalizer<PublicResources> localizer)
    {
        this.dbContext = dbContext;
        this.cart = cart;
        this.localizer = localizer;
    }

    public async Task<CheckoutPageViewModel> BuildCheckoutPageAsync(ISession session)
    {
        List<CartItem> items = await cart.GetCartAsync(session);
        CartSummary summary = await cart.GetCartSummaryAsync(session);
        IReadOnlyList<WilayaOption> wilayas = await new DeliveryZoneService(dbContext).GetActiveWilayasAsync();

        return new CheckoutPageViewModel
        {
            Items = items,
            Subtotal = summary.Subtotal,
            ItemCount = summary.ItemCount,
            Wilayas = wilayas,
            Form = new CheckoutFormInput()
        };
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(ISession session, CheckoutFormInput input)
    {
        List<CartItem> cartItems = await cart.GetCartAsync(session);
        if (cartItems.Count == 0)
        {
            return PlaceOrderResult.Fail(localizer["ShopOrder.CartEmpty"].Value);
        }

        WilayaOption? wilaya = await new DeliveryZoneService(dbContext).GetActiveWilayaAsync(input.WilayaId);
        if (wilaya is null)
        {
            return PlaceOrderResult.Fail(localizer["QuickOrder.WilayaUnavailable"].Value);
        }

        CommuneOption? commune = (await new DeliveryZoneService(dbContext).GetActiveCommunesAsync(input.WilayaId))
            .FirstOrDefault(c => c.Id == input.CommuneId);
        if (commune is null)
        {
            return PlaceOrderResult.Fail(localizer["QuickOrder.CommuneUnavailable"].Value);
        }

        // Re-validate every cart line against current inventory.
        var validationErrors = new List<string>();
        var lines = new List<ShopOrderLine>();
        decimal subtotal = 0m;

        foreach (CartItem item in cartItems)
        {
            InventoryPart? part = await dbContext.InventoryParts
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.PhoneModel)
                .Include(p => p.PartType)
                .Include(p => p.PartVariant)
                .SingleOrDefaultAsync(p => p.Id == item.InventoryPartId);

            if (part is null || !part.IsStocked || part.Quantity < item.Quantity)
            {
                validationErrors.Add(localizer["ShopOrder.PartUnavailable", item.PartDisplayName].Value);
                continue;
            }

            string displayName = PublicCulture.IsArabic
                ? $"{part.Brand.DisplayNameAr} {part.PhoneModel.DisplayNameAr} — {part.PartType.DisplayNameAr} — {part.PartVariant.DisplayNameAr}"
                : $"{part.Brand.Name} {part.PhoneModel.Name} — {part.PartType.Name} — {part.PartVariant.Name}";
            string thumb = !string.IsNullOrWhiteSpace(part.ThumbnailUrl) ? part.ThumbnailUrl :
                           !string.IsNullOrWhiteSpace(part.ImageUrl) ? part.ImageUrl : item.ThumbnailUrl;

            var line = new ShopOrderLine
            {
                InventoryPartId = part.Id,
                BrandName = part.Brand.Name,
                ModelName = part.PhoneModel.Name,
                PartTypeName = part.PartType.Name,
                VariantName = part.PartVariant.Name,
                PartDisplayName = displayName,
                Quantity = item.Quantity,
                UnitPrice = part.UnitSalePrice,
                ImageUrl = thumb
            };
            lines.Add(line);
            subtotal += line.UnitPrice * line.Quantity;
        }

        if (validationErrors.Count > 0)
        {
            return PlaceOrderResult.Fail(string.Join(" ", validationErrors.Distinct()));
        }

        decimal shippingFee = wilaya.ShippingFee;
        decimal total = subtotal + shippingFee;

        var order = new ShopOrder
        {
            OrderCode = await GenerateOrderCodeAsync(),
            CustomerName = input.CustomerName.Trim(),
            CustomerPhone = input.CustomerPhone.Trim(),
            CustomerWhatsApp = string.IsNullOrWhiteSpace(input.CustomerWhatsApp) ? null : input.CustomerWhatsApp.Trim(),
            WilayaId = wilaya.Id,
            CommuneId = commune.Id,
            WilayaName = $"{wilaya.Code} - {wilaya.NameAr}",
            CommuneName = commune.NameAr,
            CommuneNameFr = commune.NameFr,
            Address = input.Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            Status = ShopOrderStatuses.New,
            Subtotal = subtotal,
            ShippingFee = shippingFee,
            TotalAmount = total,
            CreatedAt = DateTime.Now,
            Lines = lines
        };

        dbContext.ShopOrders.Add(order);
        await dbContext.SaveChangesAsync();

        await cart.ClearCartAsync(session);

        return PlaceOrderResult.Success(order.OrderCode);
    }

    public async Task<ShopOrder?> FindByCodeAsync(string orderCode)
    {
        return await dbContext.ShopOrders
            .AsNoTracking()
            .Include(o => o.Lines)
            .ThenInclude(l => l.InventoryPart)
            .SingleOrDefaultAsync(o => o.OrderCode == orderCode);
    }

    public async Task<IReadOnlyList<ShopOrderSummary>> ListOrdersAsync(string? statusFilter)
    {
        IQueryable<ShopOrder> query = dbContext.ShopOrders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt);

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        return await query
            .Select(o => new ShopOrderSummary
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                WilayaName = o.WilayaName,
                CommuneName = o.CommuneName,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                LineCount = o.Lines.Count
            })
            .ToListAsync();
    }

    public async Task<ShopOrder?> FindForProcessingAsync(int id)
    {
        return await dbContext.ShopOrders
            .Include(o => o.Lines)
            .ThenInclude(l => l.InventoryPart)
            .SingleOrDefaultAsync(o => o.Id == id);
    }

    public async Task<int> CountNewOrdersAsync()
    {
        return await dbContext.ShopOrders.CountAsync(o => o.Status == ShopOrderStatuses.New);
    }

    public async Task<(bool Ok, string? Error)> ConfirmAsync(int id, string username)
    {
        ShopOrder? order = await FindForProcessingAsync(id);
        if (order is null) { return (false, "الطلب غير موجود."); }
        if (order.Status != ShopOrderStatuses.New)
        {
            return (false, "لا يمكن تأكيد هذا الطلب؛ حالته الحالية لا تسمح بذلك.");
        }

        // Re-check stock availability for every line before decrementing.
        var shortParts = new List<string>();
        foreach (ShopOrderLine line in order.Lines)
        {
            InventoryPart? part = line.InventoryPartId is null ? null :
                await dbContext.InventoryParts.SingleOrDefaultAsync(p => p.Id == line.InventoryPartId);
            if (part is null || !part.IsStocked || part.Quantity < line.Quantity)
            {
                shortParts.Add($"{line.PartDisplayName} — متوفر: {(part?.Quantity ?? 0)} مطلوب: {line.Quantity}");
            }
        }
        if (shortParts.Count > 0)
        {
            return (false, "نقص في المخزون: " + string.Join(" | ", shortParts));
        }

        DateTime now = DateTime.Now;
        foreach (ShopOrderLine line in order.Lines)
        {
            InventoryPart? part = line.InventoryPartId is null ? null :
                await dbContext.InventoryParts.SingleOrDefaultAsync(p => p.Id == line.InventoryPartId);
            if (part is null) { continue; }

            part.Quantity -= line.Quantity;
            part.UpdatedAt = now;

            dbContext.InventoryStockMovements.Add(new InventoryStockMovement
            {
                InventoryPartId = part.Id,
                QuantityChange = -line.Quantity,
                MovementType = InventoryMovementTypes.ShopOrder,
                OccurredAt = now,
                CreatedByUsername = username
            });
        }

        order.Status = ShopOrderStatuses.Confirmed;
        order.ConfirmedAt = now;
        await dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ShipAsync(int id, string username)
    {
        ShopOrder? order = await FindForProcessingAsync(id);
        if (order is null) { return (false, "الطلب غير موجود."); }
        if (order.Status != ShopOrderStatuses.Confirmed)
        {
            return (false, "لا يمكن شحن هذا الطلب؛ يتم الشحن بعد التأكيد فقط.");
        }
        order.Status = ShopOrderStatuses.Shipped;
        order.ShippedAt = DateTime.Now;
        await dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeliverAsync(int id, string username)
    {
        ShopOrder? order = await FindForProcessingAsync(id);
        if (order is null) { return (false, "الطلب غير موجود."); }
        if (order.Status != ShopOrderStatuses.Shipped)
        {
            return (false, "لا يمكن تسليم هذا الطلب؛ يتم التسليم بعد الشحن فقط.");
        }
        order.Status = ShopOrderStatuses.Delivered;
        order.DeliveredAt = DateTime.Now;
        await dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReturnAsync(int id, string reason, string username)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return (false, "سبب الإرجاع مطلوب.");
        }
        ShopOrder? order = await FindForProcessingAsync(id);
        if (order is null) { return (false, "الطلب غير موجود."); }
        if (order.Status != ShopOrderStatuses.Shipped)
        {
            return (false, "لا يمكن إرجاع هذا الطلب؛ يتم الإرجاع بعد الشحن فقط.");
        }

        DateTime now = DateTime.Now;
        foreach (ShopOrderLine line in order.Lines)
        {
            InventoryPart? part = line.InventoryPartId is null ? null :
                await dbContext.InventoryParts.SingleOrDefaultAsync(p => p.Id == line.InventoryPartId);
            if (part is null) { continue; }

            part.Quantity += line.Quantity;
            part.UpdatedAt = now;

            dbContext.InventoryStockMovements.Add(new InventoryStockMovement
            {
                InventoryPartId = part.Id,
                QuantityChange = line.Quantity,
                MovementType = InventoryMovementTypes.ShopOrderReturn,
                OccurredAt = now,
                CreatedByUsername = username
            });
        }

        order.Status = ShopOrderStatuses.Returned;
        order.ReturnedAt = now;
        order.ReturnReason = reason.Trim();
        await dbContext.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(int id, string? reason, string username)
    {
        ShopOrder? order = await FindForProcessingAsync(id);
        if (order is null) { return (false, "الطلب غير موجود."); }

        bool wasConfirmed = order.Status == ShopOrderStatuses.Confirmed;
        if (order.Status != ShopOrderStatuses.New && !wasConfirmed)
        {
            return (false, "لا يمكن إلغاء هذا الطلب في حالته الحالية.");
        }

        if (wasConfirmed && string.IsNullOrWhiteSpace(reason))
        {
            return (false, "سبب الإلغاء مطلوب عند إلغاء طلب مؤكد.");
        }

        DateTime now = DateTime.Now;
        if (wasConfirmed)
        {
            foreach (ShopOrderLine line in order.Lines)
            {
                InventoryPart? part = line.InventoryPartId is null ? null :
                    await dbContext.InventoryParts.SingleOrDefaultAsync(p => p.Id == line.InventoryPartId);
                if (part is null) { continue; }

                part.Quantity += line.Quantity;
                part.UpdatedAt = now;

                dbContext.InventoryStockMovements.Add(new InventoryStockMovement
                {
                    InventoryPartId = part.Id,
                    QuantityChange = line.Quantity,
                    MovementType = InventoryMovementTypes.ShopOrderCancel,
                    OccurredAt = now,
                    CreatedByUsername = username
                });
            }
        }

        order.Status = ShopOrderStatuses.Cancelled;
        order.CancelledAt = now;
        order.CancelledReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await dbContext.SaveChangesAsync();
        return (true, null);
    }

    // ORD-XXXX where XXXX is the next 4-digit sequence derived from the max existing id.
    private async Task<string> GenerateOrderCodeAsync()
    {
        int maxId = 0;
        if (await dbContext.ShopOrders.AnyAsync())
        {
            maxId = await dbContext.ShopOrders.MaxAsync(o => o.Id);
        }
        // Probe the next sequential id assuming identity gap of 1; if a code collision
        // is found, bump until a free code is available. Id is identity-assigned on
        // SaveChanges, so we generate from the last existing id + 1 with retry.
        int nextId = maxId + 1;
        string candidate = $"ORD-{nextId:D4}";
        while (await dbContext.ShopOrders.AnyAsync(o => o.OrderCode == candidate) && nextId < 99999)
        {
            nextId++;
            candidate = $"ORD-{nextId:D4}";
        }
        return candidate;
    }
}

public sealed class CheckoutFormInput
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerWhatsApp { get; set; }
    public int WilayaId { get; set; }
    public int CommuneId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class CheckoutPageViewModel
{
    public IReadOnlyList<CartItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public int ItemCount { get; set; }
    public IReadOnlyList<WilayaOption> Wilayas { get; set; } = [];
    public CheckoutFormInput Form { get; set; } = new();
}

public sealed class PlaceOrderResult
{
    public bool Ok { get; init; }
    public string? OrderCode { get; init; }
    public string? Error { get; init; }

    public static PlaceOrderResult Success(string code) => new() { Ok = true, OrderCode = code };
    public static PlaceOrderResult Fail(string error) => new() { Ok = false, Error = error };
}

public sealed class OrderConfirmationViewModel
{
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string WilayaName { get; set; } = string.Empty;
    public string CommuneName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public IReadOnlyList<ShopOrderLine> Lines { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public sealed class ShopOrderSummary
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string WilayaName { get; set; } = string.Empty;
    public string CommuneName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = ShopOrderStatuses.New;
    public DateTime CreatedAt { get; set; }
    public int LineCount { get; set; }
}

public sealed class ShopOrdersIndexViewModel
{
    public IReadOnlyList<ShopOrderSummary> Orders { get; set; } = [];
    public string StatusFilter { get; set; } = ShopOrderStatuses.New;
    public int NewCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int ShippedCount { get; set; }
    public int DeliveredCount { get; set; }
    public int ReturnedCount { get; set; }
    public int CancelledCount { get; set; }
}

public sealed class ShopOrderDetailViewModel
{
    public ShopOrder Order { get; set; } = null!;
    public bool CanConfirm { get; set; }
    public bool CanShip { get; set; }
    public bool CanDeliver { get; set; }
    public bool CanReturn { get; set; }
    public bool CanCancel { get; set; }
}