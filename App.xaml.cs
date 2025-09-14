using System.Windows;
using GamepadEmulator.Services;

namespace GamepadEmulator
{
    public partial class App : Application
    {
        private InputOrchestrator? _inputOrchestrator;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            _inputOrchestrator = new InputOrchestrator();
            
            // Store reference for main window
            Current.Properties["InputOrchestrator"] = _inputOrchestrator;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _inputOrchestrator?.Dispose();
            base.OnExit(e);
        }
    }
}