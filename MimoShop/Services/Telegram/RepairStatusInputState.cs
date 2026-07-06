using System.Collections.Concurrent;

namespace MimoShop.Services.Telegram;

public sealed class RepairStatusInputState
{
    public static readonly TimeSpan EntryTtl = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<long, PendingInput> pending = new();

    public void Begin(long chatId, int promptMessageId)
    {
        Prune();
        pending[chatId] = new PendingInput(DateTime.UtcNow, promptMessageId);
    }

    public bool IsPending(long chatId)
    {
        if (pending.TryGetValue(chatId, out PendingInput input))
        {
            if (DateTime.UtcNow - input.StartedAt <= EntryTtl)
            {
                return true;
            }

            pending.TryRemove(chatId, out _);
        }

        return false;
    }

    public bool TryConsume(long chatId, out int promptMessageId)
    {
        if (pending.TryRemove(chatId, out PendingInput input)
            && DateTime.UtcNow - input.StartedAt <= EntryTtl)
        {
            promptMessageId = input.PromptMessageId;
            return true;
        }

        promptMessageId = 0;
        return false;
    }

    public void Clear(long chatId)
    {
        pending.TryRemove(chatId, out _);
    }

    private void Prune()
    {
        DateTime cutoff = DateTime.UtcNow - EntryTtl;
        foreach (KeyValuePair<long, PendingInput> entry in pending)
        {
            if (entry.Value.StartedAt < cutoff)
            {
                pending.TryRemove(entry.Key, out _);
            }
        }
    }

    private readonly record struct PendingInput(DateTime StartedAt, int PromptMessageId);
}
