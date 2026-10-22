using Noesis;
using SeaPower;
using SeapowerUI;

namespace OneClickFormationManager;

internal readonly struct FormationRowHit
{
    internal readonly TreeViewItem Item;
    internal readonly object Row;
    internal readonly ObjectBase Unit;

    internal FormationRowHit(TreeViewItem item, object row, ObjectBase unit)
    { Item = item; Row = row; Unit = unit; }
}

internal static class FormationRowHitTest
{
    internal static FormationRowHit? Find(DependencyObject? source, TreeView tree)
    {
        var item = FindItem(source, tree, out bool header);
        if (item == null) return null;
        object row = item.DataContext;
        ObjectBase? unit = row switch
        {
            ObjectBase own => own,
            Station station => station.UnitObject,
            UnitFormation formation when header => formation.LeaderStation?.UnitObject,
            _ => null
        };
        return unit == null ? null : new FormationRowHit(item, row, unit);
    }

    internal static TreeViewItem? FindHeading(DependencyObject? source, TreeView tree)
    {
        var item = FindItem(source, tree, out bool header);
        return header && item?.DataContext is UnitFormation ? item : null;
    }

    private static TreeViewItem? FindItem(DependencyObject? source, TreeView tree, out bool header)
    {
        header = false;
        for (var node = source; node != null && node != tree; node = VisualTreeHelper.GetParent(node))
        {
            if (FormationManagerRowReorderBehavior.GetIsReorderHandle(node) ||
                node is ButtonBase || node is Selector || node is TextBoxBase || node is PasswordBox ||
                node is Thumb || node is ScrollBar || node is MenuItem) return null;
            if (node is UIElement element && !element.IsEnabled) return null;
            if (node is ContentPresenter presenter && presenter.Name == "PART_Header") header = true;
            if (node is TreeViewItem item) return item;
        }
        return null;
    }

    internal static bool IsExpansionButton(DependencyObject? source, TreeView tree)
    {
        ToggleButton? expand = null;
        for (var node = source; node != null && node != tree; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ToggleButton button && button.Name == "ExpandButton") expand = button;
            if (node is TreeViewItem item)
                return item.DataContext is UnitFormation && expand != null && ReferenceEquals(expand.TemplatedParent, item);
        }
        return false;
    }
}
