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
        public static ConfigEntry<bool> KeepHelmetLight { get; private set; }
        public static ConfigEntry<bool> OnlyOneLight { get; private set; }
        public static ConfigEntry<bool> AutoSwitchToNew { get; private set; }

        private void Awake()
        {
            MLS = Logger;

            KeepHelmetLight = Config.Bind(OptionalFixesSection, nameof(KeepHelmetLight), true, "When picking up an extra, inactive flashlight, will keep your previous flashlight active if applicable.");
            OnlyOneLight = Config.Bind(OptionalFixesSection, nameof(OnlyOneLight), true, "When turning on any flashlight, will turn off any others in your inventory that are still active.");
            AutoSwitchToNew = Config.Bind(OptionalFixesSection, nameof(AutoSwitchToNew), true, $"When switching to another flashlight, will automatically turn it on if your previous one was active and the new one has batteries. Only applies when {nameof(OnlyOneLight)} is true."); MLS.LogInfo("Configuration values initialized.");

            Harmony.CreateAndPatchAll(typeof(PlayerControllerBPatch));
            MLS.LogInfo("PlayerControllerB patched.");

            MLS.LogInfo($"{Metadata.PLUGIN_NAME} fully loaded.");
        }
    }
}