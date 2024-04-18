using BepInEx;

namespace FlashlightFix
{
    [BepInPlugin(Metadata.GUID, Metadata.PLUGIN_NAME, Metadata.VERSION)]
    [BepInDependency("ShaosilGaming.GeneralImprovements", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogWarning($"{Metadata.PLUGIN_NAME} v{Metadata.VERSION} is deprecated and has no active code! Feel free to remove it.");
        }
    }
}