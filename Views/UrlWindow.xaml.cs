using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp14.ViewModels;

namespace WpfApp14.Views
{
    /// <summary>
    /// Логика взаимодействия для UrlWindow.xaml
    /// </summary>
    public partial class UrlWindow : Window
    {
        public UrlWindow()
        {
            InitializeComponent();
            var vm = DataContext as UrlWindowVM;
            if (vm != null)
                vm.PropertyChanged += (s, e) =>
                    this.DialogResult = vm.DialogResult;
        }
    }
}
