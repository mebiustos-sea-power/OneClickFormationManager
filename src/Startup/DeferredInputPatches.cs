using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace OneClickFormationManager;

internal static class DeferredInputPatches
{
    private static Harmony? owner;
    private static bool installed;

    internal static MethodInfo Target => typeof(NoesisUnity).GetMethod(nameof(NoesisUnity.InitCore),
        BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null)
        ?? throw new MissingMethodException(typeof(NoesisUnity).FullName, nameof(NoesisUnity.InitCore));

    internal static void Install(Harmony patches)
    {
        // InitCore's type initializer is managed-only. Do not touch formation UI types here:
        // Harmony can run their static initializers while obtaining native method pointers.
        owner = patches;
        patches.Patch(Target, postfix: new HarmonyMethod(typeof(DeferredInputPatches), nameof(CoreInitialized)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CoreInitialized()
    {
        try
        {
            if (installed) return;
            var patches = owner ?? throw new InvalidOperationException("The deferred input patch owner is unavailable.");
            // This postfix runs only after the game's native core initialization has returned.
            FormationInputPatches.Install(patches);
            installed = true;
            ModLog.Info("Single-click unit selection is enabled after Noesis core initialization.");
        }
        catch (Exception error)
        {
            try { owner?.UnpatchSelf(); }
            catch (Exception rollbackError) { ModLog.Error("Deferred patch rollback failed", rollbackError); }
            ModLog.Error("Formation input initialization failed; single-click selection is unavailable", error);
        }
    }
}
