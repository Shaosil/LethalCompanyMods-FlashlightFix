using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;

namespace FlashlightFix.Patches
{
    internal static class FlashlightItemPatch
    {
        private static FieldInfo _previousPlayerField = null;

        [HarmonyPatch(typeof(FlashlightItem), nameof(Start))]
        [HarmonyPostfix]
        private static void Start(FlashlightItem __instance)
        {
            // Make sure the correct material is displayed for the lamp part
            if (__instance.changeMaterial)
            {
                var materials = __instance.flashlightMesh.sharedMaterials;
                materials[1] = __instance.isBeingUsed ? __instance.bulbLight : __instance.bulbDark;
                __instance.flashlightMesh.sharedMaterials = materials;
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(DiscardItem))]
        [HarmonyPostfix]
        private static void DiscardItem(FlashlightItem __instance)
        {
            if (!__instance.IsOwner)
            {
                return;
            }

            // If there is another flashlight being used in the player's inventory, turn the helmet light back on (only happens when keeping helmet light on)
            if (Plugin.OnlyOneLight.Value)
            {
                // Cache the reflection info
                if (_previousPlayerField == null)
                {
                    _previousPlayerField = typeof(FlashlightItem).GetField("previousPlayerHeldBy", BindingFlags.Instance | BindingFlags.NonPublic);
                }

                var player = _previousPlayerField.GetValue(__instance) as PlayerControllerB;

                for (int i = 0; i < player.ItemSlots.Length; i++)
                {
                    if (player.ItemSlots[i] is FlashlightItem flashlight && flashlight != __instance && flashlight.isBeingUsed)
                    {
                        Plugin.MLS.LogDebug($"Flashlight in slot {i} pocketing ON after a discard");
                        flashlight.PocketItem();
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(SwitchFlashlight))]
        [HarmonyPostfix]
        private static void SwitchFlashlight(FlashlightItem __instance, bool on)
        {
            // If this is a non-owner, make sure the bulb, glow, and helmet lights are correctly set
            if (!__instance.IsOwner)
            {
                bool pocketed = __instance.usingPlayerHelmetLight;
                __instance.flashlightBulb.enabled = on && !pocketed;
                __instance.flashlightBulbGlow.enabled = on && !pocketed;
                if (__instance.playerHeldBy != null)
                {
                    __instance.playerHeldBy.helmetLight.enabled = on && pocketed;
                }
                return;
            }

            // Skip this if the user does not want either configurable fix
            if (on && Plugin.OnlyOneLight.Value)
            {
                // If we are turning on a flashlight, make sure we turn off all player helmet lights since one may have been on from the above fix
                for (int i = 0; i < __instance.playerHeldBy.ItemSlots.Length; i++)
                {
                    if (!(__instance.playerHeldBy.ItemSlots[i] is FlashlightItem otherFlashlight) || otherFlashlight == __instance)
                    {
                        continue;
                    }

                    Plugin.MLS.LogDebug($"Flashlight in slot {i} pocketing OFF after turning on another flashlight");
                    otherFlashlight.isBeingUsed = false;
                    otherFlashlight.PocketItem();
                }
            }
        }
    }
}