using System;
using SeaPower;
using SeapowerUI.ViewModels;

namespace OneClickFormationManager;

internal static class FormationRowSelection
{
    internal static void Select(FormationManagerViewModel manager, FormationRowHit hit, Action<ObjectBase> selectUnit)
    {
        bool heading = hit.Row is UnitFormation;
        bool expanded = heading && hit.Item.IsExpanded;
        selectUnit(hit.Unit);
        if (!heading) return;
        // Selecting the leader can highlight its station. Keep the clicked heading
        // and its expansion state, just as the game's heading double-click does.
        manager.SelectedItem = hit.Row;
        hit.Item.IsExpanded = expanded;
    }
}
