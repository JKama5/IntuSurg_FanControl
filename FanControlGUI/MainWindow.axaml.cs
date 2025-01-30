using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FanControlGUI.Objects;
using Avalonia;
using Avalonia.Media;
using System.IO;
using Tmds.DBus.Protocol;

namespace FanControlGUI
{
    public partial class MainWindow : Window
    {
        private List<Subsystem> subsystems = new(); // Stores all subsystem objects containing fan information
        private DispatcherTimer timer; // Timer for periodic "real-time" updates to fan speeds and UI
        private const string LogFilePath = "system_log.txt"; // Path for the log file storing system performance data
        private DispatcherTimer _animationTimer; // Timer for the fan animation
        private double _currentAngle = 0; // Stores the current "angle" of the fan 
        public double _fan_animate_speed = 0; // Stores the speed of the fans
        private RotateTransform _fanTransform; 
        public MainWindow()
        {
            InitializeComponent(); // Auto-generated UI initialization

            // Schedule a configuration prompt once the window becomes visible
            Dispatcher.UIThread.Post(async () => await PromptForConfiguration(), DispatcherPriority.Background);

            // Initialize and start a timer to update fan speeds every second
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += UpdateFanSpeeds;
            timer.Start();

            var fanImage = this.FindControl<Image>("FanImage");
            _fanTransform = new RotateTransform { CenterX = 50, CenterY = 50 };
            fanImage.RenderTransform = _fanTransform;
            
            // Initialize the timer for animation
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animationTimer.Tick += (sender, e) => AnimateFan();
            _animationTimer.Start();

            InitializeLogFile(); // Prepares the log file for storing system activity
        }

