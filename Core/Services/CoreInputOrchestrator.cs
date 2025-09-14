using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GamepadEmulator.Core.Abstractions;

namespace GamepadEmulator.Core.Services
{
    public class CoreInputOrchestrator : IDisposable
    {
        private readonly IInputCaptureService _inputService;
        private readonly IGamepadOutputService _gamepadService;
        private readonly MappingService _mappingService;
        private readonly Timer _inactivityTimer;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _monitoringTask;
        private DateTime _lastMouseActivity = DateTime.Now;
        private bool _disposed;

        public bool IsPaused { get; private set; } = true;
        public bool IsRunning { get; private set; }

        public event Action<string>? LogMessage;
        public event Action<bool>? PausedStateChanged;
        public event Action<bool>? RunningStateChanged;

        public CoreInputOrchestrator(IInputCaptureService inputService, IGamepadOutputService gamepadService)
        {
            _inputService = inputService;
            _gamepadService = gamepadService;
            _mappingService = new MappingService();

            _inactivityTimer = new Timer(CheckMouseInactivity, null, Timeout.Infinite, Timeout.Infinite);

            // Wire up events
            _inputService.LogMessage += message => LogMessage?.Invoke(message);
            _gamepadService.LogMessage += message => LogMessage?.Invoke(message);

            _inputService.KeyStrokeReceived += OnKeyStrokeReceived;
            _inputService.MouseStrokeReceived += OnMouseStrokeReceived;

            _mappingService.ButtonStateChanged += (button, pressed) =>
            {
                if (!IsPaused)
                {
                    _gamepadService.SetButtonState(button, pressed);
                    _gamepadService.SubmitReport();
                }
            };

            _mappingService.LeftStickChanged += (x, y) =>
            {
                if (!IsPaused)
                {
                    _gamepadService.SetAxisValue(GamepadAxis.LeftThumbX, x);
                    _gamepadService.SetAxisValue(GamepadAxis.LeftThumbY, y);
                    _gamepadService.SubmitReport();
                }
            };

            _mappingService.RightStickChanged += (x, y) =>
            {
                if (!IsPaused)
                {
                    _gamepadService.SetAxisValue(GamepadAxis.RightThumbX, x);
                    _gamepadService.SetAxisValue(GamepadAxis.RightThumbY, y);
                    _gamepadService.SubmitReport();
                }
            };
        }

        public bool Initialize()
        {
            if (!_inputService.Initialize())
            {
                LogMessage?.Invoke("Failed to initialize input capture service.");
                return false;
            }

            if (!_gamepadService.Initialize())
            {
                LogMessage?.Invoke("Failed to initialize gamepad output service.");
                return false;
            }

            LogMessage?.Invoke("Core input orchestrator initialized successfully.");
            return true;
        }

        public async Task<bool> Start()
        {
            if (IsRunning) return true;

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                _monitoringTask = _inputService.StartMonitoring(token);
                _inactivityTimer.Change(50, 50);

                IsRunning = true;
                RunningStateChanged?.Invoke(true);
                LogMessage?.Invoke("Input monitoring started.");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Failed to start input monitoring: {ex.Message}");
                return false;
            }
        }

        public async Task Stop()
        {
            if (!IsRunning) return;

            try
            {
                _cancellationTokenSource?.Cancel();
                _inactivityTimer.Change(Timeout.Infinite, Timeout.Infinite);

                if (_monitoringTask != null)
                    await _monitoringTask;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                IsRunning = false;
                RunningStateChanged?.Invoke(false);
                LogMessage?.Invoke("Input monitoring stopped.");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Error stopping input monitoring: {ex.Message}");
            }
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            PausedStateChanged?.Invoke(paused);
            LogMessage?.Invoke(paused ? "Input processing paused." : "Input processing resumed.");
        }

        public void SetSensitivity(double sensitivity)
        {
            _mappingService.Sensitivity = sensitivity;
            LogMessage?.Invoke($"Sensitivity set to {sensitivity:F2}");
        }

        private void OnKeyStrokeReceived(KeyStroke stroke)
        {
            // Handle pause/resume key (F4 key = 62)
            if (stroke.Code == 62 && stroke.State == 0)
            {
                SetPaused(!IsPaused);
                return;
            }

            // Handle panic key (P key = 25)
            if (stroke.Code == 25 && stroke.State == 0)
            {
                LogMessage?.Invoke("Panic key pressed - stopping all input processing.");
                _ = Task.Run(Stop);
                return;
            }

            _mappingService.HandleKeyStroke(stroke);
        }

        private void OnMouseStrokeReceived(MouseStroke mouseStroke)
        {
            _lastMouseActivity = DateTime.Now;
            _mappingService.HandleMouseStroke(mouseStroke);
        }

        private void CheckMouseInactivity(object? state)
        {
            if (DateTime.Now - _lastMouseActivity > TimeSpan.FromMilliseconds(50))
            {
                _mappingService.ResetRightStick();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _ = Task.Run(Stop);
                _inactivityTimer?.Dispose();
                _inputService?.Dispose();
                _gamepadService?.Dispose();
                _disposed = true;
            }
        }
    }
}