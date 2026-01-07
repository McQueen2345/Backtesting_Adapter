using QTS.Edge.TradeManagement.Configuration;
using QTS.Edge.TradeManagement.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Services;

/// <summary>
/// Verwaltet Position State Machine, Entry/Exit und Cooldowns.
/// </summary>
public sealed class PositionTracker : IPositionTracker
{
    private readonly TradeManagementConfig _config;

    // Position State
    private PositionState _state = PositionState.Flat;
    private int _direction;
    private int _quantity;
    private decimal _entryPrice;
    private DateTime? _entryTime;

    // Cooldown State
    private DateTime? _cooldownEndTime;

    public PositionTracker(TradeManagementConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public Position CurrentPosition => new()
    {
        State = _state,
        Direction = _direction,
        Quantity = _quantity,
        EntryPrice = _entryPrice,
        EntryTime = _entryTime,
        UnrealizedPnLTicks = 0m,  // Wird extern berechnet
        UnrealizedPnLDollars = 0m // Wird extern berechnet
    };

    /// <inheritdoc />
    public bool IsInCooldown(DateTime currentTime)
    {
        if (_cooldownEndTime == null)
            return false;

        return currentTime < _cooldownEndTime.Value;
    }

    /// <inheritdoc />
    public bool TryEntry(int signal, decimal midPrice, DateTime timestamp, int quantity)
    {
        // Entry nur möglich wenn FLAT
        if (_state != PositionState.Flat)
            return false;

        // Signal validieren
        if (signal != 1 && signal != -1)
            return false;

        // Entry durchführen
        _state = signal == 1 ? PositionState.Long : PositionState.Short;
        _direction = signal;
        _quantity = quantity;
        _entryPrice = midPrice;
        _entryTime = timestamp;

        // Cooldown wird bei Entry NICHT zurückgesetzt
        // (Cooldown läuft weiter wenn noch aktiv)

        return true;
    }

    /// <inheritdoc />
    public ExitResult CheckExit(decimal midPrice, DateTime timestamp)
    {
        // Nur prüfen wenn in Position
        if (_state == PositionState.Flat)
            return ExitResult.NoExit;

        if (_entryTime == null)
            return ExitResult.NoExit;

        // 1. Emergency Stop Check (-10 Ticks)
        decimal pnlTicks = CalculatePnLTicks(midPrice);
        if (pnlTicks <= -_config.EmergencyStopTicks)
        {
            return ExitResult.Exit(DecisionReasons.EMERGENCY_STOP);
        }

        // 2. Time Exit Check (MaxPositionSeconds)
        var positionDuration = timestamp - _entryTime.Value;
        if (positionDuration.TotalSeconds >= _config.MaxPositionSeconds)
        {
            return ExitResult.Exit(DecisionReasons.TIME_EXIT);
        }

        return ExitResult.NoExit;
    }

    /// <inheritdoc />
    public TradeRecord ExecuteExit(decimal midPrice, DateTime timestamp, string reason)
    {
        if (_state == PositionState.Flat)
            throw new InvalidOperationException("Cannot exit when not in position");

        if (_entryTime == null)
            throw new InvalidOperationException("Entry time is null");

        // PnL berechnen
        decimal pnlTicks = CalculatePnLTicks(midPrice);
        decimal grossPnL = pnlTicks * _quantity * _config.TickValue;
        decimal fees = _quantity * _config.CommissionPerContractRT;
        decimal netPnL = grossPnL - fees;

        // TradeRecord erstellen
        var record = new TradeRecord
        {
            EntryTime = _entryTime.Value,
            ExitTime = timestamp,
            EntryPrice = _entryPrice,
            ExitPrice = midPrice,
            Direction = _direction,
            Quantity = _quantity,
            PnLTicks = pnlTicks,
            GrossPnLDollars = grossPnL,
            FeesDollars = fees,
            NetPnLDollars = netPnL,
            ExitReason = reason,
            Duration = timestamp - _entryTime.Value
        };

        // Position zurücksetzen
        ResetPosition();

        return record;
    }

    /// <inheritdoc />
    public void SetCooldown(DateTime timestamp, bool isEmergencyStop)
    {
        int cooldownMs = isEmergencyStop
            ? _config.CooldownEscalatedMs
            : _config.CooldownNormalMs;

        _cooldownEndTime = timestamp.AddMilliseconds(cooldownMs);
    }

    /// <inheritdoc />
    public void Reset()
    {
        ResetPosition();
        _cooldownEndTime = null;
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    private decimal CalculatePnLTicks(decimal currentPrice)
    {
        // PnL in Ticks pro Kontrakt
        // Long: (Current - Entry) / TickSize
        // Short: (Entry - Current) / TickSize
        return _direction * (currentPrice - _entryPrice) / _config.TickSize;
    }

    private void ResetPosition()
    {
        _state = PositionState.Flat;
        _direction = 0;
        _quantity = 0;
        _entryPrice = 0m;
        _entryTime = null;
    }
}
