using Noesis;
using SeaPower;
using SeapowerUI;

namespace OneClickFormationManager;

internal static class UnitRowHitTest
{
    internal static (object Row, ObjectBase Unit)? Find(DependencyObject? source, TreeView tree)
    {
        for (var node = source; node != null && node != tree; node = VisualTreeHelper.GetParent(node))
        {
            if (FormationManagerRowReorderBehavior.GetIsReorderHandle(node) ||
                node is ButtonBase || node is Selector || node is TextBoxBase || node is PasswordBox ||
                node is Thumb || node is ScrollBar || node is MenuItem) return null;
            if (node is UIElement element && !element.IsEnabled) return null;
            if (!(node is TreeViewItem item)) continue;
            object row = item.DataContext;
            // Formation headings retain their native double-click behavior.
            ObjectBase? unit = row is ObjectBase own ? own : (row as Station)?.UnitObject;
            return unit == null ? null : (row, unit);
        }
        return null;
    }
}
