using System;
using System.Threading;
using System.Threading.Tasks;
using GamepadEmulator.Core.Abstractions;

namespace ConsoleDemo.Services
{
    public class MockInputCaptureService : IInputCaptureService
    {
        private readonly Timer _simulationTimer;
        private readonly Random _random = new Random();
        private bool _disposed;

        public event Action<KeyStroke>? KeyStrokeReceived;
        public event Action<MouseStroke>? MouseStrokeReceived;
        public event Action<string>? LogMessage;

        public MockInputCaptureService()
        {
            _simulationTimer = new Timer(SimulateInput, null, Timeout.Infinite, Timeout.Infinite);
        }

        public bool Initialize()
        {
            LogMessage?.Invoke("Mock input capture service initialized (Linux compatible)");
            LogMessage?.Invoke("This simulates the Windows Interception library functionality");
            return true;
        }

        public async Task StartMonitoring(CancellationToken cancellationToken)
        {
            LogMessage?.Invoke("Starting mock input monitoring...");
            _simulationTimer.Change(2000, 2000); // Simulate input every 2 seconds

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }

            _simulationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            LogMessage?.Invoke("Mock input monitoring stopped.");
        }

        public async Task Stop()
        {
            _simulationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            LogMessage?.Invoke("Mock input monitoring stopped.");
        }

        private void SimulateInput(object? state)
        {
            try
            {
                // Simulate various key presses
                var keys = new[] { 17, 30, 31, 32, 57, 29, 19, 2, 62 }; // W, A, S, D, Space, LCtrl, R, 1, F4
                var keyCode = keys[_random.Next(keys.Length)];
                var keyState = _random.Next(2); // 0 = press, 1 = release

                var keyStroke = new KeyStroke
                {
                    Code = (ushort)keyCode,
                    State = (ushort)keyState,
                    Information = 0
                };

                LogMessage?.Invoke($"Simulated key: {GetKeyName(keyCode)} {(keyState == 0 ? "pressed" : "released")}");
                KeyStrokeReceived?.Invoke(keyStroke);

                // Occasionally simulate mouse movement
                if (_random.Next(3) == 0)
                {
                    var mouseStroke = new MouseStroke
                    {
                        X = _random.Next(-1000, 1000),
                        Y = _random.Next(-1000, 1000),
                        State = 0,
                        Flags = 0,
                        Rolling = 0,
                        Information = 0
                    };

                    LogMessage?.Invoke($"Simulated mouse: X={mouseStroke.X}, Y={mouseStroke.Y}");
                    MouseStrokeReceived?.Invoke(mouseStroke);
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Error in mock input simulation: {ex.Message}");
            }
        }

        private string GetKeyName(int keyCode)
        {
            return keyCode switch
            {
                17 => "W",
                30 => "A", 
                31 => "S",
                32 => "D",
                57 => "Space",
                29 => "LCtrl",
                19 => "R",
                2 => "1",
                62 => "F4",
                _ => $"Key{keyCode}"
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _simulationTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}