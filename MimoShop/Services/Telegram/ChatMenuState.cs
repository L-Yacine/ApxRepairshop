using System.Collections.Concurrent;

namespace MimoShop.Services.Telegram;

public sealed class ChatMenuState
{
    private readonly ConcurrentDictionary<long, int> activeMenuMessageId = new();
    private readonly ConcurrentDictionary<long, SemaphoreSlim> chatLocks = new();
    private readonly ConcurrentDictionary<long, bool> strayHinted = new();

    public void Set(long chatId, int messageId)
    {
        activeMenuMessageId[chatId] = messageId;
    }

    public bool TryGet(long chatId, out int messageId)
    {
        return activeMenuMessageId.TryGetValue(chatId, out messageId);
    }

    public void Clear(long chatId)
    {
        activeMenuMessageId.TryRemove(chatId, out _);
        strayHinted.TryRemove(chatId, out _);
    }

    public bool IsHinted(long chatId)
    {
        return strayHinted.TryGetValue(chatId, out bool value) && value;
    }

    public void MarkHinted(long chatId)
    {
        strayHinted[chatId] = true;
    }

    public void ClearHint(long chatId)
    {
        strayHinted.TryRemove(chatId, out _);
    }

    public SemaphoreSlim GetLock(long chatId)
    {
        return chatLocks.GetOrAdd(chatId, _ => new SemaphoreSlim(1, 1));
    }
}