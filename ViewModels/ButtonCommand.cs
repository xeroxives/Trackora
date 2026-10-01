using System;
using System.Windows.Input;

namespace WpfApp14.ViewModels
{
    public class ButtonCommand : ICommand
    {
        private readonly Action<object> _action;

        // Конструктор для методов С параметром (для контекстного меню)
        public ButtonCommand(Action<object> action) => _action = action;

        // Конструктор для методов БЕЗ параметра (для старых кнопок Play/Import)
        // Чтобы не ломать существующий код, добавим перегрузку или адаптер
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