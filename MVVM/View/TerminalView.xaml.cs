using Escort_Tool.Core;
using Escort_Tool.MVVM.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;

namespace Escort_Tool.MVVM.View
{
    /// <summary>
    /// Interaction logic for TerminalView.xaml
    /// </summary>
    public partial class TerminalView : UserControl
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        public static TerminalView Instance { get; private set; }

        private string buffer = "";
        private DispatcherTimer _timer;


        public TerminalView()
        {
            Instance = this;
            InitializeComponent();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;
            DataContext = new TerminalViewModel();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Output the buffer and add CRC BREAK if timer ticks
            if (!string.IsNullOrEmpty(buffer))
            {
                buffer = "XXX " + buffer + " XXX";
                AppendToTextBox(buffer);
                buffer = "";
            }
        }


        public void ReceiveCommand(string command)
        {
            string _formattedData = command;
            if (CheckCrcCheckBox.IsChecked == true)
            {
                string _lastTwoCharacters = _formattedData.Length >= 2 ? _formattedData.Substring(_formattedData.Length - 2) : _formattedData;
                string _ForCheck = _formattedData.Length >= 2 ? _formattedData.Substring(0, _formattedData.Length - 2) : "";
                if (_ForCheck.Length > 0 && MainWindow.Instance.GetCrc8HexString(MainWindow.Instance.HexStringToByteArray(_ForCheck)) == _lastTwoCharacters)
                {
                    if (TimeCheckBox.IsChecked == true)
                    {
                        // Get the elapsed time in milliseconds with high precision
                        var elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
                        var formattedTime = TimeSpan.FromMilliseconds(elapsedMilliseconds).ToString("mm':'ss':'fff") + " ";

                        _formattedData = "✔  " + formattedTime + (string)FindResource("Sent") + _formattedData;
                    }
                    else
                    {
                        _formattedData = "✔  " + (string)FindResource("Sent") + _formattedData;
                    }
                }
                else
                {
                    if (TimeCheckBox.IsChecked == true)
                    {
                        // Get the elapsed time in milliseconds with high precision
                        var elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
                        var formattedTime = TimeSpan.FromMilliseconds(elapsedMilliseconds).ToString("mm':'ss':'fff") + " ";

                        _formattedData = "X  " + formattedTime + (string)FindResource("Sent") + _formattedData;
                    }
                    else
                    {
                        _formattedData = "X  " + (string)FindResource("Sent") + _formattedData;
                    }
                }
            }
            else
            {
                if (TimeCheckBox.IsChecked == true)
                {
                    // Get the elapsed time in milliseconds with high precision
                    var elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
                    var formattedTime = TimeSpan.FromMilliseconds(elapsedMilliseconds).ToString("mm':'ss':'fff") + " ";

                    _formattedData = formattedTime + (string)FindResource("Sent") + _formattedData;
                }
                else
                {
                    _formattedData = (string)FindResource("Sent") + _formattedData;
                }
            }

            AppendToTextBox(_formattedData);
        }

