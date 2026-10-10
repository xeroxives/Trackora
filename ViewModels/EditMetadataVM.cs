using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp14.Models;

namespace WpfApp14.ViewModels
{
    public class EditMetadataVM:VM
    {
        #region Getters
        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; Notify(nameof(Title)); }
        }
        private string _author;
        public string Author
        {
            get => _author;
            set { _author = value; Notify(nameof(Author)); }
        }

        #endregion
        private readonly Song _song;
        public EditMetadataVM() { }
        public EditMetadataVM(Song s)
        {
            _song = s;
        }
    }
}
