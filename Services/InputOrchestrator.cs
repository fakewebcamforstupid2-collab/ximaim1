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

        // Expose the services
        public InterceptionService InterceptionService => _interceptionService;
        public MappingService MappingService => _mappingService;

        public event Action<string>? LogMessage;
        public event Action<bool>? PausedStateChanged;
        public event Action<bool>? RunningStateChanged;
        public event Action? ReverseModeToggled;
        public event Action? BlockKeysToggled;

        public InputOrchestrator()
        {
            _interceptionService = new InterceptionService();
            _gamepadService = new GamepadService();
            _mappingService = new MappingService();

            _blockedKeyCodes = new HashSet<int>
            {
                17, 30, 31, 32, // WASD
                57, 29, 19, 2,   // Space, LCtrl, R, 1
                82, 25,          // INSERT, P
                87, 67           // F11, F9
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

        public void SetDeadZone(double value) => _mappingService.DeadZone = value;
        public void SetHorizontalSensitivity(double value) => _mappingService.HorizontalSensitivity = value;
        public void SetVerticalSensitivity(double value) => _mappingService.VerticalSensitivity = value;
        public void SetIsExponentialCurve(bool value) => _mappingService.IsExponentialCurve = value;
        public void SetNoiseFilter(double value) => _mappingService.NoiseFilter = value;
        public void SetAreKeysBlocked(bool value) => AreKeysBlocked = value;
        public void SetIsReverseModeOn(bool value) => _mappingService.IsReverseModeOn = value;

        public bool AreKeysBlocked { get; private set; } = true;

        private bool ShouldBlockKey(InterceptionService.InterceptionStroke stroke)
        {
            if (!AreKeysBlocked)
            {
                return false; // Pass all keys through if blocking is disabled
            }

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
            // Handle pause/resume key (INSERT key = 82)
            if (stroke.code == 82 && stroke.state == 0) // INSERT key down
            {
                SetPaused(!IsPaused);
                return;
            }

            // Handle panic key (P key = 25)
            if (stroke.code == 25 && stroke.state == 0) // P key down
            {
                _ = PanicAsync();
                return;
            }

            // Handle F11 key for reverse mode toggle (F11 key = 87)
            if (stroke.code == 87 && stroke.state == 0) // F11 key down
            {
                _mappingService.IsReverseModeOn = !_mappingService.IsReverseModeOn;
                ReverseModeToggled?.Invoke();
                return;
            }

            // Handle F9 key for block keys toggle (F9 key = 67)
            if (stroke.code == 67 && stroke.state == 0) // F9 key down
            {
                AreKeysBlocked = !AreKeysBlocked;
                BlockKeysToggled?.Invoke();
                return;
            }

            // Pass key stroke to mapping service for gamepad mapping
            _mappingService.HandleKeyStroke(stroke);
        }

        private void OnMouseStrokeReceived(InterceptionService.InterceptionMouseStroke mouseStroke)
        {
            // Pass mouse stroke to mapping service for gamepad mapping
            _mappingService.HandleMouseStroke(mouseStroke);
        }

        private async Task PanicAsync()
        {
            await Stop();
            LogMessage?.Invoke("PANIC: All input processing stopped!");
        }

        private void CheckMouseInactivity(object? state)
        {
            if (!IsPaused && (DateTime.Now - _lastMouseActivity).TotalMilliseconds > 100)
            {
                _mappingService.ResetRightStick();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _inactivityTimer?.Dispose();
                _interceptionService?.Dispose();
                _gamepadService?.Dispose();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }
    }
}