        public void ReceiveData(string data)
        {
            buffer += " " + data;

            // Restart the timer
            _timer.Stop();
            _timer.Start();

            string lastTwoCharacters = buffer.Length >= 2 ? buffer.Substring(buffer.Length - 2) : buffer;
            string ForCheck = buffer.Length >= 2 ? buffer.Substring(0, buffer.Length - 2) : "";

            if (CheckCrcCheckBox.IsChecked == true)
            {
                // CRC check is active
                if (ForCheck.Length > 0 && MainWindow.Instance.GetCrc8HexString(MainWindow.Instance.HexStringToByteArray(ForCheck)) == lastTwoCharacters)
                {
                    _timer.Stop(); // Stop the timer when valid CRC is found
                    string formattedData;
                    if (TimeCheckBox.IsChecked == true)
                    {
                        // Get the elapsed time in milliseconds with high precision
                        var elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
                        var formattedTime = TimeSpan.FromMilliseconds(elapsedMilliseconds).ToString("mm':'ss':'fff");

                        formattedData = $"{formattedTime} {buffer}";
                    }
                    else
                    {
                        formattedData = buffer;
                    }
                    formattedData = "✔  " + formattedData;
                    AppendToTextBox(formattedData);
                    var viewModel = DataContext as TerminalViewModel;
                    foreach (var element in viewModel.CommandElements)
                    {
                        if (buffer != "" && buffer != null && buffer != " " && element.PromptText != null)
                        {
                            string _buffer = buffer.Replace(" ", "");
                            string _prompt = element.PromptText.Replace(" ", "");
                            if (element.IsSendingEnabled && element.SelectedSendingMode == "Send on Prompt" && _prompt == _buffer)
                            {
                                MainWindow.Instance.ProcessAndSendCommand(element.CommandText);
                            }
                        }
                    }
                    buffer = "";
                }
            }
            else
            {
                // CRC check is not active
                // Optionally handle cases where no CRC check is applied
                string formattedData;
                if (TimeCheckBox.IsChecked == true)
                {
                    // Get the elapsed time in milliseconds with high precision
                    var elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;
                    var formattedTime = TimeSpan.FromMilliseconds(elapsedMilliseconds).ToString("mm':'ss':'fff");

                    formattedData = $"{formattedTime} {buffer}";
                }
                else
                {
                    formattedData = buffer;
                }
                AppendToTextBox(formattedData);
                var viewModel = DataContext as TerminalViewModel;
                foreach (var element in viewModel.CommandElements)
                {
                    if (buffer != "" && buffer != null && buffer != " " && element.PromptText != null)
                    {
                        string _buffer = buffer.Replace(" ", "");                        
                        string _prompt = element.PromptText.Replace(" ", "");
                        if (element.IsSendingEnabled && element.SelectedSendingMode == "Send on Prompt" && _prompt == _buffer)
                        {
                            MainWindow.Instance.ProcessAndSendCommand(element.CommandText);
                        }
                    }
                }
                buffer = "";
            }
        }


        private void AppendToTextBox(string text)
        {
            if (AutoScrollCheckBox.IsChecked == true)
            {
                ReceivedDataTextBox.Text += text + Environment.NewLine;
                ReceivedDataTextBox.ScrollToEnd();
            }
            else
            {
                ReceivedDataTextBox.Text += text + Environment.NewLine;
            }
        }

        private void Command_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(sender, e);
            }
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ReceivedDataTextBox.Clear();
        }


        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

            string command = Command.Text.Replace(" ","");

            if (string.IsNullOrEmpty(command) || command.Length < 2)
            {
                MainWindow.Instance.SetErrorText((string)FindResource("Please enter a command"));
                return;
            }
            if (command.Length % 2 != 0)
            {
                MainWindow.Instance.SetErrorText((string)FindResource("Invalid length"));
                return;
            }
            try
            {
                if (CrcCheckBox.IsChecked == true && command.Length > 2)
                {
                    string lastTwoCharacters = command.Length >= 2 ? command.Substring(command.Length - 2) : command;
                    string ForCheck = command.Length >= 2 ? command.Substring(0, command.Length - 2) : "";
                    if (ForCheck.Length > 0 && MainWindow.Instance.GetCrc8HexString(MainWindow.Instance.HexStringToByteArray(ForCheck)) == lastTwoCharacters)
                    {
                        Command.Text = MainWindow.Instance.HexStringToByteArrayAndBack(Command.Text, false);
                    }
                    else
                    {
                        Command.Text = MainWindow.Instance.HexStringToByteArrayAndBack(Command.Text, true);
                    }
                }
                else
                {
                    Command.Text = MainWindow.Instance.HexStringToByteArrayAndBack(Command.Text, false);
                }
                command = Command.Text;
                MainWindow.Instance.ProcessAndSendCommand(command);
            }
            catch
            {
                MainWindow.Instance.SetErrorText((string)FindResource("Wrong symbols"));
            }

        }

    }
}
