using Avalonia.Controls;
using System.Threading.Tasks;

namespace FanControlGUI.Dialogs
{
    public class MessageDialog : Window
    {
        public MessageDialog(string message)
        {
            Title = "Message";
            Width = 300;
            Height = 150;

            var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(10) };
            stackPanel.Children.Add(new TextBlock { Text = message });

            var button = new Button { Content = "OK" };
            button.Click += (sender, e) => Close();

            stackPanel.Children.Add(button);
            Content = stackPanel;
        }

        public Task ShowAsync(Window owner)
        {
            var completionSource = new TaskCompletionSource();
            Closed += (sender, e) => completionSource.SetResult();
            ShowDialog(owner);
            return completionSource.Task;
        }
    }
}
