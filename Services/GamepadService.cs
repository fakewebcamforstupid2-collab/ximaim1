using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace GamepadEmulator.Services
{
    public class GamepadService : IDisposable
    {
        private ViGEmClient? _client;
        private IXbox360Controller? _gamepad;
        private readonly object _lockObject = new object();
        private bool _disposed;

        public event Action<string>? LogMessage;
        public bool IsConnected { get; private set; }

        public bool Initialize()
        {
            try
            {
                LogMessage?.Invoke("Attempting to create ViGEm client...");
                _client = new ViGEmClient();
                LogMessage?.Invoke("ViGEm client created successfully.");

                LogMessage?.Invoke("Attempting to create Xbox 360 controller...");
                _gamepad = _client.CreateXbox360Controller();
                LogMessage?.Invoke("Xbox 360 controller created successfully.");
                _gamepad.Connect();
                IsConnected = true;
                LogMessage?.Invoke("ViGEm client created and connected.");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Failed to initialize gamepad: {ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        public void SetButtonState(Xbox360Button button, bool pressed)
        {
            if (!IsConnected || _gamepad == null) return;

            lock (_lockObject)
            {
                _gamepad.SetButtonState(button, pressed);
            }
        }

        public void SetAxisValue(Xbox360Axis axis, short value)
        {
            if (!IsConnected || _gamepad == null) return;

            lock (_lockObject)
            {
                _gamepad.SetAxisValue(axis, value);
            }
        }

        public void SubmitReport()
        {
            if (!IsConnected || _gamepad == null) return;

            lock (_lockObject)
            {
                _gamepad.SubmitReport();
            }
        }

        public void Disconnect()
        {
            if (_gamepad != null && IsConnected)
            {
                _gamepad.Disconnect();
                IsConnected = false;
                LogMessage?.Invoke("Gamepad disconnected.");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _client?.Dispose();
                _disposed = true;
            }
        }
    }
}