using UnityEngine;

public static class Settings
{

    private const string PREF_CURRENT_STATION = "CurrentStation";

    public static string GetCurrentStationName()
    {
        return PlayerPrefs.GetString(PREF_CURRENT_STATION);
    }

    public static void SetCurrentStationName(string currentStation)
    {
        if (currentStation != null && currentStation != "")
        {
            PlayerPrefs.SetString(PREF_CURRENT_STATION, currentStation);
        }
        PlayerPrefs.Save();
    }

}