        /// <summary>
        /// Initializes the log file by writing the header row. If the file exists, it will be overwritten.
        /// </summary>
        private void InitializeLogFile()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(LogFilePath, false))
                {
                    writer.WriteLine("Timestamp,SubsystemID,MaxTemperature,FanID,FanSpeed,FanMaxRPM");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Periodically updates fan speeds and subsystem temperatures, saves logs, and refreshes the UI.
        /// </summary>
        /// <param name="sender">The source of the event (timer).</param>
        /// <param name="e">Event data.</param>
        private void UpdateFanSpeeds(object? sender, EventArgs e)
        {
            float MaxTemperatureAllSubsystems = 0;

            // Simulate temperature updates and determine the highest temperature across subsystems
            foreach (var subsystem in subsystems)
            {
                float range = new Random().NextSingle(); // Generate a random temperature range (0.00 to 1.00)
                float temperature = range * 100; // Calculate temperature for the subsystem (maximum of 100 in this example)
                subsystem.MaxTemperature = temperature;
                MaxTemperatureAllSubsystems = Math.Max(MaxTemperatureAllSubsystems, temperature); // Update global maximum
            }

            // Compute fan speed percentage based on the highest temperature
            double speedPercentage = CalculateSpeedPercentage(MaxTemperatureAllSubsystems);
            _fan_animate_speed = speedPercentage;
            // Update the speed of each fan in every subsystem
            foreach (var subsystem in subsystems)
            {
                foreach (var fan in subsystem.Fans)
                {
                    fan.UpdateSpeed(speedPercentage); // Adjust fan speed based on computed percentage
                }
            }

            SaveLog(MaxTemperatureAllSubsystems); // Record current system state in the log file
            RefreshSubsystemUI(); // Refresh UI to reflect updated speeds and temperatures
        }

        /// <summary>
        /// Writes the current state of all subsystems and fans to the log file.
        /// </summary>
        /// <param name="globalMaxTemperature">The highest temperature across all subsystems.</param>
        private void SaveLog(double globalMaxTemperature)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    foreach (var subsystem in subsystems)
                    {
                        foreach (var fan in subsystem.Fans)
                        {
                            writer.WriteLine($"{timestamp},{subsystem.SubsystemId},{subsystem.MaxTemperature:F2},{fan.FanId},{fan.Speed:F0},{fan.MaxRPM}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save log: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the UI to display the current temperature and fan speeds for all subsystems.
        /// </summary>
        private void RefreshSubsystemUI()
        {
            var SubsystemPanel = this.FindControl<StackPanel>("SubsystemPanel"); // Locate the main UI panel for subsystems
            SubsystemPanel.Children.Clear(); // Clear previous subsystem entries

            foreach (var subsystem in subsystems)
            {
                var subsystemPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
                subsystemPanel.Children.Add(new TextBlock
                {
                    Text = $"Subsystem {subsystem.SubsystemId} - Temp: {subsystem.MaxTemperature:F2}°C",
                    FontSize = 16,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });

                foreach (var fan in subsystem.Fans)
                {
                    subsystemPanel.Children.Add(new TextBlock
                    {
                        Text = $"Fan {fan.FanId}: {fan.Speed:F0} RPM (Max: {fan.MaxRPM} RPM)"
                    });
                }

                SubsystemPanel.Children.Add(subsystemPanel); // Add the subsystem's panel to the main UI
            }
        }

        /// <summary>
        /// Calculates the fan speed percentage based on the given temperature using a piecewise linear function.
        /// </summary>
        /// <param name="temperature">The current temperature to evaluate.</param>
        /// <returns>A speed percentage as a double (0.2 to 1.0).</returns>
        private double CalculateSpeedPercentage(float temperature)
        {
            if (temperature >= 75) return 1.0; // Maximum speed at temperatures >= 75°C
            if (temperature <= 25) return 0.2; // Minimum speed at temperatures <= 25°C
            return 0.2 + (temperature - 25) / 50 * 0.8; // Linear interpolation for temperatures between 25°C and 75°C
        }

        /// <summary>
        /// Guides the user through subsystem and fan configuration at startup.
        /// </summary>
        private async Task PromptForConfiguration()
        {
            var numSubsystems = await PromptForInteger("Enter the number of subsystems:");

            for (int i = 0; i < numSubsystems; i++)
            {
                int numFans = await PromptForInteger($"Enter the number of fans in subsystem {i + 1}:");
                var fans = new List<Fan>();

                for (int j = 0; j < numFans; j++)
                {
                    int maxRpm = await PromptForInteger($"Enter the maximum RPM for Fan {j + 1} in Subsystem {i + 1}:");
                    fans.Add(new Fan { FanId = j + 1, MaxRPM = maxRpm });
                }

                subsystems.Add(new Subsystem { SubsystemId = i + 1, Fans = fans });
            }

            DisplaySubsystemConfiguration(); // Update UI to reflect newly configured subsystems
        }

        /// <summary>
        /// Prompts the user to input a positive integer with error handling for invalid inputs.
        /// </summary>
        /// <param name="message">The message displayed in the input dialog.</param>
        /// <returns>The user's input as a positive integer.</returns>
        private async Task<int> PromptForInteger(string message)
        {
            while (true)
            {
                var inputDialog = new Dialogs.InputDialog(message);
                string input = await inputDialog.ShowAsync(this);

                if (int.TryParse(input, out int result) && result > 0)
                {
                    return result;
                }
                else
                {
                    var errorDialog = new Dialogs.MessageDialog("Invalid input. Please enter a positive integer.");
                    await errorDialog.ShowAsync(this);
                }
            }
        }

        /// <summary>
        /// Displays the current configuration of subsystems and fans in the UI.
        /// </summary>
        private void DisplaySubsystemConfiguration()
        {
            var SubsystemPanel = this.FindControl<StackPanel>("SubsystemPanel"); // Locate the main UI panel for subsystems
            SubsystemPanel.Children.Clear(); // Clear previous configurations

            foreach (var subsystem in subsystems)
            {
                var subsystemPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
                subsystemPanel.Children.Add(new TextBlock
                {
                    Text = $"Subsystem {subsystem.SubsystemId} - Temp: {subsystem.MaxTemperature:F2}°C",
                    FontSize = 16,
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });

                foreach (var fan in subsystem.Fans)
                {
                    subsystemPanel.Children.Add(new TextBlock
                    {
                        Text = $"Fan {fan.FanId}: {fan.Speed:F0} RPM (Max: {fan.MaxRPM} RPM"
                    });
                }

                SubsystemPanel.Children.Add(subsystemPanel); // Add the subsystem's panel to the main UI
            }
        }

        private void AnimateFan()
        {
            // Get speed from the fans
            double speed = 20 * _fan_animate_speed;
            
            // Update the rotation angle
            _currentAngle += speed;
            if (_currentAngle >= 360)
                _currentAngle -= 360;

            // Apply the rotation to the fan transform
            _fanTransform.Angle = _currentAngle;

            // Update fan speed display
            var fanSpeedText = this.FindControl<TextBlock>("FanSpeedText");
            fanSpeedText.Text = $"Fan Speed: {_fan_animate_speed*100:F0} %";
        }

    }
}
