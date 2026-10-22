using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OneClickFormationManager;
using static Program;

internal static class StartupTests
{
    internal static bool CoreInitialized { get; private set; }

    internal static void Run()
    {
        var patches = new Harmony("mebiustos.oneclickformationmanager.tests.startup");
        var initialized = typeof(Noesis.GUI).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(Noesis.GUI).FullName, "_initialized");
        Require(!(bool)initialized.GetValue(null)!, "The startup regression must begin before native UI initialization.");
        try
        {
            Check("early startup installs only the readiness hook without initializing native UI or formation behaviors", () =>
            {
                DeferredInputPatches.Install(patches);
                Require(!(bool)initialized.GetValue(null)!, "Installing the startup hook initialized Noesis.");
                Require(Harmony.GetPatchInfo(DeferredInputPatches.Target)!.Owners.Contains(patches.Id), "The readiness hook was not installed.");
                foreach (var target in new[] { FormationInputPatches.AttachedTarget, FormationInputPatches.DetachingTarget })
                    Require(Harmony.GetPatchInfo(target)?.Owners.Contains(patches.Id) != true, "A formation UI hook was installed before readiness.");
            });
            Check("the actual game InitCore completion installs formation hooks once and preserves later initialization calls", () =>
            {
                NoesisUnity.InitCore();
                CoreInitialized = true;
                Require((bool)initialized.GetValue(null)!, "The native UI core did not initialize.");
                foreach (var target in new[] { FormationInputPatches.AttachedTarget, FormationInputPatches.DetachingTarget })
                    Require(Harmony.GetPatchInfo(target)!.Owners.Contains(patches.Id), "A formation hook was not installed after readiness.");
                NoesisUnity.InitCore();
                var hooks = Harmony.GetPatchInfo(FormationInputPatches.AttachedTarget)!.Postfixes;
                Require(hooks.Count(p => p.owner == patches.Id) == 1, "A later core initialization duplicated the input hook.");
            });
            Check("input callback exceptions stay inside the mod and a subsequent callback can recover", () =>
            {
                var input = new FormationClickInput(null!);
                var guard = typeof(FormationClickInput).GetMethod("Guard", BindingFlags.Instance | BindingFlags.NonPublic)!;
                guard.Invoke(input, new object[] { new Action(() => throw new InvalidOperationException("handler failure")) });
                bool recovered = false;
                guard.Invoke(input, new object[] { new Action(() => recovered = true) });
                Require(recovered, "A failed input callback blocked later callbacks.");
            });
        }
        finally { patches.UnpatchSelf(); }
    }
}
