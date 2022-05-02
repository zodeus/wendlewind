using System;
using Grafted.Sim.Persistence;

namespace Grafted.Sim;

public enum TimeOfDay {
    Error,
    Day,
    Night,
    Morning,
    LateMorning,
    Noon,
    Afternoon,
    Evening,
    Midnight,
    AllDay
}

public class SimTime : IExposable {
    public const int SecondsInMinute = 60;
    public const int SecondsInHour = SecondsInMinute * 60;
    public const int SecondsInDay = SecondsInHour * HoursInDay;

    public const int HoursInDay = 24;

    public const int MinutesPerKm = 10;
    public int CurrentTimeInSeconds = -1 /*SecondsInDay * 2 + 4325*/;
    public int Ticks = 0;
    public string CurrentTimeString => TimeSpan.FromSeconds(CurrentTimeInSeconds).ToString(@"hh\:mm");

    public int CurrentTime {
        get {
            TimeSpan time = TimeSpan.FromSeconds(CurrentTimeInSeconds);
            return time.Hours * 100 + time.Minutes;
        }
    }

    public string CurrentDayString => TimeSpan.FromSeconds(CurrentTimeInSeconds).Days.ToString();

    public bool IsNight => CurrentTime is > 1700 or < 0400;

    public void ExposeData() { }

    public override string ToString() {
        return CurrentTimeString;
    }

    public bool IsIntervalOf(int intervalInSeconds) {
        return CurrentTimeInSeconds % intervalInSeconds == 0;
    }

    public static int MinutesToSeconds(int minutes) {
        return SecondsInMinute * minutes;
    }

    public static int HoursToTicks(int hours) {
        return hours * SecondsInMinute;
    }

    public bool IsTimeOfDay(TimeOfDay timeOfDay) {
        return timeOfDay switch {
            TimeOfDay.Day => CurrentTime is > 0400 and < 1800,
            TimeOfDay.Night => CurrentTime is > 1800 or < 0400,
            TimeOfDay.Morning => CurrentTime is > 0400 and < 1100,
            TimeOfDay.Noon => CurrentTime is > 1200 and < 1300,
            TimeOfDay.Afternoon => CurrentTime is > 1300 and < 1700,
            TimeOfDay.Evening => CurrentTime > 1700,
            TimeOfDay.Midnight => CurrentTime is > 0 and < 0100,
            TimeOfDay.AllDay => true,
            _ => throw new ArgumentOutOfRangeException(nameof(timeOfDay), timeOfDay, null)
        };
    }

    public TimeOfDay GeneralTimeOfDay() {
        if (CurrentTime is >= 0400 and < 1200) {
            return TimeOfDay.Morning;
        }

        if (CurrentTime is > 1200 and < 1300) {
            return TimeOfDay.Noon;
        }

        if (CurrentTime is >= 1200 and < 1300) {
            return TimeOfDay.Noon;
        }

        if (CurrentTime is >= 1300 and < 1700) {
            return TimeOfDay.Afternoon;
        }

        if (CurrentTime >= 1700) {
            return TimeOfDay.Evening;
        }

        if (CurrentTime is >= 0 and < 0100) {
            return TimeOfDay.Midnight;
        }

        if (CurrentTime is >= 0100 and < 0400) {
            return TimeOfDay.Night;
        }

        return TimeOfDay.Error;
    }

    public static double HoursToSeconds() {
        throw new NotImplementedException();
    }
}