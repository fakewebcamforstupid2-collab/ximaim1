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
            // Switch to home view
        }

        private void ConfigTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to config view
        }

        private void CrosshairTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to crosshair view
        }

        private void ScriptsTab_Click(object sender, RoutedEventArgs e)
        {
            // Switch to scripts view
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }

    public class ValueToAngleConverter : IValueConverter
    {
        public static ValueToAngleConverter Instance { get; } = new ValueToAngleConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Convert slider value (0-100) to angle (-135 to +135 degrees)
                return (doubleValue / 100.0) * 270 - 135;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public static InverseBoolConverter Instance { get; } = new InverseBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}