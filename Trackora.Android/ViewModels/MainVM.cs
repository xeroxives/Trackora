using System;
using System.Windows.Input;
using Trackora.Core.Interfaces;
using Trackora.Core.ViewModels;
using Trackora.Core.Utils;

namespace Trackora.Android.ViewModels;

public class MainVM : VM
{
    #region Privates
    private VM _currentVM;
    private MainPageVM _mainPageVM;
    private readonly I_AudioService _audioService;
    private readonly I_UITimer _uiTimer;
    private readonly I_UiThread _uiThread;
    private readonly I_PathProvider _pathProvider;
    private readonly I_FilePicker _filePicker;
    private readonly I_OsActions _osActions;
    private readonly I_DialogService _dialogService;
    private readonly I_FileHasher _fileHasher;
    private readonly I_DbService _dbService;
    #endregion

    #region Getters
    public VM CurrentVM
    {
        get => _currentVM;
        set { _currentVM = value; Notify(nameof(CurrentVM)); }
    }
    #endregion

    #region Navigation Methods
    private void ToMainPage()
    {
        if (_mainPageVM == null)
        {
            _mainPageVM = new MainPageVM(
                    _audioService,
                    _uiTimer,
                    _uiThread,
                    _pathProvider,
                    _filePicker,
                    _osActions,
                    _dialogService,
                    _fileHasher,
                    _dbService);
        }
        CurrentVM = _mainPageVM;
    }

    private async void ToSettings()
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Settings", "Be implemented soon", "OK");
        }
    }

    private async void ToStats()
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert("Stats", "Be implemented soon", "OK");
        }
    }
    #endregion

    #region Buttons
    public ICommand MainPageCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand StatsCommand { get; }
    #endregion

    #region Constructor

    public MainVM(
        I_AudioService audioService,
        I_UITimer uiTimer,
        I_UiThread uiThread,
        I_PathProvider pathProvider,
        I_FilePicker filePicker,
        I_OsActions osActions,
        I_DialogService dialogService,
        I_FileHasher fileHasher,
        I_DbService dbService)
    {
        _audioService = audioService;
        _uiTimer = uiTimer;
        _uiThread = uiThread;
        _pathProvider = pathProvider;
        _filePicker = filePicker;
        _osActions = osActions;
        _dialogService = dialogService;
        _fileHasher = fileHasher;
        _dbService = dbService;

        Logger.Log($"\n\n==============================\nВерсия: {Environment.Version}");
        Logger.Log($"Описание: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}\n==============================\n\n");

        MainPageCommand = new ButtonCommand(ToMainPage);
        SettingsCommand = new ButtonCommand(ToSettings);
        StatsCommand = new ButtonCommand(ToStats);

        ToMainPage();
    }

    #endregion
}
