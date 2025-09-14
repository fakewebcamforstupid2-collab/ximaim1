using System;
using System.Collections.Generic;
using GamepadEmulator.Core.Abstractions;

namespace GamepadEmulator.Core.Services
{
    public class MappingService
    {
        private readonly Dictionary<int, GamepadButton> _keyToButton = new Dictionary<int, GamepadButton>
        {
            { 57, GamepadButton.A }, // Space
            { 29, GamepadButton.B }, // LCtrl
            { 19, GamepadButton.X }, // R
            { 2, GamepadButton.Y }, // 1
            { 15, GamepadButton.Back }, // Tab
            { 1, GamepadButton.Start }, // ESC
            { 16, GamepadButton.LeftShoulder }, // Q
            { 18, GamepadButton.RightShoulder }, // E
            { 56, GamepadButton.Up }, // L Alt
            { 58, GamepadButton.Down }, // Caps Lock
            { 46, GamepadButton.Left }, // C
            { 4, GamepadButton.Right }, // 3
            { 42, GamepadButton.LeftThumb }, // L Shift
            { 47, GamepadButton.RightThumb } // V
        };

        private readonly Dictionary<int, Tuple<int, double>> _movementKeys = new Dictionary<int, Tuple<int, double>>
        {
            { 17, new Tuple<int, double>(0, 1.0) }, // W
            { 31, new Tuple<int, double>(0, -1.0) }, // S
            { 32, new Tuple<int, double>(1, 1.0) }, // D
            { 30, new Tuple<int, double>(1, -1.0) } // A
        };

        private readonly HashSet<int> _activeKeys = new HashSet<int>();

        public double Sensitivity { get; set; } = 1.05;

        public event Action<GamepadButton, bool>? ButtonStateChanged;
        public event Action<short, short>? LeftStickChanged;
        public event Action<short, short>? RightStickChanged;

        public void HandleKeyStroke(KeyStroke stroke)
        {
            if (_keyToButton.TryGetValue(stroke.Code, out var button))
            {
                bool pressed = stroke.State == 0;
                ButtonStateChanged?.Invoke(button, pressed);
            }

            if (_movementKeys.ContainsKey(stroke.Code))
            {
                if (stroke.State == 0) 
                    _activeKeys.Add(stroke.Code);
                else 
                    _activeKeys.Remove(stroke.Code);
                
                UpdateLeftStick();
            }
        }

        public void HandleMouseStroke(MouseStroke mouseStroke)
        {
            double rjoystick_x = (mouseStroke.X / 32767.0) * Sensitivity;
            double rjoystick_y = (mouseStroke.Y / 32767.0) * Sensitivity;

            rjoystick_x = Math.Max(Math.Min(rjoystick_x, 1.0), -1.0);
            rjoystick_y = Math.Max(Math.Min(rjoystick_y, 1.0), -1.0);

            short joystick_x = (short)(rjoystick_x * short.MaxValue);
            short joystick_y = (short)(-rjoystick_y * short.MaxValue);

            RightStickChanged?.Invoke(joystick_x, joystick_y);
        }

        public void ResetRightStick()
        {
            RightStickChanged?.Invoke(0, 0);
        }

        private void UpdateLeftStick()
        {
            double x = 0;
            double y = 0;

            foreach (var key in _activeKeys)
            {
                if (_movementKeys.TryGetValue(key, out var movement))
                {
                    if (movement.Item1 == 0)
                    {
                        y += movement.Item2;
                    }
                    else
                    {
                        x += movement.Item2;
                    }
                }
            }

            short stick_x = (short)(x * short.MaxValue);
            short stick_y = (short)(y * short.MaxValue);

            LeftStickChanged?.Invoke(stick_x, stick_y);
        }
    }
}