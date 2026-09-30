using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MimoShop.Models;

namespace MimoShop.Services;

// CartService stores a List<CartItem> in server-side session as JSON.
// Cart is anonymous and lost on session expiry — acceptable for the V1
// COD storefront per SHOP.md §6.1.

public sealed class CartService
{
    private const string SessionKey = "MimoShop.Cart";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<CartItem>> GetCartAsync(ISession session)
    {
        string? json = session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            List<CartItem>? items = JsonSerializer.Deserialize<List<CartItem>>(json, JsonOptions);
            return items ?? [];
        }
        catch
        {
            // Corrupt cart — reset to keep checkout working.
            session.Remove(SessionKey);
            return [];
        }
    }

    public async Task AddItemAsync(ISession session, CartItem item)
    {
        List<CartItem> items = await GetCartAsync(session);
        CartItem? existing = items.FirstOrDefault(i => i.InventoryPartId == item.InventoryPartId);
        if (existing is not null)
        {
            int index = items.IndexOf(existing);
            items[index] = existing with { Quantity = existing.Quantity + item.Quantity };
        }
        else
        {
            items.Add(item);
        }

        await SaveCartAsync(session, items);
    }

    public async Task UpdateQuantityAsync(ISession session, int inventoryPartId, int quantity)
    {
        List<CartItem> items = await GetCartAsync(session);
        if (quantity <= 0)
        {
            items.RemoveAll(i => i.InventoryPartId == inventoryPartId);
        }
        else
        {
            CartItem? existing = items.FirstOrDefault(i => i.InventoryPartId == inventoryPartId);
            if (existing is not null)
            {
                int index = items.IndexOf(existing);
                items[index] = existing with { Quantity = quantity };
            }
        }

        await SaveCartAsync(session, items);
    }

    public async Task RemoveItemAsync(ISession session, int inventoryPartId)
    {
        List<CartItem> items = await GetCartAsync(session);
        items.RemoveAll(i => i.InventoryPartId == inventoryPartId);
        await SaveCartAsync(session, items);
    }

    public async Task ClearCartAsync(ISession session)
    {
        session.Remove(SessionKey);
        await Task.CompletedTask;
    }

    public async Task<CartSummary> GetCartSummaryAsync(ISession session)
    {
        List<CartItem> items = await GetCartAsync(session);
        return new CartSummary
        {
            ItemCount = items.Sum(i => i.Quantity),
            Subtotal = items.Sum(i => i.UnitPrice * i.Quantity)
        };
    }

    private static Task SaveCartAsync(ISession session, List<CartItem> items)
    {
        string json = JsonSerializer.Serialize(items, JsonOptions);
        session.SetString(SessionKey, json);
        return Task.CompletedTask;
    }
}

public sealed class CartSummary
{
    public int ItemCount { get; set; }
    public decimal Subtotal { get; set; }
}