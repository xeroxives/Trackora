using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfApp14.Models;
using WpfApp14.ViewModels;
using WpfApp14.Views;

namespace WpfApp14.Services
{
    public class DialogService
    {
        private readonly Window _owner;
        public DialogService(Window owner) => _owner = owner;

        public void ShowEditMetadata(Song song)
        {
            var vm = new EditMetadataVM(song);
            var window = new EditMetadataWindow(vm) { Owner = _owner };
            window.Show(); // Или ShowDialog(), если хотите блокировать основное окно
        }
    }
}
