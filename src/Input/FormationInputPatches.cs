using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Noesis;
using Noesis.Interactivity;
using SeapowerUI;

namespace OneClickFormationManager;

internal static class FormationInputPatches
{
    private static readonly ConditionalWeakTable<FormationManagerSelectionBehavior, FormationClickInput> inputs = new();

    internal static MethodInfo AttachedTarget => Target("OnAttached");
    internal static MethodInfo DetachingTarget => Target("OnDetaching");

    private static MethodInfo Target(string name)
    {
        var method = typeof(FormationManagerSelectionBehavior).GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null)
            ?? throw new MissingMethodException(typeof(FormationManagerSelectionBehavior).FullName, name);
        if (method.ReturnType != typeof(void)) throw new InvalidOperationException("Unsupported formation behavior lifecycle method: " + name);
        return method;
    }

    internal static void Install(Harmony patches)
    {
        var attached = AttachedTarget;
        var detaching = DetachingTarget;
        patches.Patch(attached, postfix: new HarmonyMethod(typeof(FormationInputPatches), nameof(Attached)));
        patches.Patch(detaching, prefix: new HarmonyMethod(typeof(FormationInputPatches), nameof(Detaching)));
    }

    private static void Attached(FormationManagerSelectionBehavior __instance)
    {
        try
        {
            if (NoesisTeardown.InProgress || inputs.TryGetValue(__instance, out _)) return;
            var tree = ((AttachableObject)__instance).AssociatedObject as TreeView
                ?? throw new InvalidOperationException("The formation selection behavior has no TreeView.");
            var input = new FormationClickInput(tree);
            input.Attach();
            inputs.Add(__instance, input);
        }
        catch (Exception error) { ModLog.Error("Could not attach formation click handlers", error); }
    }

    private static void Detaching(FormationManagerSelectionBehavior __instance)
    {
        try
        {
            if (!inputs.TryGetValue(__instance, out var input)) return;
            inputs.Remove(__instance);
            input.Detach();
        }
        catch (Exception error) { ModLog.Error("Could not detach formation click handlers", error); }
    }
}
