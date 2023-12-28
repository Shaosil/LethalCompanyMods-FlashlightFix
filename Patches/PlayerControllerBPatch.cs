using GameNetcodeStuff;
using HarmonyLib;

namespace FlashlightFix.Patches
{
    internal static class PlayerControllerBPatch
    {
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
                            Plugin.MLS.LogDebug($"Flashlight in slot {slot} turning ON after switching to it");
                            slotFlashlight.UseItemOnClient(true);
                        }
                        // Otherwise, turn the helmet lamp of the old one back on
                        else
                        {
                            Plugin.MLS.LogDebug($"Flashlight in slot {i} pocketing ON after switching to empty flashlight");
                            otherFlashlight.PocketItem();
                        }

                        break;
                    }
                }
            }
        }
    }
}