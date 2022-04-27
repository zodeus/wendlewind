using System;
using Grafted.Sim.Entities.Pawns;
using Grafted.Sim.Gui;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public class SimTime : IExposable {
    public const int SecondsInMinute = 60;
    public const int SecondsInHour = SecondsInMinute * 60;
    public const int SecondsInDay = SecondsInHour * 24;
    public const int MinutesPerKm = 10;
    public int CurrentTimeInSeconds = 0 /*SecondsInDay * 2 + 4325*/;
    public int Ticks = 0;
    public string CurrentTimeString => TimeSpan.FromSeconds(CurrentTimeInSeconds).ToString(@"hh\:mm");
    public string CurrentDayString => TimeSpan.FromSeconds(CurrentTimeInSeconds).Days.ToString();

    public void ExposeData() { }

    public override string ToString() {
        return CurrentTimeString;
    }

    public static float MinutesToSeconds(int minutes) {
        return SecondsInMinute * minutes;
    }
}

//
// public static class SimTime {
//     //public const int TicksPerDay = (int) (1 * Ticker.TicksPerSecond); // super fast mode
//     public const int TicksPerDay = 120 * Ticker.TicksPerSecond; // currently 2 minutes
//     public const int DaysPerYear = 256;
//     public const int DaysPerQuarter = DaysPerYear / 4;
//     public const int DaysPerMonth = DaysPerQuarter / 2;
//     public const int MonthsPerYear = DaysPerYear / DaysPerMonth;
//     public const int TicksPerHour = TicksPerDay / 24;
//
//     public const int TicksPerYear = DaysPerYear * TicksPerDay;
//     // 18000 Day
//     // 750 Hour
//     // 375 30 minutes
//     // 187 15 minutes
//     // 125 10 minutes
//
//     public static int DaysPassed => Convert.ToInt32((float) Core.Sim.Ticker.Ticks / TicksPerDay);
//     public static int DayOfYear => DayOfYearAt(Core.Sim.Ticker.Ticks);
//
//     public static string CurrentDate => TickToDate(Core.Sim.Ticker.Ticks);
//
//     public static int YearAt(int atTick) {
//         return Convert.ToInt32((float) atTick / TicksPerYear) + 1;
//     }
//
//
//     public static int DayOfYearAt(int atTick) {
//         return Mathf.FloorToInt((float) (atTick % TicksPerYear) / TicksPerDay);
//     }
//
//     public static int HourOfDayAt(int atTick) {
//         return Mathf.FloorToInt((float) (atTick % TicksPerDay) / TicksPerHour);
//     }
//
//     public static string TickToDate(int atTick) {
//         return $"Year {YearAt(atTick)}, Day {DayOfYearAt(atTick)}, Hour {HourOfDayAt(atTick).ToString().PadLeft(2, '0')}";
//     }
// }