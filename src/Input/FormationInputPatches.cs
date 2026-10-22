using System;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Noesis;
using Noesis.Interactivity;
using SeapowerUI;

namespace OneClickFormationManager;

internal static class FormationInputPatches
{
    private static readonly ConditionalWeakTable<FormationManagerSelectionBehavior, FormationClickInput> inputs = new();
    private static Harmony? owner;

    internal static MethodInfo AttachedTarget => Target("OnAttached");
    internal static MethodInfo DetachingTarget => Target("OnDetaching");

    internal static MethodInfo DoubleClickTarget => Target("OnPreviewMouseLeftButtonDown", typeof(object), typeof(MouseButtonEventArgs));

    private static MethodInfo Target(string name, params Type[] parameters)
    {
        var method = typeof(FormationManagerSelectionBehavior).GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, parameters, null)
            ?? throw new MissingMethodException(typeof(FormationManagerSelectionBehavior).FullName, name);
        if (method.ReturnType != typeof(void)) throw new InvalidOperationException("Unsupported formation behavior method: " + name);
        return method;
    }

    internal static void Install(Harmony patches)
    {
        var attached = AttachedTarget;
        var detaching = DetachingTarget;
        patches.Patch(attached, postfix: new HarmonyMethod(typeof(FormationInputPatches), nameof(Attached)));
        patches.Patch(detaching, prefix: new HarmonyMethod(typeof(FormationInputPatches), nameof(Detaching)));
        owner = patches;
    }

    private static bool DoubleClick(FormationManagerSelectionBehavior __instance, MouseButtonEventArgs __1)
    {
        try
        {
            if (NoesisTeardown.InProgress) return false;
            if (__1.ClickCount < 2) return true;
            var tree = ((AttachableObject)__instance).AssociatedObject as TreeView
                ?? throw new InvalidOperationException("The formation selection behavior has no TreeView.");
            var source = __1.Source as DependencyObject;
            // Let the arrow handle its clicks without the native heading selection.
            if (FormationRowHitTest.IsExpansionButton(source, tree)) return false;
            var heading = FormationRowHitTest.FindHeading(source, tree);
            if (heading == null) return true;
            // The first click already selects the leader. Consume the second press
            // so only this handler toggles expansion, without a native duplicate.
            heading.IsExpanded = !heading.IsExpanded;
            __1.Handled = true;
            return false;
        }
        catch (Exception error)
        {
            ModLog.Error("Could not check formation double-click target", error);
            return false;
        }
    }

    private static void Attached(FormationManagerSelectionBehavior __instance)
    {
        try
        {
            if (NoesisTeardown.InProgress || inputs.TryGetValue(__instance, out _)) return;
            var tree = ((AttachableObject)__instance).AssociatedObject as TreeView
                ?? throw new InvalidOperationException("The formation selection behavior has no TreeView.");
            // The native attachment has already established the game state used
            // by this handler. Obtain its method pointer only at that point.
            var patches = owner ?? throw new InvalidOperationException("The formation patch owner is unavailable.");
            var doubleClick = DoubleClickTarget;
            if (Harmony.GetPatchInfo(doubleClick)?.Owners.Contains(patches.Id) != true)
                patches.Patch(doubleClick, prefix: new HarmonyMethod(typeof(FormationInputPatches), nameof(DoubleClick)));
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
