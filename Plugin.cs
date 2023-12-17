using BepInEx;
using BepInEx.Logging;
using FlashlightFix.Patches;
using HarmonyLib;

namespace FlashlightFix
{
    [BepInPlugin(Metadata.GUID, Metadata.PLUGIN_NAME, Metadata.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource MLS { get; private set; }

        private void Awake()
        {
            MLS = Logger;
            MLS.LogInfo($"{Metadata.PLUGIN_NAME} loaded.");

            Harmony.CreateAndPatchAll(typeof(PlayerControllerBPatch));
        }
    }
}