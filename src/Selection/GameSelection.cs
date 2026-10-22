using System;
using System.Linq;
using SeaPower;
using SeapowerUI.ViewModels;

namespace OneClickFormationManager;

internal static class GameSelection
{
    internal static void Select(MainGameViewModel model, ObjectBase unit)
    {
        if (unit == null) return;
        if (!unit.IsPlayerObject) throw new InvalidOperationException("A formation-manager unit is not player controlled.");
        var map = model.Map ?? throw new InvalidOperationException("The current battle map is unavailable.");
        var entry = map.MapKnownUnits.AllUnits.SingleOrDefault(item => ReferenceEquals(item.Unit.Object, unit))
            ?? throw new InvalidOperationException("The unit is missing from the current battle map.");
        var command = model.SelectUnitCommand
            ?? throw new InvalidOperationException("The game's unit-selection command is unavailable.");

        SelectionCommand.Execute(command, unit, () =>
        {
            if (!ReferenceEquals(model.Map, map)) throw new InvalidOperationException("The battle map changed during unit selection.");
            var current = map.SelectedObject.Value;
            if (ReferenceEquals(current, entry.Unit) || ReferenceEquals(current, unit)) return;
            // The camera command can be a no-op for the same unit. Use the native map
            // transaction only when needed; an installed reselection fix may already have done this.
            entry.Selected = true;
        });
    }
}
