using System;

namespace GamepadEmulator.Core.Abstractions
{
    public enum GamepadButton
    {
        A, B, X, Y, Start, Back,
        LeftShoulder, RightShoulder,
        LeftThumb, RightThumb,
        Up, Down, Left, Right
    }

    public enum GamepadAxis
    {
        LeftThumbX, LeftThumbY,
        RightThumbX, RightThumbY,
        LeftTrigger, RightTrigger
    }

    public interface IGamepadOutputService : IDisposable
    {
        event Action<string>? LogMessage;
        bool IsConnected { get; }

        bool Initialize();
        void SetButtonState(GamepadButton button, bool pressed);
        void SetAxisValue(GamepadAxis axis, short value);
        void SubmitReport();
        void Disconnect();
    }
}