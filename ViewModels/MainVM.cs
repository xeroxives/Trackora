using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp14.Converters;
using WpfApp14.Models;
using WpfApp14.Services;
using WpfApp14.Utils;
using WpfApp14.Views;

namespace WpfApp14.ViewModels
{
    public class MainVM : VM
    {
        #region Privates
        private VM _currentVM;
        private MainPageVM _mainPageVM;
        private SettingsVM _settingsVM;
        //private StatsVM _StatsVM;
        #endregion

        #region Getters
        public VM CurrentVM
        {
            get => _currentVM;
            set{_currentVM = value;Notify(nameof(CurrentVM));}
        }
        #endregion

        #region Navigation Methods
        private void ToMainPage()
        {
            if (_mainPageVM == null)
            {
                _mainPageVM = new MainPageVM();
            }
            CurrentVM = _mainPageVM;
        }
        private void ToSettings()
        {
            // if (_settingsVM == null)
            // {
                // _settingsVM = new SettingsVM();
            // }
            // CurrentVM = _settingsVM;
            MessageBox.Show("Be implemented soon","Settings");
        }
        private void ToStats()
        {
            //if (_settingsVM == null)
            //{
            //    _settingsVM = new SettingsVM();
            //}
            //CurrentVM = _settingsVM;
            MessageBox.Show("Be implemented soon", "Stats");
        }
        #endregion

        #region Buttons
        public ICommand MainPageCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand StatsCommand { get; }
        #endregion

        #region Constructor

        public MainVM()
        {
            MainPageCommand = new ButtonCommand(ToMainPage);
            SettingsCommand = new ButtonCommand(ToSettings);
            StatsCommand = new ButtonCommand(ToStats);

            ToMainPage();
        }

        #endregion
    }
}