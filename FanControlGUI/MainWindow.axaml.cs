using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FanControlGUI.Objects;
using Avalonia;
using System.IO;

namespace FanControlGUI
{
    public partial class MainWindow : Window
    {
        private List<Subsystem> subsystems = new();
        private DispatcherTimer timer;
        private const string LogFilePath = "system_log.txt";

        public MainWindow()
        {
            InitializeComponent();

            // Schedule the configuration prompt after the window becomes visible
            Dispatcher.UIThread.Post(async () => await PromptForConfiguration(), DispatcherPriority.Background);

            // Initialize and start the timer for real-time updates
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += UpdateFanSpeeds;
            timer.Start();

            InitializeLogFile();
        }
        private void InitializeLogFile()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(LogFilePath, false))
                {
                    writer.WriteLine("Timestamp,SubsystemID,MaxTemperature, FanID,FanSpeed,FanMaxRPM");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize log file: {ex.Message}");
            }
        }

        private void UpdateFanSpeeds(object? sender, EventArgs e)
        {
            float globalMaxTemperature = 0;

            // Determine the maximum temperature across all subsystems
            foreach (var subsystem in subsystems)
            {
                float range = new Random().NextSingle(); // Mock temperature
                float factor = new Random().Next(0, 100);
                float temperature = range * factor;
                subsystem.MaxTemperature = temperature;
                globalMaxTemperature = Math.Max(globalMaxTemperature, temperature);
            }

            // Calculate the fan speed percentage based on the global maximum temperature
            double speedPercentage = CalculateSpeedPercentage(globalMaxTemperature);

            // Update all fans in all subsystems
            foreach (var subsystem in subsystems)
            {
                foreach (var fan in subsystem.Fans)
                {
                    fan.UpdateSpeed(speedPercentage);
                }
            }
            SaveLog(globalMaxTemperature);
            // Refresh the UI
            RefreshSubsystemUI();
        }

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
                            writer.WriteLine($"{timestamp}\t{subsystem.SubsystemId}\t{subsystem.MaxTemperature:F2}\t{fan.FanId},\t{fan.Speed:F0}\t{fan.MaxRPM}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save log: {ex.Message}");
            }
        }

        private void RefreshSubsystemUI()
        {
            // Ensure SubsystemPanel exists
            var SubsystemPanel = this.FindControl<StackPanel>("SubsystemPanel");
            SubsystemPanel.Children.Clear();

            foreach (var subsystem in subsystems)
            {
                var subsystemPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
                subsystemPanel.Children.Add(new TextBlock { Text = $"Subsystem {subsystem.SubsystemId} - Temp: {subsystem.MaxTemperature:F2}°C", FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold });

                foreach (var fan in subsystem.Fans)
                {
                    subsystemPanel.Children.Add(new TextBlock
                    {
                        Text = $"Fan {fan.FanId}: {fan.Speed:F0} RPM (Max: {fan.MaxRPM} RPM, Speed Percentage: {fan.Speed / fan.MaxRPM * 100:F2}%)"
                    });
                }

                SubsystemPanel.Children.Add(subsystemPanel);
            }
        }

        private double CalculateSpeedPercentage(float temperature)
        {
            if (temperature >= 75) return 1.0f;
            if (temperature <= 25) return 0.2f;
            return 0.2f + (temperature - 25) / 50 * 0.8f;
        }

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

            DisplaySubsystemConfiguration();
        }

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

        private async void DisplaySubsystemConfiguration()
        {
            // Ensure SubsystemPanel exists
            var SubsystemPanel = this.FindControl<StackPanel>("SubsystemPanel");
            SubsystemPanel.Children.Clear();

            foreach (var subsystem in subsystems)
            {
                var subsystemPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
                subsystemPanel.Children.Add(new TextBlock { Text = $"Subsystem {subsystem.SubsystemId} - Temp: {subsystem.MaxTemperature:F2}°C", FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold });

                foreach (var fan in subsystem.Fans)
                {
                    subsystemPanel.Children.Add(new TextBlock
                    {
                        Text = $"Fan {fan.FanId}: {fan.Speed:F0} RPM (Max: {fan.MaxRPM} RPM, Speed Percentage: {fan.Speed / fan.MaxRPM * 100:F2}%)"
                    });
                }

                SubsystemPanel.Children.Add(subsystemPanel);
            }
        }
    }
}
