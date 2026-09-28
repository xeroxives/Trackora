using System;
using System.Windows.Input;

namespace WpfApp14.ViewModels
{
	public class ButtonCommand : ICommand
	{
		private readonly Action _action;
		public ButtonCommand(Action action) => _action = action;
		public event EventHandler CanExecuteChanged;
		public bool CanExecute(object parameter) => true;
		public void Execute(object parameter) => _action();
	}
}