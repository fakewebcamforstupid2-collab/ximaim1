using System;
using System.Collections.Generic;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace GamepadEmulator.Services
{
    public class MappingService
    {
        private readonly Dictionary<int, Xbox360Button> _keyToButton = new Dictionary<int, Xbox360Button>
        {
            { 57, Xbox360Button.A }, // Space
            { 29, Xbox360Button.B }, // LCtrl
            { 19, Xbox360Button.X }, // R
            { 2, Xbox360Button.Y }, // 1
            { 15, Xbox360Button.Back }, // Tab
            { 1, Xbox360Button.Start }, // ESC
            { 16, Xbox360Button.LeftShoulder }, // Q
            { 18, Xbox360Button.RightShoulder }, // E
            { 56, Xbox360Button.Up }, // L Alt
            { 58, Xbox360Button.Down }, // Caps Lock
            { 46, Xbox360Button.Left }, // C
            { 4, Xbox360Button.Right }, // 3
            { 42, Xbox360Button.LeftThumb }, // L Shift
            { 47, Xbox360Button.RightThumb } // V
        };

        private readonly Dictionary<int, Tuple<int, double>> _movementKeys = new Dictionary<int, Tuple<int, double>>
        {
            { 17, new Tuple<int, double>(0, 1.0) }, // W
            { 31, new Tuple<int, double>(0, -1.0) }, // S
            { 32, new Tuple<int, double>(1, 1.0) }, // D
            { 30, new Tuple<int, double>(1, -1.0) } // A
        };

        private readonly HashSet<int> _activeKeys = new HashSet<int>();

        private double _previousX = 0;
        private double _previousY = 0;

        public double DeadZone { get; set; } = 5.0;
        public double HorizontalSensitivity { get; set; } = 1.0;
        public double VerticalSensitivity { get; set; } = 1.0;
        public bool IsExponentialCurve { get; set; } = true;
        public double NoiseFilter { get; set; } = 15.0;
        public bool IsReverseModeOn { get; set; }

        public event Action<Xbox360Button, bool>? ButtonStateChanged;
        public event Action<short, short>? LeftStickChanged;
        public event Action<short, short>? RightStickChanged;

        public void HandleKeyStroke(InterceptionService.InterceptionStroke stroke)
        {
            if (_keyToButton.TryGetValue(stroke.code, out var button))
            {
                bool pressed = stroke.state == 0;
                ButtonStateChanged?.Invoke(button, pressed);
            }

            if (_movementKeys.ContainsKey(stroke.code))
            {
                if (stroke.state == 0) 
                    _activeKeys.Add(stroke.code);
                else 
                    _activeKeys.Remove(stroke.code);
                
                UpdateLeftStick();
            }
        }

        public void HandleMouseStroke(InterceptionService.InterceptionMouseStroke mouseStroke)
        {
            // Normalize mouse input
            double mouse_x = mouseStroke.x / 32767.0;
            double mouse_y = mouseStroke.y / 32767.0;

            // Apply Dead Zone
            double deadZoneAmount = DeadZone / 100.0;
            if (Math.Abs(mouse_x) < deadZoneAmount) mouse_x = 0;
            if (Math.Abs(mouse_y) < deadZoneAmount) mouse_y = 0;

            // Apply Sensitivity
            double rjoystick_x = mouse_x * HorizontalSensitivity;
            double rjoystick_y = mouse_y * VerticalSensitivity;

            // Apply Exponential Curve
            if (IsExponentialCurve)
            {
                rjoystick_x = Math.Sign(rjoystick_x) * Math.Pow(Math.Abs(rjoystick_x), 2);
                rjoystick_y = Math.Sign(rjoystick_y) * Math.Pow(Math.Abs(rjoystick_y), 2);
            }

            // Clamp values to [-1.0, 1.0]
            rjoystick_x = Math.Max(Math.Min(rjoystick_x, 1.0), -1.0);
            rjoystick_y = Math.Max(Math.Min(rjoystick_y, 1.0), -1.0);

            // Apply Noise Filter
            double filterWeight = NoiseFilter / 100.0;
            rjoystick_x = (rjoystick_x * (1 - filterWeight)) + (_previousX * filterWeight);
            rjoystick_y = (rjoystick_y * (1 - filterWeight)) + (_previousY * filterWeight);
            _previousX = rjoystick_x;
            _previousY = rjoystick_y;

            // Convert to short for the gamepad driver
            short joystick_x = (short)(rjoystick_x * short.MaxValue);
            short joystick_y = (short)(-rjoystick_y * short.MaxValue); // Y-axis is inverted by default

            // Apply Reverse Mode
            if (IsReverseModeOn)
            {
                joystick_y = (short)-joystick_y;
            }

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