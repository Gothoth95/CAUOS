using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ClassicAssist.UI.Views
{
    public partial class PvpTabControl : UserControl
    {
        private readonly Dictionary<string, FrameworkElement> _sections = new Dictionary<string, FrameworkElement>();

        public PvpTabControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded( object sender, RoutedEventArgs e )
        {
            Loaded -= OnLoaded;
            ShowSection( GetActiveSectionKey() );
        }

        private void OnSectionNav( object sender, RoutedEventArgs e )
        {
            if ( !IsLoaded )
            {
                return;
            }

            ShowSection( GetActiveSectionKey() );
        }

        private string GetActiveSectionKey()
        {
            if ( NavColors.IsChecked == true )
            {
                return "colors";
            }

            if ( NavGuard.IsChecked == true )
            {
                return "guard";
            }

            if ( NavPvpCycle.IsChecked == true )
            {
                return "cycle";
            }

            if ( NavFriendly.IsChecked == true )
            {
                return "friendly";
            }

            if ( NavRehue.IsChecked == true )
            {
                return "rehue";
            }

            return "target";
        }

        private void ShowSection( string key )
        {
            if ( !_sections.TryGetValue( key, out FrameworkElement section ) )
            {
                section = (FrameworkElement) Application.LoadComponent( new Uri( GetSectionUri( key ), UriKind.Relative ) );
                section.DataContext = DataContext;
                _sections[key] = section;
            }

            SectionHost.Content = section;
        }

        private static string GetSectionUri( string key )
        {
            switch ( key )
            {
                case "colors":
                    return "/ClassicAssist;component/UI/Views/PvpSections/PvpColorsSection.xaml";
                case "guard":
                    return "/ClassicAssist;component/UI/Views/PvpSections/PvpGuardSection.xaml";
                case "cycle":
                    return "/ClassicAssist;component/UI/Views/PvpSections/PvpCycleSection.xaml";
                case "friendly":
                    return "/ClassicAssist;component/UI/Views/PvpSections/PvpFriendlySection.xaml";
                case "rehue":
                    return "/ClassicAssist;component/UI/Views/PvpSections/PvpRehueSection.xaml";
                default:
                    return "/ClassicAssist;component/UI/Views/PvpSections/PvpTargetSection.xaml";
            }
        }
    }
}
