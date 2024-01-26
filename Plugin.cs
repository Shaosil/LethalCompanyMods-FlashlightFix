using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FlashlightFix.Patches;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine.InputSystem;

namespace FlashlightFix
{
    [BepInPlugin(Metadata.GUID, Metadata.PLUGIN_NAME, Metadata.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource MLS { get; private set; }

        private const string OptionalFixesSection = "Optional Fixes";
        public static ConfigEntry<bool> OnlyOneLight { get; private set; }
        public static ConfigEntry<bool> TreatLaserPointersAsFlashlights { get; private set; }
        public static ConfigEntry<string> ToggleShortcut { get; private set; }

        private void Awake()
        {
            MLS = Logger;

            var validKeys = Enum.GetValues(typeof(Key)).Cast<Key>().Where(c => c < Key.OEM1).Select(c => c.ToString()).ToArray();

            OnlyOneLight = Config.Bind(OptionalFixesSection, nameof(OnlyOneLight), true, "When turning on any flashlight, will turn off any others in your inventory that are still active.");
            TreatLaserPointersAsFlashlights = Config.Bind(OptionalFixesSection, nameof(TreatLaserPointersAsFlashlights), false, "If set to true, laser pointers will be like flashlights and automatically toggle off and on when switching to them, etc.");
            ToggleShortcut = Config.Bind(OptionalFixesSection, nameof(ToggleShortcut), Key.None.ToString(), new ConfigDescription($"A shortcut key to allow toggling a flashlight at any time.", new AcceptableValueList<string>(validKeys)));
            PlayerControllerBPatch.ToggleShortcutKey = Enum.Parse<Key>(ToggleShortcut.Value);
            MLS.LogInfo("Configuration Initialized.");

            Harmony.CreateAndPatchAll(typeof(PlayerControllerBPatch));
            MLS.LogInfo("PlayerControllerB patched.");

            Harmony.CreateAndPatchAll(typeof(FlashlightItemPatch));
            MLS.LogInfo("FlashlightItem patched.");

            MLS.LogInfo($"{Metadata.PLUGIN_NAME} v{Metadata.VERSION} fully loaded.");
        }
    }
}