using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;
using GamepadEmulator.Services;
using GamepadEmulator.ViewModels;

namespace GamepadEmulator.Views
{
    public partial class ModernMainWindow : Window
    {
        private MainViewModel _viewModel;

        public ModernMainWindow()
        {
            InitializeComponent();
            
            // Get InputOrchestrator from App
            var inputOrchestrator = (InputOrchestrator)System.Windows.Application.Current.Properties["InputOrchestrator"]!;
            _viewModel = new MainViewModel(inputOrchestrator);
            DataContext = _viewModel;

            // Removed references to ControllerView since we're not using it anymore
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void HideToTray_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void HomeTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to home view
            MappingConfigPanel.Visibility = Visibility.Collapsed;
        }

        private void ConfigTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to config view
            MappingConfigPanel.Visibility = Visibility.Visible;
        }

        private void CrosshairTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to crosshair view
            MappingConfigPanel.Visibility = Visibility.Collapsed;
        }

        private void ScriptsTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to scripts view
            MappingConfigPanel.Visibility = Visibility.Collapsed;
        }

        private void CloseMappingConfig_Click(object sender, RoutedEventArgs e)
        {
            MappingConfigPanel.Visibility = Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}