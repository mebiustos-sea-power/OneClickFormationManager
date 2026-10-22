using System;

namespace OneClickFormationManager;

// Wait for release so a drag never changes the selected unit or camera.
internal sealed class ClickGesture<T> where T : class
{
    private object? row;
    private T? target;
    private float startX, startY;
    internal bool Pending => target != null;

    internal void Begin(object? hitRow, T? hitTarget, int clickCount, bool modified, float x, float y)
    {
        Cancel();
        if (hitRow == null || hitTarget == null || clickCount != 1 || modified || !Finite(x, y)) return;
        row = hitRow;
        target = hitTarget;
        startX = x;
        startY = y;
    }

    internal void Move(float x, float y, bool leftPressed)
    {
        if (!leftPressed || !WithinClickDistance(x, y)) Cancel();
    }

    internal T? Complete(object? hitRow, T? hitTarget, bool modified, float x, float y)
    {
        T? result = !modified && ReferenceEquals(row, hitRow) && ReferenceEquals(target, hitTarget)
            && WithinClickDistance(x, y) ? target : null;
        Cancel();
        return result;
    }

    internal void Cancel() { row = null; target = null; }

    private bool WithinClickDistance(float x, float y)
    {
        float dx = x - startX, dy = y - startY;
        // Both native formation drag behaviors start at five pixels of movement.
        return Finite(x, y) && dx * dx + dy * dy < 25;
    }

    private static bool Finite(float x, float y) =>
        !float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y);
}
