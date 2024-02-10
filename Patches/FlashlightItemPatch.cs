using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace FlashlightFix.Patches
{
    internal static class FlashlightItemPatch
    {
        public const int LaserPointerTypeID = 2; // See BBFlashlight.prefab, FlashlightItem.prefab, and LaserPointer.prefab

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

        [HarmonyPatch(typeof(FlashlightItem), "EquipItem")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchEquipFlashlight(IEnumerable<CodeInstruction> instructions)
        {
            var codeList = instructions.ToList();

            for (int i = 0; i < codeList.Count; i++)
            {
                if (codeList[i].opcode == OpCodes.Callvirt && (codeList[i].operand as MethodInfo)?.Name == nameof(PlayerControllerB.ChangeHelmetLight))
                {
                    // Remove both lines that affect the player's helmet light
                    codeList.RemoveRange(i - 5, 11);
                }
                else if (codeList[i].opcode == OpCodes.Call && (codeList[i].operand as MethodInfo)?.Name == nameof(FlashlightItem.SwitchFlashlight))
                {
                    // Remove the call to switch flashlight (moving it to the postfix)
                    codeList.RemoveRange(i - 5, 6);
                }
            }

            Plugin.MLS.LogDebug("Patched FlashlightItem.EquipItem");

            return codeList.AsEnumerable();
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(EquipItem))]
        [HarmonyPostfix]
        private static void EquipItem(FlashlightItem __instance)
        {
            // Move the switch to POST equip so we are aware of the pocketed field being set correctly
            if (__instance.isBeingUsed)
            {
                __instance.SwitchFlashlight(true);
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), "PocketItem")]
        [HarmonyPatch(typeof(FlashlightItem), nameof(PocketFlashlightClientRpc))]
        [HarmonyPrefix]
        private static void PocketFlashlightClientRpc(FlashlightItem __instance)
        {
            // Before we pocket, make sure we update the helmet lamp to the current type IF it is not on already (lasers can cause oddities)
            if (!__instance.playerHeldBy.helmetLight.enabled && __instance.isBeingUsed)
            {
                Plugin.MLS.LogDebug($"Updating helmet lamp type to {__instance.flashlightTypeID} after an active pocket");
                __instance.playerHeldBy.ChangeHelmetLight(__instance.flashlightTypeID, true);
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

            // If there is an active flashlight in the players inventory, turn the helmet light back on. Otherwise, activate one
            var otherFlashlights = ___previousPlayerHeldBy.ItemSlots.Where(i => i is FlashlightItem f && !f.CheckForLaser() && !f.insertedBattery.empty).Cast<FlashlightItem>();
            var activeFlashlight = otherFlashlights.FirstOrDefault(f => f.isBeingUsed);

            if (activeFlashlight != null)
            {
                Plugin.MLS.LogDebug($"Updating and enabling active helmet light to type {__instance.flashlightTypeID} after another flashlight was discarded.");
                ___previousPlayerHeldBy.ChangeHelmetLight(activeFlashlight.flashlightTypeID, true);
            }
            else if (___previousPlayerHeldBy.IsOwner && otherFlashlights.Any())
            {
                Plugin.MLS.LogDebug("Turning another flashlight on after an active one was discarded");
                otherFlashlights.First().UseItemOnClient();
            }
        }

        [HarmonyPatch(typeof(FlashlightItem), nameof(SwitchFlashlight))]
        [HarmonyPrefix]
        private static bool SwitchFlashlight(FlashlightItem __instance, bool on)
        {
            __instance.isBeingUsed = on;
            __instance.flashlightBulb.enabled = on && !__instance.isPocketed;
            __instance.flashlightBulbGlow.enabled = on && !__instance.isPocketed;

            // Copied and optimized from vanilla method
            if (__instance.changeMaterial)
            {
                __instance.flashlightMesh.materials[1] = on ? __instance.bulbLight : __instance.bulbDark;
            }

            // If there are no other active flashlights, turn helmet light off
            if (!on && __instance.playerHeldBy != null && __instance.playerHeldBy.helmetLight.enabled && !__instance.playerHeldBy.ItemSlots.Any(i => i is FlashlightItem f && !f.CheckForLaser() && f.isBeingUsed))
            {
                Plugin.MLS.LogDebug("Turning off helmet light");
                __instance.playerHeldBy.helmetLight.enabled = false;
            }
            else if (__instance.playerHeldBy != null && on)
            {
                // Update pocketed flashlight if needed
                if (__instance.isPocketed && __instance.playerHeldBy.pocketedFlashlight != __instance)
                {
                    Plugin.MLS.LogDebug("Updating pocketed flashlight");
                    __instance.playerHeldBy.pocketedFlashlight = __instance;
                }

                // Make sure the owner changes the helmet light here since we stripped it out of EquipItem()
                if (!__instance.CheckForLaser() || !__instance.playerHeldBy.helmetLight.enabled)
                {
                    Plugin.MLS.LogDebug($"Updating helmet light to type {__instance.flashlightTypeID} ({(on ? "ON" : "OFF")})");
                    __instance.playerHeldBy.ChangeHelmetLight(__instance.flashlightTypeID, __instance.isPocketed);
                }

                if (Plugin.OnlyOneLight.Value && !__instance.CheckForLaser())
                {
                    if (!__instance.IsOwner)
                    {
                        return false;
                    }

                    // Make sure no other flashlights in our inventory are on
                    for (int i = 0; i < __instance.playerHeldBy.ItemSlots.Length; i++)
                    {
                        if (__instance.playerHeldBy.ItemSlots[i] is FlashlightItem otherFlashlight && otherFlashlight != __instance && otherFlashlight.isBeingUsed && !otherFlashlight.CheckForLaser())
                        {
                            Plugin.MLS.LogDebug($"Flashlight in pocket slot {i} TURNING OFF after turning on a held flashlight");
                            otherFlashlight.UseItemOnClient();
                        }
                    }
                }
            }

            // Never call the original method
            return false;
        }

        public static bool CheckForLaser(this FlashlightItem flashlight)
        {
            // Return true if this is a laser pointer IF we are not treating them as flashlights
            return flashlight.flashlightTypeID == LaserPointerTypeID && !Plugin.TreatLaserPointersAsFlashlights.Value;
        }
    }
}