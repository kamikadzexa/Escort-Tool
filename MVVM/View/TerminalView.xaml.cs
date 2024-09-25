using System.Diagnostics;
using System.Windows;
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
                if (_ForCheck.Length > 0 && GetCrc8HexString(HexStringToByteArray(_ForCheck)) == _lastTwoCharacters)
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
                        var formattedTime = TimeSpan.FromMilliseconds(elapsedMilliseconds).ToString("mm':'ss':'fff" + " ");

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
                    var formattedTime = TimeSpan.FromMilliseconds(elapsedMilliseconds).ToString("mm':'ss':'fff" + " ");

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
                if (ForCheck.Length > 0 && GetCrc8HexString(HexStringToByteArray(ForCheck)) == lastTwoCharacters)
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
                Command.Focus();
                string command = Command.Text;

                if (string.IsNullOrEmpty(command))
                {
                    MainWindow.Instance.SetErrorText((string)FindResource("Please enter a command"));
                    return;
                }

                if (CrcCheckBox.IsChecked == true)
                {
                    try
                    {
                        byte[] HexCommand = HexStringToByteArray(command);
                        command += GetCrc8HexString(HexCommand);
                        MainWindow.Instance.ProcessAndSendCommand(command);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Instance.SetErrorText((string)FindResource("Wrong symbols or length"));
                    }
                }
                else
                {
                    MainWindow.Instance.ProcessAndSendCommand(command);
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ReceivedDataTextBox.Clear();
        }


        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

            string command = Command.Text;

            if (string.IsNullOrEmpty(command))
            {
                MainWindow.Instance.SetErrorText((string)FindResource("Please enter a command"));
                return;
            }
            if (CrcCheckBox.IsChecked == true)
            {

                try
                {
                    byte[] HexCommand = HexStringToByteArray(command);
                    command += GetCrc8HexString(HexCommand);
                    MainWindow.Instance.ProcessAndSendCommand(command);
                }
                catch (Exception ex)
                {
                    MainWindow.Instance.SetErrorText((string)FindResource("Wrong symbols or length"));
                }

            }
            else
            {
                MainWindow.Instance.ProcessAndSendCommand(command);
            }
        }

        private const byte Polynomial = 0x31; // Polynomial x^8 + x^5 + x^4 + 1 (0x31 in hexadecimal)

        private static byte CalculateCrc8(byte[] data)
        {
            byte crc = 0; // Initialize CRC value to 0
            foreach (byte b in data)
            {
                crc = Crc8(b, crc); // Calculate CRC for each byte
            }
            return crc;
        }

        // CRC-8 calculation method based on provided algorithm
        private static byte Crc8(byte data, byte crc)
        {
            byte i = (byte)(data ^ crc);
            crc = 0;
            if ((i & 0x01) != 0) crc ^= 0x5e;
            if ((i & 0x02) != 0) crc ^= 0xbc;
            if ((i & 0x04) != 0) crc ^= 0x61;
            if ((i & 0x08) != 0) crc ^= 0xc2;
            if ((i & 0x10) != 0) crc ^= 0x9d;
            if ((i & 0x20) != 0) crc ^= 0x23;
            if ((i & 0x40) != 0) crc ^= 0x46;
            if ((i & 0x80) != 0) crc ^= 0x8c;
            return crc;
        }

        // Method to calculate CRC-8 and return as a hex string
        private static string GetCrc8HexString(byte[] data)
        {
            byte crc = CalculateCrc8(data);
            return crc.ToString("X2"); // Convert CRC value to hex string
        }

        private byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", ""); // Remove any spaces
            hex = hex.Replace("$", "");
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public void Fsend(string commmmand)
        {
            MainWindow.Instance.ProcessAndSendCommand(commmmand);
        }

    }
}
