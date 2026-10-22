using System;
using System.Windows.Input;

namespace Trackora.Core.ViewModels
{
    public class ButtonCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object>? _canExecute;
        public ButtonCommand(Action execute): this(_ => execute(), null){}
        public ButtonCommand(Action<object> execute): this(execute, null){}
        public ButtonCommand(Action<object> execute, Predicate<object>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) { return _canExecute == null || _canExecute(parameter); }
        public void Execute(object? parameter){ _execute(parameter); }
        public void RaiseCanExecuteChanged(){ CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
}