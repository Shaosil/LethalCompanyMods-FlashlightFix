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

                // Ensure we are using the proper helmet light each time we switch to a flashlight
                UpdateHelmetLight(__instance);
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
                var targetFlashlight = __instance.ItemSlots.OfType<FlashlightItem>().Where(f => !f.insertedBattery.empty) // All charged flashlight items
                    .OrderBy(f => f.CheckForLaser()) // Sort by non-lasers first
                    .ThenByDescending(f => __instance.currentlyHeldObjectServer == f) // ... then by held items
                    .ThenByDescending(f => f.isBeingUsed) // ... then by active status
                    .ThenBy(f => f.flashlightTypeID) // ... then by pro, regular, laser
                    .FirstOrDefault();

                // Active lasers in hand are toggling OFF first

                if (targetFlashlight != null)
                {
                    targetFlashlight.UseItemOnClient();
                }
            }
        }

        public static void UpdateHelmetLight(PlayerControllerB player)
        {
            // The helmet light should always be the first sorted flashlight that is on (lasers are sorted last, then pocketed flashlights are prioritized)
            var activeLight = player.ItemSlots.OfType<FlashlightItem>()
                .Where(f => f.isBeingUsed)
                .OrderBy(f => f.CheckForLaser())
                .ThenByDescending(f => f.isPocketed)
                .FirstOrDefault();

            // Update it if the current helmet light is something else
            if (activeLight != null && player.helmetLight != player.allHelmetLights[activeLight.flashlightTypeID])
            {
                Plugin.MLS.LogDebug($"Updating helmet light to type {activeLight.flashlightTypeID} ({(activeLight.isPocketed ? "ON" : "OFF")}).");
                player.ChangeHelmetLight(activeLight.flashlightTypeID, activeLight.isPocketed);
                player.pocketedFlashlight = activeLight;
            }

            // Always make sure the helmet light state is correct
            bool helmetLightShouldBeOn = activeLight != null && activeLight.isBeingUsed && activeLight.isPocketed;
            if (player.helmetLight != null && player.helmetLight.enabled != helmetLightShouldBeOn)
            {
                // Toggle helmet light here if needed
                Plugin.MLS.LogDebug($"Toggling helmet light {(helmetLightShouldBeOn ? "on" : "off")}.");
                player.helmetLight.enabled = helmetLightShouldBeOn;
            }
        }
    }
}