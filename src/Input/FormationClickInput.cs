using System;
using System.Collections.Generic;
using Noesis;
using SeaPower;
using SeapowerUI.ViewModels;

namespace OneClickFormationManager;

internal sealed class FormationClickInput
{
    private readonly TreeView tree;
    private readonly ClickGesture<ObjectBase> gesture = new();
    private readonly List<Action> unsubscribe = new();
    private bool failed;

    internal FormationClickInput(TreeView tree) { this.tree = tree; }

    internal void Attach()
    {
        try
        {
            Bind(() => tree.PreviewMouseDown += Down, () => tree.PreviewMouseDown -= Down);
            Bind(() => tree.PreviewMouseUp += Up, () => tree.PreviewMouseUp -= Up);
            Bind(() => tree.PreviewMouseMove += Move, () => tree.PreviewMouseMove -= Move);
            Bind(() => tree.LostMouseCapture += CancelMouse, () => tree.LostMouseCapture -= CancelMouse);
            Bind(() => tree.MouseLeave += CancelMouse, () => tree.MouseLeave -= CancelMouse);
            Bind(() => tree.DragOver += CancelDrag, () => tree.DragOver -= CancelDrag);
            Bind(() => tree.PreviewKeyDown += CancelKey, () => tree.PreviewKeyDown -= CancelKey);
            Bind(() => tree.Unloaded += Unloaded, () => tree.Unloaded -= Unloaded);
        }
        catch
        {
            Detach();
            throw;
        }
    }

    private void Bind(Action add, Action remove) { add(); unsubscribe.Add(remove); }

    internal void Detach()
    {
        gesture.Cancel();
        var errors = new List<Exception>();
        // Native UI objects may already be gone during domain teardown, as in the game's own behaviors.
        if (!NoesisTeardown.InProgress)
            foreach (var remove in unsubscribe)
                try { remove(); } catch (Exception error) { errors.Add(error); }
        unsubscribe.Clear();
        if (errors.Count > 0) throw new AggregateException("Could not detach formation input handlers.", errors);
    }

    private void Down(object sender, MouseButtonEventArgs e) => Guard(() =>
    {
        gesture.Cancel();
        if (e.Handled || e.ChangedButton != MouseButton.Left || e.ClickCount != 1) return;
        var hit = FormationRowHitTest.Find(e.Source as DependencyObject, tree);
        if (hit == null) return;
        var p = e.GetPosition(tree);
        gesture.Begin(hit.Value.Row, hit.Value.Unit, e.ClickCount, tree.Keyboard.Modifiers != ModifierKeys.None, p.X, p.Y);
    });

    private void Up(object sender, MouseButtonEventArgs e) => Guard(() =>
    {
        if (!gesture.Pending) return;
        if (e.Handled || e.ChangedButton != MouseButton.Left) { gesture.Cancel(); return; }
        var hit = FormationRowHitTest.Find(e.Source as DependencyObject, tree);
        var p = e.GetPosition(tree);
        var unit = gesture.Complete(hit?.Row, hit?.Unit, tree.Keyboard.Modifiers != ModifierKeys.None, p.X, p.Y);
        if (unit == null) return;
        var model = Globals._mainGameViewModel
            ?? throw new InvalidOperationException("The current game view model is unavailable.");
        if (!ReferenceEquals(tree.DataContext, model.FormationManager))
            throw new InvalidOperationException("The clicked formation list does not belong to the current battle.");
        FormationRowSelection.Select(model.FormationManager, hit!.Value, selected => GameSelection.Select(model, selected));
    });

    private void Move(object sender, MouseEventArgs e) => Guard(() =>
    {
        if (!gesture.Pending) return;
        var p = e.GetPosition(tree);
        gesture.Move(p.X, p.Y, e.LeftButton == MouseButtonState.Pressed);
    });

    private void CancelMouse(object sender, MouseEventArgs e) => Guard(() => gesture.Cancel());
    private void CancelDrag(object sender, DragEventArgs e) => Guard(() => gesture.Cancel());
    private void CancelKey(object sender, KeyEventArgs e) => Guard(() => gesture.Cancel());
    private void Unloaded(object sender, RoutedEventArgs e) => Guard(() => gesture.Cancel());

    private void Guard(Action action)
    {
        try
        {
            if (NoesisTeardown.InProgress) { gesture.Cancel(); return; }
            action();
            failed = false;
        }
        catch (Exception error)
        {
            gesture.Cancel();
            if (failed) return;
            failed = true;
            ModLog.Error("Formation click failed", error);
        }
    }
}
