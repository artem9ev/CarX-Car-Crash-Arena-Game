using UnityEngine;

public static class TimeConverter
{
    private static string RoundTimerText(int time)
    {
        return time < 10 ? $"0{time}" : time.ToString();
    }

    public static string GetTimer(float time)
    {
        int minutes = Mathf.FloorToInt(time) / 60;
        int seconds = Mathf.FloorToInt(time) % 60;
        int milisecs = Mathf.FloorToInt(time * 100) % 100;

        return $"{RoundTimerText(minutes)}:{RoundTimerText(seconds)}:{RoundTimerText(milisecs)}";
    }
}
