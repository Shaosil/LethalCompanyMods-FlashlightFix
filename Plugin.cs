using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FlashlightFix.Patches;
using HarmonyLib;

namespace FlashlightFix
{
    [BepInPlugin(Metadata.GUID, Metadata.PLUGIN_NAME, Metadata.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource MLS { get; private set; }

        private const string OptionalFixesSection = "Optional Fixes";
        public static ConfigEntry<bool> OnlyOneLight { get; private set; }

        private void Awake()
        {
            MLS = Logger;

            OnlyOneLight = Config.Bind(OptionalFixesSection, nameof(OnlyOneLight), true, "When turning on any flashlight, will turn off any others in your inventory that are still active.");
            MLS.LogInfo("Configuration Initialized.");

            Harmony.CreateAndPatchAll(typeof(PlayerControllerBPatch));
            MLS.LogInfo("PlayerControllerB patched.");

            MLS.LogInfo($"{Metadata.PLUGIN_NAME} fully loaded.");
        }
    }
}