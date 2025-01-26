using Avalonia.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FanControlGUI.Objects;
using Avalonia.Interactivity;

namespace FanControlGUI
{
    public partial class MainWindow : Window
    {
        private List<Subsystem> subsystems = new();

        public MainWindow()
        {
            InitializeComponent();

            // Schedule the configuration prompt after the window becomes visible
            Dispatcher.UIThread.Post(async () => await PromptForConfiguration(), DispatcherPriority.Background);
        }

        private void UpdateFanSpeed(object? sender, RoutedEventArgs e)
        {
            if (double.TryParse(TempInput.Text, out double temperature))
            {
                double speedPercentage = CalculateSpeedPercentage(temperature);
                FanSpeedLabel.Text = $"Fan Speed: {speedPercentage * 100:F2}%";
            }
            else
            {
                FanSpeedLabel.Text = "Invalid temperature!";
            }
        }

        private double CalculateSpeedPercentage(double temperature)
        {
            if (temperature >= 75) return 1.0;
            if (temperature <= 25) return 0.2;
            return 0.2 + (temperature - 25) / 50 * 0.8;
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
                    fans.Add(new Fan { FanId = j + 1 });
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
            var configuration = "";
            foreach (var subsystem in subsystems)
            {
                configuration += $"Subsystem {subsystem.SubsystemId}: {subsystem.Fans.Count} fans\n";
            }

            var messageDialog = new Dialogs.MessageDialog(configuration);
            await messageDialog.ShowAsync(this);
        }
    }
}
