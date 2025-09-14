using System.Windows;
using GamepadEmulator.Services;
using GamepadEmulator.ViewModels;

namespace GamepadEmulator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Get InputOrchestrator from App
            var inputOrchestrator = (InputOrchestrator)Application.Current.Properties["InputOrchestrator"]!;
            DataContext = new MainViewModel(inputOrchestrator);
        }
    }
}