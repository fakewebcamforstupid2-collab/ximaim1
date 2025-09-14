using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GamepadEmulator.Services
{
    public class InterceptionService : IDisposable
    {
        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr interception_create_context();

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void interception_destroy_context(IntPtr context);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_receive(IntPtr context, int device, ref InterceptionStroke stroke, uint nstroke);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_receive(IntPtr context, int device, ref InterceptionMouseStroke stroke, uint nstroke);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void interception_set_filter(IntPtr context, InterceptionPredicate predicate, ushort filter);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_wait(IntPtr context);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_send(IntPtr context, int device, ref InterceptionStroke stroke, uint nstroke);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_send(IntPtr context, int device, ref InterceptionMouseStroke stroke, uint nstroke);

        public delegate int InterceptionPredicate(int device);

        private const ushort INTERCEPTION_FILTER_KEY_DOWN = 0x01;
        private const ushort INTERCEPTION_FILTER_KEY_UP = 0x02;
        private const ushort INTERCEPTION_FILTER_MOUSE_MOVE = 0x01;
        private const ushort INTERCEPTION_FILTER_MOUSE_BUTTONS = 0xFF;

        [StructLayout(LayoutKind.Sequential)]
        public struct InterceptionStroke
        {
            public ushort code;
            public ushort state;
            public uint information;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct InterceptionMouseStroke
        {
            public ushort state;
            public ushort flags;
            public short rolling;
            public int x;
            public int y;
            public uint information;
        }

        public event Action<InterceptionStroke>? KeyStrokeReceived;
        public event Action<InterceptionMouseStroke>? MouseStrokeReceived;
        public event Action<string>? LogMessage;

        // Delegate that returns true if the key should be blocked (consumed by gamepad), false if it should pass through
        public Func<InterceptionStroke, bool>? ShouldBlockKeyStroke;
        public Func<InterceptionMouseStroke, bool>? ShouldBlockMouseStroke;

        private IntPtr _context;
        private bool _disposed;

        public bool Initialize()
        {
            try
            {
                LogMessage?.Invoke("Checking if Interceptor is loaded...");
                _context = interception_create_context();
                if (_context == IntPtr.Zero)
                {
                    LogMessage?.Invoke("Failed to create interception context.");
                    return false;
                }

                bool mouseDetected = false;
                for (int i = 11; i <= 20; i++)
                {
                    if (IsMouseDevice(i) == 1)
                    {
                        LogMessage?.Invoke($"Mouse device detected: {i}");
                        mouseDetected = true;
                        break;
                    }
                }

                if (!mouseDetected)
                {
                    LogMessage?.Invoke("No mouse devices detected.");
                    return false;
                }

                // Set filters for keyboard and mouse
                interception_set_filter(_context, IsKeyboardDevice, INTERCEPTION_FILTER_KEY_DOWN | INTERCEPTION_FILTER_KEY_UP);
                interception_set_filter(_context, IsMouseDevice, INTERCEPTION_FILTER_MOUSE_MOVE | INTERCEPTION_FILTER_MOUSE_BUTTONS);

                LogMessage?.Invoke("Interception context created successfully.");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Exception while initializing Interceptor: {ex.Message}");
                return false;
            }
        }

        public async Task StartMonitoring(CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        int device = interception_wait(_context);

                        if (IsKeyboardDevice(device) == 1)
                        {
                            // Handle keyboard event
                            InterceptionStroke stroke = new InterceptionStroke();
                            interception_receive(_context, device, ref stroke, 1);
                            
                            // Check if this key should be blocked (used for gamepad mapping)
                            bool shouldBlock = ShouldBlockKeyStroke?.Invoke(stroke) ?? false;
                            
                            if (shouldBlock)
                            {
                                // This key is mapped to gamepad - consume it and fire event
                                KeyStrokeReceived?.Invoke(stroke);
                            }
                            else
                            {
                                // This key is not mapped - pass it through to Windows
                                interception_send(_context, device, ref stroke, 1);
                            }
                        }
                        else if (IsMouseDevice(device) == 1)
                        {
                            // Handle mouse event
                            InterceptionMouseStroke mouseStroke = new InterceptionMouseStroke();
                            interception_receive(_context, device, ref mouseStroke, 1);
                            
                            // Check if this mouse event should be blocked (used for gamepad mapping)
                            bool shouldBlock = ShouldBlockMouseStroke?.Invoke(mouseStroke) ?? false;
                            
                            if (shouldBlock)
                            {
                                // This mouse event is mapped to gamepad - consume it and fire event
                                MouseStrokeReceived?.Invoke(mouseStroke);
                            }
                            else
                            {
                                // This mouse event is not mapped - pass it through to Windows
                                interception_send(_context, device, ref mouseStroke, 1);
                            }
                        }

                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"Error in input monitoring: {ex.Message}");
                    }
                }
            }, cancellationToken);
        }

        private static int IsKeyboardDevice(int device)
        {
            return (device >= 1 && device <= 10) ? 1 : 0;
        }

        private static int IsMouseDevice(int device)
        {
            return (device >= 11 && device <= 20) ? 1 : 0;
        }

        public void Dispose()
        {
            if (!_disposed && _context != IntPtr.Zero)
            {
                interception_destroy_context(_context);
                _context = IntPtr.Zero;
                _disposed = true;
            }
        }
    }
}