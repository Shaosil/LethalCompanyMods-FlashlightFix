using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace FlashlightFix.Patches
{
    internal static class FlashlightItemPatch
    {
        private const int LaserPointerTypeID = 2; // See BBFlashlight.prefab, FlashlightItem.prefab, and LaserPointer.prefab

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
        private static void DiscardItem(FlashlightItem __instance, PlayerControllerB ___previousPlayerHeldBy)
        {
            if (!__instance.isBeingUsed || !Plugin.OnlyOneLight.Value)
            {
                return;
            }

            // If there is another flashlight in the player's inventory, turn the helmet light back on (only happens when keeping helmet light on)
            for (int i = 0; i < ___previousPlayerHeldBy.ItemSlots.Length; i++)
            {
                var slotLight = ___previousPlayerHeldBy.ItemSlots[i] as FlashlightItem;
                if (slotLight != null && slotLight != __instance && !slotLight.insertedBattery.empty && !slotLight.CheckForLaser())
                {
                    Plugin.MLS.LogDebug($"Flashlight in slot {i} pocketing ON for {___previousPlayerHeldBy.playerUsername} after a discard");
                    ToggleFlashlightInPocket(slotLight, ___previousPlayerHeldBy, true, false);
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(SwitchFlashlight))]
        [HarmonyPrefix]
        private static void SwitchFlashlight(FlashlightItem __instance, bool on, out KeyValuePair<int, bool> __state)
        {
            __state = new KeyValuePair<int, bool>(-1, false);

            // Clients need to keep track of what the previous helmet style was so they can change it back if needed
            if (__instance.playerHeldBy != null && !__instance.playerHeldBy.IsOwner)
            {
                __state = new KeyValuePair<int, bool>(GetActiveFlashlightType(__instance.playerHeldBy), __instance.playerHeldBy.helmetLight.enabled);
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(SwitchFlashlight))]
        [HarmonyPostfix]
        private static void SwitchFlashlight(FlashlightItem __instance, bool on, KeyValuePair<int, bool> __state)
        {
            // If this is a non-owner, make sure the bulb, glow, and helmet lights are correctly set
            if (!__instance.IsOwner)
            {
                bool pocketed = __instance.usingPlayerHelmetLight;
                __instance.flashlightBulb.enabled = on && !pocketed;
                __instance.flashlightBulbGlow.enabled = on && !pocketed;

                // If this is a laser and we have a previous helmet light, switch it back
                if (__state.Key >= 0 && __instance.CheckForLaser())
                {
                    Plugin.MLS.LogDebug($"Setting {__instance.playerHeldBy.playerUsername}'s helmet light type to {__state.Key} (on = {__state.Value})");
                    __instance.playerHeldBy.ChangeHelmetLight(__state.Key, __state.Value);
                }

                return;
            }

            // Skip this if the user does not care about having only one light on
            if (on && Plugin.OnlyOneLight.Value && !__instance.CheckForLaser())
            {
                // If we are turning on a flashlight, make sure we turn off all player helmet lights since one may have been on from the above fix
                for (int i = 0; i < __instance.playerHeldBy.ItemSlots.Length; i++)
                {
                    // If the other flashlight is being used and not a laser pointer, turn it off
                    if (__instance.playerHeldBy.ItemSlots[i] is FlashlightItem otherFlashlight && otherFlashlight != __instance && otherFlashlight.isBeingUsed && !otherFlashlight.CheckForLaser())
                    {
                        Plugin.MLS.LogDebug($"Flashlight in slot {i} pocketing OFF after turning on another flashlight");
                        ToggleFlashlightInPocket(otherFlashlight, __instance.playerHeldBy, false);
                    }
                }
            }
        }

        public static void ToggleFlashlightInPocket(FlashlightItem flashlight, PlayerControllerB player, bool on, bool callRPC = true)
        {
            if (!flashlight.insertedBattery.empty)
            {
                flashlight.isBeingUsed = on;
                flashlight.usingPlayerHelmetLight = on;
                flashlight.flashlightBulb.enabled = false;
                flashlight.flashlightBulbGlow.enabled = false;
                player.pocketedFlashlight = flashlight;
                if (on)
                {
                    player.ChangeHelmetLight(flashlight.flashlightTypeID, on);
                }
                if (callRPC)
                {
                    flashlight.PocketFlashlightServerRpc(on);
                }
            }
        }

        public static bool CheckForLaser(this FlashlightItem flashlight)
        {
            // Return true if this is a laser pointer IF we are not treating them as flashlights
            return flashlight.flashlightTypeID == LaserPointerTypeID && !Plugin.TreatLaserPointersAsFlashlights.Value;
        }

        public static int GetActiveFlashlightType(PlayerControllerB player)
        {
            if (player.helmetLight.enabled) return player.allHelmetLights.ToList().IndexOf(player.helmetLight);
            else return (player.currentlyHeldObjectServer as FlashlightItem)?.flashlightTypeID ?? -1;
        }
    }
}