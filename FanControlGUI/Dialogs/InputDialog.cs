using Avalonia;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace FanControlGUI.Dialogs
{
    public class InputDialog : Window
    {
        private TextBox _inputBox;
        private TaskCompletionSource<string> _resultCompletionSource;

        public InputDialog(string prompt)
        {
            Title = "Number of Subsystems";
            Width = 800;
            Height = 250;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock { Text = prompt });

            _inputBox = new TextBox { Margin = new Thickness(0, 10, 0, 10) };
            stackPanel.Children.Add(_inputBox);

            var button = new Button { Content = "OK" };
            button.Click += (sender, e) => CloseDialog();

            stackPanel.Children.Add(button);
            Content = stackPanel;

            _resultCompletionSource = new TaskCompletionSource<string>();
        }

        private void CloseDialog()
        {
            _resultCompletionSource.SetResult(_inputBox.Text ?? string.Empty);
            Close();
        }

        public Task<string> ShowAsync(Window owner)
        {
            ShowDialog(owner);
            return _resultCompletionSource.Task;
        }
    }
}
