using System;
using System.Collections.Generic;
using System.Windows.Input;
using OneClickFormationManager;
using static Program;

internal static class SelectionTests
{
    private sealed class Command : ICommand
    {
        internal Func<object?, bool> Available = _ => true;
        internal Action<object?> Action = _ => { };
        public bool CanExecute(object? parameter) => Available(parameter);
        public void Execute(object? parameter) => Action(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    internal static void Run()
    {
        Check("native command availability, parameter and execution order are honored", () =>
        {
            var calls = new List<string>(); var unit = new object();
            var command = new Command
            {
                Available = p => { Require(ReferenceEquals(p, unit), "Availability received another unit."); calls.Add("available"); return true; },
                Action = p => { Require(ReferenceEquals(p, unit), "The command received another unit."); calls.Add("execute"); }
            };
            SelectionCommand.Execute(command, unit, () => calls.Add("selection"));
            Require(string.Join(",", calls) == "available,execute,selection", "The native command was bypassed or reordered.");
        });
        Check("unavailable commands do not move the camera or synchronize selection", () =>
        {
            bool executed = false, synchronized = false;
            var command = new Command { Available = _ => false, Action = _ => executed = true };
            SelectionCommand.Execute(command, new object(), () => synchronized = true);
            Require(!executed && !synchronized, "An unavailable command performed an action.");
        });
        Check("failed native availability or execution does not perform a selection repair", () =>
        {
            bool synchronized = false;
            var command = new Command { Available = _ => throw new InvalidOperationException("availability") };
            Throws<InvalidOperationException>(() => SelectionCommand.Execute(command, new object(), () => synchronized = true));
            command.Available = _ => true;
            command.Action = _ => throw new InvalidOperationException("execute");
            Throws<InvalidOperationException>(() => SelectionCommand.Execute(command, new object(), () => synchronized = true));
            Require(!synchronized, "Selection was repaired after a failed native command.");
        });
        Check("selection failures remain observable to the callback error boundary", () =>
        {
            Throws<InvalidOperationException>(() => SelectionCommand.Execute(new Command(), new object(),
                () => throw new InvalidOperationException("selection")));
        });

        var native = SelectionGameFixture.ReadNativeCommand();
        Check("the actual game command reproduces stale same-unit selection", () =>
        {
            var ship = new SelectionGameFixture.Unit();
            SelectionGameFixture.Current = new SelectionGameFixture.Scene(ship);
            var contact = new object();
            SelectionGameFixture.Current.Panel = SelectionGameFixture.Current.MapSelection = contact;
            native(ship);
            Require(ReferenceEquals(SelectionGameFixture.Current.Panel, contact), "The original stale-panel scenario no longer reproduces.");
        });
        Check("single-click command restores same-unit selection without an extra camera move", () =>
        {
            var ship = new SelectionGameFixture.Unit();
            var scene = SelectionGameFixture.Current = new SelectionGameFixture.Scene(ship);
            scene.Panel = scene.MapSelection = new object();
            var command = new Command { Action = native };
            SelectionCommand.Execute(command, ship, () => scene.Select(ship));
            Require(ReferenceEquals(scene.Panel, ship) && ReferenceEquals(scene.MapSelection, ship), "The unit was not restored.");
            Require(scene.CameraChanges == 0 && scene.SelectionChanges == 1, "The camera or selection changed twice.");
        });
        Check("native camera switching is preserved with the original command flags", () =>
        {
            var first = new SelectionGameFixture.Unit(); var next = new SelectionGameFixture.Unit();
            var scene = SelectionGameFixture.Current = new SelectionGameFixture.Scene(first);
            SelectionCommand.Execute(new Command { Action = native }, next, () => scene.Select(next));
            Require(ReferenceEquals(scene.Camera, next) && ReferenceEquals(scene.Panel, next), "Camera/selection did not switch.");
            Require(scene.Arguments == (false, false, false), "Native camera flags changed.");
            Require(scene.CameraChanges == 1 && scene.SelectionChanges == 0, "Already-synchronized selection was changed again.");
        });
        Check("an existing command wrapper's reselection is not duplicated", () =>
        {
            var ship = new SelectionGameFixture.Unit();
            var scene = SelectionGameFixture.Current = new SelectionGameFixture.Scene(ship);
            scene.Panel = scene.MapSelection = new object();
            var wrapped = new Command { Action = p => { native(p); scene.Select(p); } };
            SelectionCommand.Execute(wrapped, ship, () => scene.Select(ship));
            Require(scene.SelectionChanges == 1 && ReferenceEquals(scene.Panel, ship), "The wrapper repair was lost or duplicated.");
        });
    }
}
