using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Windows.Input;
using HarmonyLib;
using OneClickFormationManager;
using SeapowerUI.ViewModels;
using static Program;

internal static class PatchTests
{
    private static void OtherMod() { }
    internal static void Run(string gameDirectory)
    {
        if (Type.GetType("Mono.Runtime") == null)
            throw new PlatformNotSupportedException("Run the patch checks with the game's Mono runtime.");
        Check("formation input hooks install and roll back without removing another mod's command patch", () =>
        {
            var patches = new Harmony("mebiustos.oneclickformationmanager.tests");
            var other = new Harmony("mebiustos.oneclickformationmanager.tests.other");
            var commandSetter = typeof(MainGameViewModel).GetProperty("SelectUnitCommand")!.GetSetMethod(true)!;
            try
            {
                other.Patch(commandSetter, prefix: new HarmonyMethod(typeof(PatchTests), nameof(OtherMod)));
                FormationInputPatches.Install(patches);
                foreach (var target in new[] { FormationInputPatches.AttachedTarget, FormationInputPatches.DetachingTarget })
                    Require(Harmony.GetPatchInfo(target)!.Owners.Contains(patches.Id), "A native formation lifecycle hook was not installed.");
                patches.UnpatchSelf();
                foreach (var target in new[] { FormationInputPatches.AttachedTarget, FormationInputPatches.DetachingTarget })
                    Require(Harmony.GetPatchInfo(target)?.Owners.Contains(patches.Id) != true, "A lifecycle hook was not removed.");
                Require(Harmony.GetPatchInfo(commandSetter)!.Owners.Contains(other.Id), "Another mod's selection command patch was removed.");
            }
            finally { patches.UnpatchSelf(); other.UnpatchSelf(); }
        });
        string installedFix = Path.Combine(gameDirectory, "Sea Power_Data", "StreamingAssets", "MebiusTosBugFixes", "MebiusTosBugFixes.dll");
        if (!File.Exists(installedFix))
        {
            Console.WriteLine("SKIP optional installed Bug Fixes command integration: the mod is not installed.");
            return;
        }
        Check("installed Bug Fixes command wrapping coexists with the new input hooks", () =>
        {
            var patches = new Harmony("mebiustos.oneclickformationmanager.tests.coexist");
            var fixes = new Harmony("mebiustos.oneclickformationmanager.tests.bugfixes");
            try
            {
                var assembly = Assembly.LoadFrom(installedFix);
                var fixType = assembly.GetType("MebiusTosBugFixes.Fixes.Selection.FormationUnitReselectionFix", true)!;
                object fix = Activator.CreateInstance(fixType, true)!;
                fixType.GetMethod("Install")!.Invoke(fix, new object[] { fixes });
                FormationInputPatches.Install(patches);
                var setter = typeof(MainGameViewModel).GetProperty("SelectUnitCommand")!.GetSetMethod(true)!;
                var call = new DynamicMethod("AssignFormationCommand", typeof(void),
                    new[] { typeof(MainGameViewModel), typeof(ICommand) }, typeof(PatchTests).Module, true);
                var il = call.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, setter); il.Emit(OpCodes.Ret);
                var assign = (Action<MainGameViewModel, ICommand>)call.CreateDelegate(typeof(Action<MainGameViewModel, ICommand>));
#pragma warning disable SYSLIB0050
                var model = (MainGameViewModel)FormatterServices.GetUninitializedObject(typeof(MainGameViewModel));
#pragma warning restore SYSLIB0050
                var original = new ProbeCommand();
                assign(model, original);
                var wrapped = model.SelectUnitCommand;
                Require(wrapped.GetType().Assembly == assembly, "The installed fix did not wrap the command.");
                object parameter = new object();
                int repairs = 0;
                SelectionCommand.Execute(wrapped, parameter, () => repairs++);
                Require(ReferenceEquals(original.Parameter, parameter) && original.Executions == 1 && repairs == 1,
                    "The installed wrapper did not forward the new selection action exactly once.");
                patches.UnpatchSelf();
                Require(Harmony.GetPatchInfo(setter)!.Owners.Contains(fixes.Id), "Removing input hooks removed the installed fix.");
            }
            finally { patches.UnpatchSelf(); fixes.UnpatchSelf(); }
        });
    }

    private sealed class ProbeCommand : ICommand
    {
        internal object? Parameter;
        internal int Executions;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { Parameter = parameter; Executions++; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
