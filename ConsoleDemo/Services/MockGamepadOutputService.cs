using System;
using GamepadEmulator.Core.Abstractions;

namespace ConsoleDemo.Services
{
    public class MockGamepadOutputService : IGamepadOutputService
    {
        private bool _disposed;
        private short _leftThumbX, _leftThumbY, _rightThumbX, _rightThumbY;

        public event Action<string>? LogMessage;
        public bool IsConnected { get; private set; }

        public bool Initialize()
        {
            IsConnected = true;
            LogMessage?.Invoke("Mock gamepad output service initialized (Linux compatible)");
            LogMessage?.Invoke("This simulates the Windows ViGEm virtual gamepad functionality");
            return true;
        }

        public void SetButtonState(GamepadButton button, bool pressed)
        {
            LogMessage?.Invoke($"Button {button}: {(pressed ? "PRESSED" : "released")}");
        }

        public void SetAxisValue(GamepadAxis axis, short value)
        {
            string axisName = axis.ToString();
            string percentage = (value / 327.67).ToString("F1");

            // Store values for display
            switch (axis)
            {
                case GamepadAxis.LeftThumbX: _leftThumbX = value; break;
                case GamepadAxis.LeftThumbY: _leftThumbY = value; break;
                case GamepadAxis.RightThumbX: _rightThumbX = value; break;
                case GamepadAxis.RightThumbY: _rightThumbY = value; break;
            }

            if (Math.Abs(value) > 1000) // Only log significant movements
            {
                LogMessage?.Invoke($"Axis {axisName}: {value} ({percentage}%)");
            }
        }

        public void SubmitReport()
        {
            // Show current gamepad state summary if any axis has significant value
            if (Math.Abs(_leftThumbX) > 1000 || Math.Abs(_leftThumbY) > 1000 || 
                Math.Abs(_rightThumbX) > 1000 || Math.Abs(_rightThumbY) > 1000)
            {
                LogMessage?.Invoke($"Gamepad State - Left: ({_leftThumbX:F0},{_leftThumbY:F0}) Right: ({_rightThumbX:F0},{_rightThumbY:F0})");
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
            LogMessage?.Invoke("Mock gamepad disconnected");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _disposed = true;
            }
        }
    }
}