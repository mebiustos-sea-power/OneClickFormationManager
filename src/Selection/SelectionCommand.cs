using System;
using System.Windows.Input;

namespace OneClickFormationManager;

internal static class SelectionCommand
{
    internal static void Execute(ICommand command, object unit, Action synchronize)
    {
        if (!command.CanExecute(unit)) return;
        command.Execute(unit);
        synchronize();
    }
}
