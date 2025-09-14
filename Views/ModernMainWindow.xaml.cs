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

            // Subscribe to stick updates
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.LeftStickX) || e.PropertyName == nameof(_viewModel.LeftStickY))
                {
                    ControllerView.UpdateLeftStick(_viewModel.LeftStickX, _viewModel.LeftStickY);
                }
                else if (e.PropertyName == nameof(_viewModel.RightStickX) || e.PropertyName == nameof(_viewModel.RightStickY))
                {
                    ControllerView.UpdateRightStick(_viewModel.RightStickX, _viewModel.RightStickY);
                }
            };

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
            HomeContent.Visibility = Visibility.Visible;
            ConfigContent.Visibility = Visibility.Collapsed;
            LogContent.Visibility = Visibility.Collapsed;
        }

        private void ConfigTab_Click(object sender, RoutedEventArgs e)
        {
            HomeContent.Visibility = Visibility.Collapsed;
            ConfigContent.Visibility = Visibility.Visible;
            LogContent.Visibility = Visibility.Collapsed;
        }

        private void LogTab_Click(object sender, RoutedEventArgs e)
        {
            HomeContent.Visibility = Visibility.Collapsed;
            ConfigContent.Visibility = Visibility.Collapsed;
            LogContent.Visibility = Visibility.Visible;
        }

        private void CrosshairTab_Click(object sender, RoutedEventArgs e)
        {
            // Not implemented
        }

        private void ScriptsTab_Click(object sender, RoutedEventArgs e)
        {
            // Not implemented
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}