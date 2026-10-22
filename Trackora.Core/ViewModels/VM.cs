using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Trackora.Core.ViewModels
{
	public abstract class VM : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void Notify([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}