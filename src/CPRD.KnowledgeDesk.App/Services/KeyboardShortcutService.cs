using System.Windows;
using System.Windows.Input;

namespace CPRD.KnowledgeDesk.App.Services;

public sealed class KeyboardShortcutService
{
    public void Register(
        Window window,
        Action focusSearch,
        Action openQuickCapture,
        ICommand saveCommand,
        ICommand closeWorkspaceCommand,
        ICommand toggleFavoriteCommand)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.InputBindings.Add(new KeyBinding(new DelegateCommand(openQuickCapture), Key.N, ModifierKeys.Control));
        window.InputBindings.Add(new KeyBinding(new DelegateCommand(openQuickCapture), Key.N, ModifierKeys.Control | ModifierKeys.Shift));
        window.InputBindings.Add(new KeyBinding(new DelegateCommand(focusSearch), Key.K, ModifierKeys.Control));
        window.InputBindings.Add(new KeyBinding(new DelegateCommand(focusSearch), Key.F, ModifierKeys.Control));
        window.InputBindings.Add(new KeyBinding(saveCommand, Key.S, ModifierKeys.Control));
        window.InputBindings.Add(new KeyBinding(closeWorkspaceCommand, Key.W, ModifierKeys.Control));
        window.InputBindings.Add(new KeyBinding(toggleFavoriteCommand, Key.F, ModifierKeys.Control | ModifierKeys.Shift));
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;

        public DelegateCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }
}
