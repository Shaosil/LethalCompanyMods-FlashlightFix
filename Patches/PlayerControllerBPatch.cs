using GameNetcodeStuff;
using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace FlashlightFix.Patches
{
    internal static class PlayerControllerBPatch
    {
        private static FieldInfo _previousPlayerField = null;

        [HarmonyPatch(typeof(PlayerControllerB), nameof(ItemTertiaryUse_performed))]
        [HarmonyPrefix]
        private static bool ItemTertiaryUse_performed(PlayerControllerB __instance)
        {
            // Flashlights do not have tertiary uses, so cancel out when this is called in that case
            return !(__instance.currentlyHeldObjectServer is FlashlightItem);
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(DiscardItem))]
        [HarmonyPostfix]
        private static void DiscardItem(FlashlightItem __instance)
        {
            // If there is a flashlight being used in the player's inventory, turn the helmet light back on (only happens when keeping helmet light on)
            if (Plugin.KeepHelmetLight.Value)
            {
                // Cache the reflection info
                if (_previousPlayerField == null)
                {
                    _previousPlayerField = typeof(FlashlightItem).GetField("previousPlayerHeldBy", BindingFlags.Instance | BindingFlags.NonPublic);
                }

                var player = _previousPlayerField.GetValue(__instance) as PlayerControllerB;

                for (int i = 0; i < player.ItemSlots.Length; i++)
                {
                    if (player.ItemSlots[i] is FlashlightItem flashlight && flashlight.isBeingUsed)
                    {
                        player.allHelmetLights[flashlight.flashlightTypeID].enabled = true;
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(SwitchToItemSlot))]
        [HarmonyPostfix]
        private static void SwitchToItemSlot(PlayerControllerB __instance, int slot)
        {
            // If the player already has an active flashlight when picking up a new INACTIVE one, turn the helmet lamp back on (if configured)
            if (Plugin.KeepHelmetLight.Value && __instance.ItemSlots[slot] is FlashlightItem slotFlashlight && !slotFlashlight.isBeingUsed)
            {
                for (int i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    // Find any other active flashlights in our inventory
                    if (i != slot && __instance.ItemSlots[i] is FlashlightItem otherFlashlight && otherFlashlight.isBeingUsed)
                    {
                        // If the new one should auto toggle, and it still has batteries, turn it on (the old one will have been toggled off by native code)
                        if (Plugin.OnlyOneLight.Value && Plugin.AutoSwitchToNew.Value && !slotFlashlight.insertedBattery.empty)
                        {
                            slotFlashlight.SwitchFlashlight(true);
                        }
                        // Otherwise, turn the helmet lamp of the old one back on
                        else
                        {
                            __instance.allHelmetLights[otherFlashlight.flashlightTypeID].enabled = true;
                            otherFlashlight.usingPlayerHelmetLight = true;
                        }

                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(SwitchFlashlight))]
        [HarmonyPrefix]
        private static void SwitchFlashlight(FlashlightItem __instance, bool on)
        {
            // Skip this if the user does not want either configurable fix
            if (on && (Plugin.KeepHelmetLight.Value || Plugin.OnlyOneLight.Value))
            {
                // If we are turning on a flashlight, make sure we turn off all player helmet lights since one may have been on from the above fix
                foreach (var flashlight in __instance.playerHeldBy.ItemSlots.OfType<FlashlightItem>())
                {
                    if (flashlight == __instance)
                    {
                        continue;
                    }

                    // If we are not configured to care about helmet lights, skip this extra code
                    if (Plugin.KeepHelmetLight.Value)
                    {
                        flashlight.usingPlayerHelmetLight = false;
                        __instance.playerHeldBy.allHelmetLights[flashlight.flashlightTypeID].enabled = false;
                    }

                    // Also make sure no other flashlight is on (if configured)
                    if (Plugin.OnlyOneLight.Value)
                    {
                        flashlight.SwitchFlashlight(false);
                    }
                }
            }
        }
    }
}