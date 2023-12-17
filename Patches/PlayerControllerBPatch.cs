using GameNetcodeStuff;
using HarmonyLib;

namespace FlashlightFix.Patches
{
    internal static class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB), nameof(ItemTertiaryUse_performed))]
        [HarmonyPrefix]
        public static bool ItemTertiaryUse_performed(PlayerControllerB __instance)
        {
            // Flashlights do not have tertiary uses, so cancel out when this is called in that case
            return !(__instance.currentlyHeldObjectServer is FlashlightItem);
        }
    }
}