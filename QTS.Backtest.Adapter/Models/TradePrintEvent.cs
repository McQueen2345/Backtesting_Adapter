using QTS.Backtest.Adapter.Enums;

namespace QTS.Backtest.Adapter.Models;

/// <summary>
/// Represents a trade execution event (print) parsed from market data.
/// Used for Phase-2 OFI/Flow signals. In Phase-1 we focus on BookUpdateEvents.
/// </summary>
public class TradePrintEvent
{
    /// <summary>
    /// Timestamp of the trade. MUST be UTC!
    /// Validation enforced in parser.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Row index from source file for stable sorting when timestamps are equal.
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// Trade execution price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Traded quantity/size.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Identifies who initiated the trade (aggressor).
    /// Bid = Buyer aggressive (market buy hitting the ask)
    /// Ask = Seller aggressive (market sell hitting the bid)
    /// </summary>
    public Side Aggressor { get; set; }
}
