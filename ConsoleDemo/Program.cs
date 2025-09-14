using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleDemo.Services;
using GamepadEmulator.Core.Services;

namespace ConsoleDemo
{
    class Program
    {
        private static CoreInputOrchestrator? _orchestrator;
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    GAMEPAD EMULATOR DEMO                    ║");
            Console.WriteLine("║                     (Linux Compatible)                      ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  This demonstrates the WPF Gamepad Emulator functionality   ║");
            Console.WriteLine("║  using mock services that simulate Windows-only features.   ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║  On Windows, this would use:                                ║");
            Console.WriteLine("║  • Interception driver for low-level input capture         ║");
            Console.WriteLine("║  • ViGEm for virtual Xbox 360 controller output            ║");
            Console.WriteLine("║  • WPF UI with real-time controls and visualizations       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Setup mock services
            var inputService = new MockInputCaptureService();
            var gamepadService = new MockGamepadOutputService();
            _orchestrator = new CoreInputOrchestrator(inputService, gamepadService);

            // Wire up logging
            _orchestrator.LogMessage += message => 
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                Console.WriteLine($"[{timestamp}] {message}");
            };

            _orchestrator.RunningStateChanged += isRunning =>
            {
                Console.WriteLine($">>> STATUS: {(isRunning ? "RUNNING" : "STOPPED")} <<<");
            };

            _orchestrator.PausedStateChanged += isPaused =>
            {
                Console.WriteLine($">>> STATUS: {(isPaused ? "PAUSED" : "ACTIVE")} <<<");
            };

            // Setup signal handling
            AppDomain.CurrentDomain.ProcessExit += (_, _) => _cancellationTokenSource.Cancel();

            // Initialize and start
            Console.WriteLine("Initializing gamepad emulator...");
            if (!_orchestrator.Initialize())
            {
                Console.WriteLine("Failed to initialize. Exiting.");
                return;
            }

            Console.WriteLine("\nStarting input monitoring...");
            if (!await _orchestrator.Start())
            {
                Console.WriteLine("Failed to start input monitoring. Exiting.");
                return;
            }

            Console.WriteLine("\nGamepad emulator is running!");
            Console.WriteLine("Key Mappings:");
            Console.WriteLine("  • WASD → Left stick movement");
            Console.WriteLine("  • Mouse → Right stick movement");
            Console.WriteLine("  • Space → A button");
            Console.WriteLine("  • LCtrl → B button");
            Console.WriteLine("  • R → X button");
            Console.WriteLine("  • 1 → Y button");
            Console.WriteLine("  • F4 → Pause/Resume");
            Console.WriteLine("\nSimulated input will appear every 2 seconds...");
            Console.WriteLine("Press Ctrl+C to stop.\n");

            // Demonstrate functionality
            await DemonstrateFeatures();

            // Wait for cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            Console.WriteLine("\nShutting down...");
            await _orchestrator.Stop();
            _orchestrator.Dispose();
            Console.WriteLine("Gamepad emulator demo completed.");
        }

        private static async Task DemonstrateFeatures()
        {
            await Task.Delay(3000);

            Console.WriteLine("\n--- DEMONSTRATING FEATURES ---");
            
            // Test pause/resume
            Console.WriteLine("Testing pause/resume functionality...");
            _orchestrator!.SetPaused(false);
            await Task.Delay(2000);
            _orchestrator.SetPaused(true);
            await Task.Delay(1000);
            _orchestrator.SetPaused(false);

            await Task.Delay(2000);

            // Test sensitivity adjustment
            Console.WriteLine("Testing sensitivity adjustment...");
            _orchestrator.SetSensitivity(0.5);
            await Task.Delay(2000);
            _orchestrator.SetSensitivity(2.0);
            await Task.Delay(2000);
            _orchestrator.SetSensitivity(1.05); // Default

            Console.WriteLine("Feature demonstration complete. Mock input simulation continues...\n");
        }
    }
}