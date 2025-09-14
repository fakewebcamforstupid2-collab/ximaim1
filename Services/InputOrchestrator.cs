using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace GamepadEmulator.Services
{
    public class InputOrchestrator : IDisposable
    {
        private readonly InterceptionService _interceptionService;
        private readonly GamepadService _gamepadService;
        private readonly MappingService _mappingService;
        private readonly Timer _inactivityTimer;
        private readonly HashSet<int> _blockedKeyCodes;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _keyboardTask;
        private Task? _mouseTask;
        private DateTime _lastMouseActivity = DateTime.Now;
        private bool _disposed;

        public bool IsPaused { get; private set; } = true;
        public bool IsRunning { get; private set; }

        public event Action<string>? LogMessage;
        public event Action<bool>? PausedStateChanged;
        public event Action<bool>? RunningStateChanged;

        public InputOrchestrator()
        {
            _interceptionService = new InterceptionService();
            _gamepadService = new GamepadService();
            _mappingService = new MappingService();

            _blockedKeyCodes = new HashSet<int>
            {
                17, 30, 31, 32, // WASD
                57, 29, 19, 2,   // Space, LCtrl, R, 1
                62, 25           // F4, P
            };

            _inactivityTimer = new Timer(CheckMouseInactivity, null, Timeout.Infinite, Timeout.Infinite);

            // Wire up events
            _interceptionService.LogMessage += message => LogMessage?.Invoke(message);
            _gamepadService.LogMessage += message => LogMessage?.Invoke(message);

            _interceptionService.KeyStrokeReceived += OnKeyStrokeReceived;
            _interceptionService.MouseStrokeReceived += OnMouseStrokeReceived;

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
                    _gamepadService.SetAxisValue(Xbox360Axis.LeftThumbX, x);
                    _gamepadService.SetAxisValue(Xbox360Axis.LeftThumbY, y);
                    _gamepadService.SubmitReport();
                }
            };

            _mappingService.RightStickChanged += (x, y) =>
            {
                if (!IsPaused)
                {
                    _gamepadService.SetAxisValue(Xbox360Axis.RightThumbX, x);
                    _gamepadService.SetAxisValue(Xbox360Axis.RightThumbY, y);
                    _gamepadService.SubmitReport();
                }
            };
        }

        public bool Initialize()
        {
            if (!_interceptionService.Initialize())
            {
                LogMessage?.Invoke("Failed to initialize interception service.");
                return false;
            }

            if (!_gamepadService.Initialize())
            {
                LogMessage?.Invoke("Failed to initialize gamepad service.");
                return false;
            }

            // Set up input filtering - only block keys that are mapped to gamepad functions
            _interceptionService.ShouldBlockKeyStroke = ShouldBlockKey;
            _interceptionService.ShouldBlockMouseStroke = ShouldBlockMouse;

            LogMessage?.Invoke("Input orchestrator initialized successfully.");
            return true;
        }

        public async Task<bool> Start()
        {
            if (IsRunning) return true;

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                // Use single monitoring task instead of separate keyboard/mouse tasks
                _keyboardTask = _interceptionService.StartMonitoring(token);
                _mouseTask = null; // No longer needed

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

                if (_keyboardTask != null)
                    await _keyboardTask;

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

        private bool ShouldBlockKey(InterceptionService.InterceptionStroke stroke)
        {
            // Only block keys that are mapped to gamepad functions
            // Block means "consume for gamepad use", unblock means "pass through to Windows"
            return _blockedKeyCodes.Contains(stroke.code);
        }

        private bool ShouldBlockMouse(InterceptionService.InterceptionMouseStroke mouseStroke)
        {
            // Block mouse movement for right stick mapping, but let clicks through
            // Mouse movement should be consumed for gamepad, but clicks should pass through to Windows
            return (mouseStroke.x != 0 || mouseStroke.y != 0); // Block movement, allow clicks
        }

        private void OnKeyStrokeReceived(InterceptionService.InterceptionStroke stroke)
        {
            // Handle pause/resume key (F4 key = 62)
            if (stroke.code == 62 && stroke.state == 0)
            {
                SetPaused(!IsPaused);
                return;
            }

            // Handle panic key (P key = 25)
            if (stroke.code == 25 && stroke.state == 0)
            {
                LogMessage?.Invoke("Panic key pressed - stopping all input processing.");
                _ = Task.Run(Stop);
                return;
            }

            _mappingService.HandleKeyStroke(stroke);
        }

        private void OnMouseStrokeReceived(InterceptionService.InterceptionMouseStroke mouseStroke)
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
                Stop().Wait(); // Block and wait for stop to complete
                _inactivityTimer?.Dispose();
                _interceptionService?.Dispose();
                _gamepadService?.Dispose();
                _disposed = true;
            }
        }
    }
}