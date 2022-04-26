// using System;
// using Grafted.Maths;
//
// namespace Grafted.Sim;
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