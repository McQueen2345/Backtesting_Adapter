namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Verwaltet ein rollendes Fenster von Werten und berechnet Median/MAD.
/// WICHTIG: Bei IsDataStale=true darf Add() NICHT aufgerufen werden!
/// </summary>
public interface IRollingStatistics
{
    int Count { get; }
    bool IsWarm { get; }
    void Add(double value);
    double GetMedian();
    double GetMad();
    void Reset();
}
