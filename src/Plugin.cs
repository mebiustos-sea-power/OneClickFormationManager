using System;
using System.IO;
using AnchorChain;
using HarmonyLib;
using SeaPower;

namespace OneClickFormationManager;

[ACPlugin(ModInfo.Id, ModInfo.Name, ModInfo.Version)]
public sealed class Plugin : IAnchorChainMod
{
    private static bool initialized;

    public void TriggerEntryPoint()
    {
        Harmony? patches = null;
        try
        {
            if (initialized) return;
            initialized = true;
            patches = new Harmony(ModInfo.Id);
            string package = Path.GetDirectoryName(typeof(Plugin).Assembly.Location)
                ?? throw new InvalidOperationException("The mod package directory is unavailable.");
            if (!ModActivation.IsEnabled(typeof(FileManager), package))
            {
                ModLog.Info("Disabled in the mod manager; no patches installed.");
                return;
            }
            DeferredInputPatches.Install(patches);
            ModLog.Info("Initialized " + ModInfo.Version + ". Awaiting Noesis core initialization.");
        }
        catch (Exception error)
        {
            try { patches?.UnpatchSelf(); }
            catch (Exception rollbackError) { ModLog.Error("Patch rollback failed", rollbackError); }
            ModLog.Error("Initialization failed; single-click unit selection is unavailable", error);
        }
    }
}
