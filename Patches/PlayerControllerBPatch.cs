using GameNetcodeStuff;
using HarmonyLib;
using System.Linq;
using UnityEngine.InputSystem;

namespace FlashlightFix.Patches
{
    internal static class PlayerControllerBPatch
    {
        public static Key ToggleShortcutKey;

        [HarmonyPatch(typeof(PlayerControllerB), nameof(ItemTertiaryUse_performed))]
        [HarmonyPrefix]
        private static bool ItemTertiaryUse_performed(PlayerControllerB __instance)
        {
            // Flashlights do not have tertiary uses, so cancel out when this is called in that case
            return !(__instance.currentlyHeldObjectServer is FlashlightItem);
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(SwitchToItemSlot))]
        [HarmonyPostfix]
        private static void SwitchToItemSlot(PlayerControllerB __instance, int slot)
        {
            if (__instance.ItemSlots[slot] is FlashlightItem slotFlashlight)
            {
                if (slotFlashlight.isBeingUsed)
                {
                    // Also make sure the helmet light turns off
                    if (!slotFlashlight.CheckForLaser() || __instance.allHelmetLights[FlashlightItemPatch.LaserPointerTypeID].enabled)
                    {
                        Plugin.MLS.LogDebug($"Turning off helmet light after switching to an active flashlight.");
                        __instance.helmetLight.enabled = false;
                    }
                }

                // If the player already has an active flashlight (helmet lamp will be on) when picking up a new INACTIVE one, switch to the new one
                if (__instance.IsOwner && !slotFlashlight.isBeingUsed && __instance.helmetLight.enabled && !slotFlashlight.CheckForLaser() && Plugin.OnlyOneLight.Value)
                {
                    for (int i = 0; i < __instance.ItemSlots.Length; i++)
                    {
                        // Find the first active flashlights in our inventory that still has battery, and turn it on
                        if (i != slot && __instance.ItemSlots[i] is FlashlightItem otherFlashlight && otherFlashlight.usingPlayerHelmetLight
                            && !otherFlashlight.CheckForLaser() && !slotFlashlight.insertedBattery.empty && !slotFlashlight.CheckForLaser())
                        {
                            Plugin.MLS.LogDebug($"Flashlight in slot {slot} turning ON after switching to it");
                            slotFlashlight.UseItemOnClient();
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(Update))]
        [HarmonyPostfix]
        private static void Update(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner || ToggleShortcutKey == Key.None || __instance.inTerminalMenu || __instance.isTypingChat || !__instance.isPlayerControlled)
            {
                return;
            }

            if (Keyboard.current[ToggleShortcutKey].wasPressedThisFrame)
            {
                // Get the nearest flashlight with charge, whether it's held or in the inventory
                var heldChargedFlashlights = __instance.ItemSlots.OfType<FlashlightItem>().Where(f => !f.CheckForLaser() && !f.insertedBattery.empty);
                var targetFlashlight = heldChargedFlashlights.OrderByDescending(f => __instance.currentlyHeldObjectServer == f) // Held items first...
                    .ThenByDescending(f => f.isBeingUsed) // ... then by active status
                    .ThenBy(f => f.flashlightTypeID) // ... then by pro, regular, laser
                    .FirstOrDefault();

                if (targetFlashlight != null)
                {
                    targetFlashlight.UseItemOnClient();
                }
            }
        }
    }
}