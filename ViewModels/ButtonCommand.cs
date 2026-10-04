using System;
using System.Windows.Input;

namespace WpfApp14.ViewModels
{
    public class ButtonCommand : ICommand
    {
        private readonly Action<object> _action;
        public ButtonCommand(Action<object> action) => _action = action;
        public ButtonCommand(Action action) => _action = _ => action();

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _action(parameter);
    }
}