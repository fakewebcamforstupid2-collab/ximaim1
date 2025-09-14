using System;
using System.Threading;
using System.Threading.Tasks;

namespace GamepadEmulator.Core.Abstractions
{
    public struct KeyStroke
    {
        public ushort Code { get; set; }
        public ushort State { get; set; }
        public uint Information { get; set; }
    }

    public struct MouseStroke
    {
        public ushort State { get; set; }
        public ushort Flags { get; set; }
        public short Rolling { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public uint Information { get; set; }
    }

    public interface IInputCaptureService : IDisposable
    {
        event Action<KeyStroke>? KeyStrokeReceived;
        event Action<MouseStroke>? MouseStrokeReceived;
        event Action<string>? LogMessage;

        bool Initialize();
        Task StartMonitoring(CancellationToken cancellationToken);
        Task Stop();
    }
}