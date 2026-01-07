using QTS.Edge.TradeManagement.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Services;

/// <summary>
/// Trade Journal - speichert und verwaltet Trade-Historie.
/// </summary>
public sealed class TradeJournal : ITradeJournal
{
    private readonly List<TradeRecord> _trades = new();

    /// <inheritdoc />
    public void RecordTrade(TradeRecord trade)
    {
        _trades.Add(trade);
    }

    /// <inheritdoc />
    public IReadOnlyList<TradeRecord> GetTrades(DateTime? from = null, DateTime? to = null)
    {
        IEnumerable<TradeRecord> result = _trades;

        if (from.HasValue)
        {
            result = result.Where(t => t.ExitTime >= from.Value);
        }

        if (to.HasValue)
        {
            result = result.Where(t => t.ExitTime <= to.Value);
        }

        return result.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<TradeRecord> GetTodayTrades()
    {
        var today = DateTime.UtcNow.Date;
        return _trades
            .Where(t => t.ExitTime.Date == today)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public void Clear()
    {
        _trades.Clear();
    }
}
