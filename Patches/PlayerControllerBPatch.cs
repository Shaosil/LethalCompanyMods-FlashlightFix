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
        [HarmonyPrefix]
        private static void SwitchToItemSlot(PlayerControllerB __instance, int slot, out int __state)
        {
            // Keep track of what helmet cam type we are currently using
            __state = -1;

            if (__instance.ItemSlots[slot] is FlashlightItem flashlight && flashlight.CheckForLaser())
            {
                __state = FlashlightItemPatch.GetActiveFlashlightType(__instance);
                Plugin.MLS.LogDebug($"Storing state {__state} pre switch");
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(SwitchToItemSlot))]
        [HarmonyPostfix]
        private static void SwitchToItemSlot(PlayerControllerB __instance, int slot, int __state)
        {
            if (__instance.ItemSlots[slot] is FlashlightItem slotFlashlight)
            {
                if (slotFlashlight.isBeingUsed && slotFlashlight.CheckForLaser() && __state >= 0)
                {
                    // If we just switched to an active laser, make sure we change the helmet light back to whatever was stored in the prefix
                    Plugin.MLS.LogDebug($"Switched to laser. Changing {__instance.playerUsername}'s helmet cam type back to {__state}.");
                    __instance.ChangeHelmetLight(__state, true);
                }
                else if (__instance.IsOwner && Plugin.OnlyOneLight.Value && !slotFlashlight.isBeingUsed)
                {
                    // If the player already has an active flashlight when picking up a new INACTIVE one, keep a light on (if configured)
                    for (int i = 0; i < __instance.ItemSlots.Length; i++)
                    {
                        // Find any other active flashlights (not lasers) in our inventory
                        if (i != slot && __instance.ItemSlots[i] is FlashlightItem otherFlashlight && otherFlashlight.isBeingUsed && !otherFlashlight.CheckForLaser())
                        {
                            // If the new one still has batteries, turn it on (the old one will have been toggled off by native code)
                            if (!slotFlashlight.insertedBattery.empty && !slotFlashlight.CheckForLaser())
                            {
                                Plugin.MLS.LogDebug($"Flashlight in slot {slot} turning ON after switching to it");
                                slotFlashlight.UseItemOnClient();
                            }
                            // Otherwise, turn the helmet lamp of the old one back on
                            else
                            {
                                Plugin.MLS.LogDebug($"Flashlight in slot {i} pocketing ON after switching to empty flashlight or laser");
                                otherFlashlight.UseItemOnClient();
                                FlashlightItemPatch.ToggleFlashlightInPocket(otherFlashlight, __instance, true);
                            }

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
            if (!__instance.IsOwner || __instance.inTerminalMenu || __instance.isTypingChat || !__instance.isPlayerControlled)
            {
                return;
            }

            if (Keyboard.current[ToggleShortcutKey].wasPressedThisFrame)
            {
                // Get the nearest flashlight with charge, whether it's held or in the inventory
                var heldChargedFlashlights = __instance.ItemSlots.OfType<FlashlightItem>().Where(f => !f.CheckForLaser() && !f.insertedBattery.empty);
                var targetFlashlight = heldChargedFlashlights.FirstOrDefault(f => __instance.currentlyHeldObjectServer == f) // Held item?
                    ?? heldChargedFlashlights.FirstOrDefault(f => f.isBeingUsed) // Active already?
                    ?? heldChargedFlashlights.OrderBy(f => f.flashlightTypeID).FirstOrDefault(); // Whatever is left, ordered by Pro then regular

                if (targetFlashlight != null)
                {
                    targetFlashlight.UseItemOnClient();
                    if (targetFlashlight.isPocketed)
                    {
                        // If it was pocketed, we need to manually udpate the helmet light and send the signal to others
                        __instance.ChangeHelmetLight(targetFlashlight.flashlightTypeID, targetFlashlight.isBeingUsed);
                        FlashlightItemPatch.ToggleFlashlightInPocket(targetFlashlight, __instance, targetFlashlight.isBeingUsed);
                    }
                }
            }
        }
    }
}