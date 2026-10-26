using HarmonyLib;
using MelonLoader;

namespace EyesSpeedrunToolkit
{
    [HarmonyPatch]
    public static class WinTriggerPatch
    {
        // Патчим FadeOutMobile — он вызывается ТОЛЬКО при реальной победе
        [HarmonyPatch(typeof(ExitDoor), "FadeOutMobile")]
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (SpeedrunTimer.IsRunning())
            {
                MelonLogger.Msg($"WinTrigger: Player WON! Stopping timer at {SpeedrunTimer.GetCurrentTimeFormatted()}");
                SpeedrunTimer.Finish();
            }
        }
    }
}