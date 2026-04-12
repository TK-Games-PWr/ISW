using UnityEngine;

public static class Options
{
    public static bool IsCrouchHold = PlayerPrefs.HasKey("crouchMode") && PlayerPrefs.GetInt("crouchMode") == 0;
    public static bool IsLeanHold = PlayerPrefs.HasKey("leanMode") && PlayerPrefs.GetInt("leanMode") == 0;
}
