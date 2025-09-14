using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GamepadEmulator.Services;

namespace GamepadEmulator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly InputOrchestrator _inputOrchestrator;
        private bool _isInterceptorLoaded;
        private bool _isControllerConnected;
        private bool _isRunning;
        private bool _isPaused = true;

        // New properties for Modern UI
        private double _deadZone = 5.0;
        private double _horizontalSensitivity = 1.0;
        private double _verticalSensitivity = 1.0;
        private double _doubleMvm = 0.0;
        private double _noiseFilter = 15.0;
        private bool _isExponentialCurve = true;
        private bool _isReverseModeOn;
        private bool _areKeysBlocked;

        private short _leftStickX;
        private short _leftStickY;
        private short _rightStickX;
        private short _rightStickY;

        public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();

        public bool IsInterceptorLoaded
        {
            get => _isInterceptorLoaded;
            set => SetProperty(ref _isInterceptorLoaded, value);
        }

        public bool IsControllerConnected
        {
            get => _isControllerConnected;
            set => SetProperty(ref _isControllerConnected, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public double DeadZone
        {
            get => _deadZone;
            set
            {
                if (SetProperty(ref _deadZone, value))
                    _inputOrchestrator.SetDeadZone(value);
            }
        }

        public double HorizontalSensitivity
        {
            get => _horizontalSensitivity;
            set
            {
                if (SetProperty(ref _horizontalSensitivity, value))
                    _inputOrchestrator.SetHorizontalSensitivity(value);
            }
        }

        public double VerticalSensitivity
        {
            get => _verticalSensitivity;
            set
            {
                if (SetProperty(ref _verticalSensitivity, value))
                    _inputOrchestrator.SetVerticalSensitivity(value);
            }
        }

        public double DoubleMvm
        {
            get => _doubleMvm;
            set => SetProperty(ref _doubleMvm, value); // No backend logic for this yet
        }

        public double NoiseFilter
        {
            get => _noiseFilter;
            set
            {
                if (SetProperty(ref _noiseFilter, value))
                    _inputOrchestrator.SetNoiseFilter(value);
            }
        }

        public bool IsExponentialCurve
        {
            get => _isExponentialCurve;
            set
            {
                if (SetProperty(ref _isExponentialCurve, value))
                    _inputOrchestrator.SetIsExponentialCurve(value);
            }
        }

        public bool IsReverseModeOn
        {
            get => _isReverseModeOn;
            set
            {
                if (SetProperty(ref _isReverseModeOn, value))
                    _inputOrchestrator.SetIsReverseModeOn(value);
            }
        }

        public bool AreKeysBlocked
        {
            get => _areKeysBlocked;
            set
            {
                if (SetProperty(ref _areKeysBlocked, value))
                    _inputOrchestrator.SetAreKeysBlocked(value);
            }
        }

        public short LeftStickX
        {
            get => _leftStickX;
            set => SetProperty(ref _leftStickX, value);
        }

        public short LeftStickY
        {
            get => _leftStickY;
            set => SetProperty(ref _leftStickY, value);
        }

        public short RightStickX
        {
            get => _rightStickX;
            set => SetProperty(ref _rightStickX, value);
        }

        public short RightStickY
        {
            get => _rightStickY;
            set => SetProperty(ref _rightStickY, value);
        }

        public string StatusText => 
            $"Interceptor: {(IsInterceptorLoaded ? "✓" : "✗")} | " +
            $"Controller: {(IsControllerConnected ? "✓" : "✗")} | " +
            $"Status: {(IsRunning ? (IsPaused ? "Paused" : "Running") : "Stopped")}";

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand PauseResumeCommand { get; }
        public ICommand PanicCommand { get; }

        public MainViewModel(InputOrchestrator inputOrchestrator)
        {
            _inputOrchestrator = inputOrchestrator;

            StartCommand = new RelayCommand(async () => await StartAsync(), () => !IsRunning);
            StopCommand = new RelayCommand(async () => await StopAsync(), () => IsRunning);
            PauseResumeCommand = new RelayCommand(() => TogglePause(), () => IsRunning);
            PanicCommand = new RelayCommand(async () => await PanicAsync());

            // Wire up events
            _inputOrchestrator.LogMessage += OnLogMessage;
            _inputOrchestrator.RunningStateChanged += state => Application.Current.Dispatcher.InvokeAsync(() => IsRunning = state);
            _inputOrchestrator.PausedStateChanged += state => Application.Current.Dispatcher.InvokeAsync(() => IsPaused = state);
            _inputOrchestrator.ReverseModeToggled += () => Application.Current.Dispatcher.InvokeAsync(() => IsReverseModeOn = !IsReverseModeOn);
            _inputOrchestrator.BlockKeysToggled += () => Application.Current.Dispatcher.InvokeAsync(() => AreKeysBlocked = !AreKeysBlocked);

            // Initialize
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            AddLogMessage("Initializing gamepad emulator...");
            
            bool initialized = _inputOrchestrator.Initialize();
            IsInterceptorLoaded = initialized;
            IsControllerConnected = initialized;
            
            if (initialized)
            {
                AddLogMessage("Gamepad emulator initialized successfully.");
                AddLogMessage("Ready to start. Press 'Start' to begin input monitoring.");
                AddLogMessage("Controls: INSERT = Pause/Resume, P = Panic/Stop");
            }
            else
            {
                AddLogMessage("Failed to initialize. Check that drivers are installed.");
            }
        }

        private async Task StartAsync()
        {
            if (await _inputOrchestrator.Start())
            {
                AddLogMessage("Input monitoring started successfully.");
            }
            else
            {
                AddLogMessage("Failed to start input monitoring.");
            }
        }

        private async Task StopAsync()
        {
            await _inputOrchestrator.Stop();
            AddLogMessage("Input monitoring stopped.");
        }

        private void TogglePause()
        {
            _inputOrchestrator.SetPaused(!IsPaused);
        }

        private async Task PanicAsync()
        {
            await _inputOrchestrator.Stop();
            AddLogMessage("PANIC: All input processing stopped!");
        }

        private void OnLogMessage(string message)
        {
            Application.Current.Dispatcher.InvokeAsync(() => AddLogMessage(message));
        }

        private void AddLogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogMessages.Add($"[{timestamp}] {message}");
            
            // Keep only last 100 messages
            while (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(0);
            }

            OnPropertyChanged(nameof(StatusText));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}