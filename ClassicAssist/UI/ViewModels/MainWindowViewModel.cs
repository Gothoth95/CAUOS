using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Assistant;
using ClassicAssist.Data;
using ClassicAssist.Shared.Resources;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.Views;
using Hardcodet.Wpf.TaskbarNotification;

namespace ClassicAssist.UI.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private ICommand _debugCommand;
        private DebugWindow _debugWindow;
        private ICommand _minimizeCommand;
        private ICommand _restoreWindowCommand;
        private ICommand _maximizeWindowCommand;
        private string _status = Strings.Ready___;
        private TaskbarIcon _taskbarIcon;
        private string _title = Strings.ProductName;

        public MainWindowViewModel()
        {
            OptionsTab = new OptionsTabViewModel();
            Engine.Dispatcher = Dispatcher.CurrentDispatcher;
            Engine.UpdateWindowTitleEvent += OnUpdateWindowTitleEvent;
            Engine.ClientClosing += OnClientClosing;

            NavEntries = new List<MainNavEntry>
            {
                new MainNavEntry( 0, "\u2302", Strings.General ),
                new MainNavEntry( 1, "\u2699", Strings.Options ),
                new MainNavEntry( 2, "\u2328", Strings.Hotkeys ),
                new MainNavEntry( 3, "\u2726", Strings.Macros ),
                new MainNavEntry( 4, "\u26A1", Strings.Skills ),
                new MainNavEntry( 5, "\u2692", Strings.Agents ),
                new MainNavEntry( 6, "\u2601", Strings.Public_Macros ),
                new MainNavEntry( 7, "\u23F3", Strings.Timers ),
                new MainNavEntry( 8, "\u2694", Strings.PvP ),
                new MainNavEntry( 9, "\u2139", Strings.About )
            };
        }

        public IReadOnlyList<MainNavEntry> NavEntries { get; }

        public OptionsTabViewModel OptionsTab { get; }

        public ICommand DebugCommand =>
            _debugCommand ?? ( _debugCommand = new RelayCommand( ShowDebugWindow, o => true ) );

        public ICommand MinimizeCommand =>
            _minimizeCommand ?? ( _minimizeCommand = new RelayCommand( Minimize, o => true ) );

        public ICommand RestoreWindowCommand =>
            _restoreWindowCommand ?? ( _restoreWindowCommand = new RelayCommand( RestoreWindow, o => true ) );

        public ICommand MaximizeWindowCommand =>
            _maximizeWindowCommand ?? ( _maximizeWindowCommand = new RelayCommand( MaximizeOrRestoreWindow, o => o is Window ) );

        public string Status
        {
            get => _status;
            set => SetProperty( ref _status, value );
        }

        public string Title
        {
            get => _title;
            set => SetProperty( ref _title, value );
        }

        private void OnClientClosing()
        {
            if ( _taskbarIcon != null )
            {
                _dispatcher.Invoke( () => { _taskbarIcon.Visibility = Visibility.Hidden; } );
            }
        }

        private void OnUpdateWindowTitleEvent()
        {
            Title = string.IsNullOrEmpty( Engine.Player?.Name )
                ? Strings.ProductName
                : $"{Engine.Player?.Name} - {( Options.CurrentOptions.ShowProfileNameWindowTitle ? $"({Options.CurrentOptions.Name}) - " : "" )}{Strings.ProductName}";

            if ( _taskbarIcon != null )
            {
                _dispatcher.Invoke( () => { _taskbarIcon.ToolTipText = Title; } );
            }
        }

        private void ShowDebugWindow( object obj )
        {
            _debugWindow = new DebugWindow();
            _debugWindow.Show();
        }

        private static void MaximizeOrRestoreWindow( object obj )
        {
            if ( !( obj is Window window ) )
            {
                return;
            }

            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Minimize( object obj )
        {
            if ( !( obj is Window window ) )
            {
                return;
            }

            if ( !Options.CurrentOptions.SysTray )
            {
                window.WindowState = WindowState.Minimized;
                return;
            }

            if ( _taskbarIcon == null )
            {
                _taskbarIcon = new TaskbarIcon
                {
                    Icon = Properties.Resources.logo,
                    Visibility = Visibility.Visible,
                    LeftClickCommand = RestoreWindowCommand,
                    LeftClickCommandParameter = window,
                    NoLeftClickDelay = true
                };
            }

            _taskbarIcon.ToolTipText = Title;
            _taskbarIcon.Visibility = Visibility.Visible;
            window.ShowInTaskbar = false;
            window.Hide();
        }

        private void RestoreWindow( object obj )
        {
            if ( !( obj is Window window ) )
            {
                return;
            }

            window.ShowInTaskbar = true;
            window.WindowState = WindowState.Normal;
            window.Show();
            _taskbarIcon.Visibility = Visibility.Hidden;
        }
    }
}