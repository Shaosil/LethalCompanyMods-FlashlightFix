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
                        Plugin.MLS.LogDebug($"Flashlight ID {flashlight.GetInstanceID()} pocketing ON after a discard");
                        flashlight.PocketItem();
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(SwitchToItemSlot))]
        [HarmonyPostfix]
        private static void SwitchToItemSlot(PlayerControllerB __instance, int slot)
        {
            if (!__instance.IsOwner)
            {
                return;
            }

            // If the player already has an active flashlight when picking up a new INACTIVE one, keep a light on (if configured)
            if (Plugin.OnlyOneLight.Value && __instance.ItemSlots[slot] is FlashlightItem slotFlashlight && !slotFlashlight.isBeingUsed)
            {
                for (int i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    // Find any other active flashlights in our inventory
                    if (i != slot && __instance.ItemSlots[i] is FlashlightItem otherFlashlight && otherFlashlight.isBeingUsed)
                    {
                        // If the new one still has batteries, turn it on (the old one will have been toggled off by native code)
                        if (!slotFlashlight.insertedBattery.empty)
                        {
                            Plugin.MLS.LogDebug($"Flashlight ID {slotFlashlight.GetInstanceID()} turning ON after switching to it");
                            slotFlashlight.UseItemOnClient(true);
                        }
                        // Otherwise, turn the helmet lamp of the old one back on
                        else
                        {
                            Plugin.MLS.LogDebug($"Flashlight ID {otherFlashlight.GetInstanceID()} pocketing ON after switching to empty flashlight");
                            otherFlashlight.PocketItem();
                        }

                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(SwitchFlashlight))]
        [HarmonyPostfix]
        private static void SwitchFlashlight(FlashlightItem __instance, bool on)
        {
            if (!__instance.IsOwner)
            {
                // Not sure why the bulb and glow are always set to false for non owners, so just override that here and return
                __instance.flashlightBulb.enabled = on;
                __instance.flashlightBulbGlow.enabled = on;
                return;
            }

            // Skip this if the user does not want either configurable fix
            if (on && Plugin.OnlyOneLight.Value)
            {
                // If we are turning on a flashlight, make sure we turn off all player helmet lights since one may have been on from the above fix
                foreach (var flashlight in __instance.playerHeldBy.ItemSlots.OfType<FlashlightItem>())
                {
                    if (flashlight == __instance || !flashlight.isBeingUsed)
                    {
                        continue;
                    }

                    Plugin.MLS.LogDebug($"Flashlight ID {flashlight.GetInstanceID()} pocketing OFF after turning on flashlight");
                    flashlight.isBeingUsed = false;
                    flashlight.PocketItem();
                }
            }
        }
    }
}