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

namespace WpfApp14.Views
{
    public partial class EditMetadataWindow : Window
    {
        private object _data = null;
        public EditMetadataWindow(object o)
        {
            _data = o;
            InitializeComponent();
            //tb.Text = _data.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            
        }
    }
}
