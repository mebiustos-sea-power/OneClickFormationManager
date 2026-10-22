using OneClickFormationManager;
using static Program;

internal static class GestureTests
{
    internal static void Run()
    {
        var row = new object(); var unit = new object();
        var gesture = new ClickGesture<object>();
        Check("release over the same unit selects it once, allowing small pointer jitter", () =>
        {
            gesture.Begin(row, unit, 1, false, 10, 20);
            Require(gesture.Pending, "The click was not armed.");
            gesture.Move(12, 21, true);
            Require(ReferenceEquals(gesture.Complete(row, unit, false, 12, 21), unit), "The unit was not selected.");
            Require(gesture.Complete(row, unit, false, 12, 21) == null, "The release selected twice.");
        });
        Check("five-pixel drag does not select, even after returning to the starting point", () =>
        {
            gesture.Begin(row, unit, 1, false, 0, 0);
            gesture.Move(3, 4, true);
            gesture.Move(0, 0, true);
            Require(gesture.Complete(row, unit, false, 0, 0) == null, "A completed drag became a click.");
        });
        Check("release movement rejects a drag when no intermediate move was delivered", () =>
        {
            gesture.Begin(row, unit, 1, false, 0, 0);
            Require(gesture.Complete(row, unit, false, 5, 0) == null, "A drag was selected on release.");
        });
        Check("release over another row or a replaced unit does not select", () =>
        {
            gesture.Begin(row, unit, 1, false, 0, 0);
            Require(gesture.Complete(new object(), unit, false, 0, 0) == null, "A different row was accepted.");
            gesture.Begin(row, unit, 1, false, 0, 0);
            Require(gesture.Complete(row, new object(), false, 0, 0) == null, "A replaced unit was accepted.");
        });
        Check("excluded controls and empty targets do not select or retain an earlier press", () =>
        {
            gesture.Begin(row, unit, 1, false, 0, 0);
            gesture.Begin(null, null, 1, false, 0, 0);
            Require(gesture.Complete(row, unit, false, 0, 0) == null, "The excluded press retained an old unit.");
            gesture.Begin(row, null, 1, false, 0, 0);
            Require(!gesture.Pending, "An empty station was armed.");
        });
        Check("a modified press or release leaves selection to native input", () =>
        {
            gesture.Begin(row, unit, 1, true, 0, 0);
            Require(gesture.Complete(row, unit, false, 0, 0) == null, "A modified press selected a unit.");
            gesture.Begin(row, unit, 1, false, 0, 0);
            Require(gesture.Complete(row, unit, true, 0, 0) == null, "A modified release selected a unit.");
        });
        Check("the second double-click press adds no selection to the native double-click action", () =>
        {
            gesture.Begin(row, unit, 1, false, 0, 0);
            Require(ReferenceEquals(gesture.Complete(row, unit, false, 0, 0), unit), "The first click did not select.");
            gesture.Begin(row, unit, 2, false, 0, 0);
            Require(gesture.Complete(row, unit, false, 0, 0) == null, "The second press added a click action.");
        });
        Check("capture loss, leaving, unloading and cancellation cannot cause a later stale selection", () =>
        {
            gesture.Begin(row, unit, 1, false, 0, 0);
            gesture.Cancel();
            Require(gesture.Complete(row, unit, false, 0, 0) == null, "A canceled gesture survived.");
            gesture.Begin(row, unit, 1, false, 0, 0);
            gesture.Move(0, 0, false);
            Require(gesture.Complete(row, unit, false, 0, 0) == null, "A released mouse kept an armed click.");
        });
        Check("the same row can be selected again without a changed-item event", () =>
        {
            for (int i = 0; i < 2; i++)
            {
                gesture.Begin(row, unit, 1, false, 0, 0);
                Require(ReferenceEquals(gesture.Complete(row, unit, false, 0, 0), unit), "A repeated click was ignored.");
            }
        });
        Check("separate formation lists cannot consume each other's press", () =>
        {
            var other = new ClickGesture<object>();
            gesture.Begin(row, unit, 1, false, 0, 0);
            Require(other.Complete(row, unit, false, 0, 0) == null, "Another list consumed a press.");
            Require(ReferenceEquals(gesture.Complete(row, unit, false, 0, 0), unit), "The owning list lost its press.");
        });
    }
}
