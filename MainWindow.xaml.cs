using System;
using System.Windows;
using System.Windows.Input;
using System.IO.Ports;
using System.Windows.Threading;
using System.Windows.Interop;
using MessageBox = System.Windows.MessageBox;
using Escort_Tool.MVVM.View;
using Application = System.Windows.Application;
using Escort_Tool.MVVM.ViewModel;
using System.IO;
using System.Media;
using System.Reflection;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Controls;
using System.Globalization;
using System.Resources;
using System.Windows.Media.Imaging;

namespace Escort_Tool
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private DispatcherTimer _portCheckTimer;
        private string? _lastSelectedPort;
        private bool _isPortOpen = false;
        private MainViewModel _mainViewModel;
        public static MainWindow Instance { get; private set; }
        private DispatcherTimer clearTextTimer;


        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.LocationChanged += MainWindow_LocationChanged; // Handle window movement
            LoadAvailablePorts();
            InitializePortCheckTimer();
            InitializeComboBoxes();
            _mainViewModel = new MainViewModel();
            this.DataContext = _mainViewModel;
            clearTextTimer = new DispatcherTimer();
            clearTextTimer.Interval = TimeSpan.FromSeconds(5);
            clearTextTimer.Tick += ClearTextTimer_Tick;
        }



        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (LanguageComboBox.SelectedIndex is 0)
            {
                    ChangeLanguage("en-US");
                
            }
            if (LanguageComboBox.SelectedIndex is 1)
            {
                ChangeLanguage("ru-RU");
            }
        }
        private void ChangeLanguage(string language)
        {
            ResourceDictionary dict = new ResourceDictionary();

            switch (language)
            {
                case "en-US":
                    dict.Source = new Uri("Languages/English.xaml", UriKind.Relative);
                    break;
                case "ru-RU":
                    dict.Source = new Uri("Languages/Russian.xaml", UriKind.Relative);
                    break;
            }

            // Remove existing language dictionaries and apply the new one
            var mergedDicts = Application.Current.Resources.MergedDictionaries;
            if (mergedDicts.Count > 0)
            {
                mergedDicts.RemoveAt(0); // Remove the current language
            }
            mergedDicts.Add(dict); // Add the new language
        }


        private async void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] hexData = ReadSerialPort();
            string data = BitConverter.ToString(hexData).Replace("-", " ");

            Dispatcher.Invoke(() =>
            {
                if (_mainViewModel.CurrentView is TerminalViewModel terminalVm && !string.IsNullOrEmpty(data))
                {
                    TerminalView.Instance.ReceiveData(data);
                }
            });
        }

        private void InitializeComboBoxes()
        {
            BaudRateComboBox.ItemsSource = new int[] { 4800, 9600, 14400, 19200, 38400, 56000, 57600, 115200 };
            BaudRateComboBox.SelectedIndex = 3; // Default to 19200

            DataBitsComboBox.ItemsSource = new int[] { 5, 6, 7, 8 };
            DataBitsComboBox.SelectedIndex = 3; // Default to 8

            ParityComboBox.ItemsSource = Enum.GetValues(typeof(Parity));
            ParityComboBox.SelectedItem = Parity.None; // Default to None

            StopBitsComboBox.ItemsSource = Enum.GetValues(typeof(StopBits));
            StopBitsComboBox.SelectedItem = StopBits.One; // Default to One

            var image1 = new BitmapImage(new Uri("pack://application:,,,/Images/English.png"));
            var image2 = new BitmapImage(new Uri("pack://application:,,,/Images/Russian.png"));

            // Set the ComboBox items
            LanguageComboBox.ItemsSource = new[] { image1, image2 };
            LanguageComboBox.SelectedIndex = 0;
            

        }

        public static void PlayErrorSound()
        {
            try
            {
                // Retrieve the stream of the embedded sound file
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Escort_Tool.Sounds.Error.wav";
                using (Stream soundStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (soundStream != null)
                    {
                        using (SoundPlayer player = new SoundPlayer(soundStream))
                        {
                            player.Play();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Sound file not found in resources.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                MessageBox.Show("Error playing sound: " + ex.Message);
            }
        }

        public void SetErrorText(string message)
        {
            Error.Text = message;
            MainWindow.PlayErrorSound();

            // Start or restart the timer
            if (clearTextTimer.IsEnabled)
            {
                clearTextTimer.Stop();
            }
            clearTextTimer.Start();
        }

        private void ClearTextTimer_Tick(object sender, EventArgs e)
        {
            Error.Text = string.Empty;
            clearTextTimer.Stop(); // Stop the timer after clearing the text
        }


        public void ProcessAndSendCommand(string command)
        {
            // Replace spaces and $ in the command string
            string hexCommand = command.Replace(" ", "").Replace("$", "");

            // Send the processed command to the COM port
            SendCommandToComPort(hexCommand);
        }

        private void SendCommandToComPort(string hexCommand)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                SetErrorText((string)FindResource("COM port is not open"));
                return;
            }

            try
            {
                // Convert the hex string to a byte array
                byte[] bytesToSend = HexStringToByteArray(hexCommand);

                // Send the byte array to the COM port
                _serialPort.Write(bytesToSend, 0, bytesToSend.Length);

                // Format the sent command as hex with spaces for display
                string formattedCommand = BitConverter.ToString(bytesToSend).Replace("-", " ");

                // Update the output with the sent command
                Dispatcher.Invoke(() =>
                {
                    if (_mainViewModel.CurrentView is TerminalViewModel terminalVm)
                    {
                         TerminalView.Instance.ReceiveCommand(formattedCommand);
                        
                    }

                });
            }
            catch (Exception ex)
            {
                SetErrorText((string)FindResource("Wrong symbols or length"));
            }
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



        private byte[] ReadSerialPort()
        {
            int bytesToRead = _serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            _serialPort.Read(buffer, 0, bytesToRead);
            return buffer;
        }



        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            UpdateMaxHeight(); // Set initial MaxHeight based on the current screen
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            UpdateMaxHeight(); // Update MaxHeight whenever the window is moved
        }


        private void UpdateMaxHeight()
        {
            var handle = new WindowInteropHelper(this).Handle;
            // Get the screen where the window is currently located
            Screen currentScreen = System.Windows.Forms.Screen.FromHandle(handle);

            // Get the DPI scaling factor for the current screen
            var source = PresentationSource.FromVisual(this);
            double dpiY = 96.0; // Default DPI
            if (source != null)
            {
                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            }

            // Adjust the work area height for DPI scaling
            double adjustedHeight = currentScreen.WorkingArea.Height * (96.0 / dpiY);

            this.MaxHeight = adjustedHeight + 8;

        }


        private void InitializePortCheckTimer()
        {
            _portCheckTimer = new DispatcherTimer();
            _portCheckTimer.Interval = TimeSpan.FromSeconds(1); // Check every second
            _portCheckTimer.Tick += PortCheckTimer_Tick;
            _portCheckTimer.Start(); // Start the port checking timer
        }

        private void LoadAvailablePorts()
        {
            var ports = SerialPort.GetPortNames();
            PortComboBox.ItemsSource = ports;

            if (ports.Length > 0)
            {
                PortComboBox.SelectedIndex = PortComboBox.Items.Count - 1;
            }
            else
            {
                PortComboBox.SelectedIndex = -1; // No selection if no items are available
            }
        }

        private void PortCheckTimer_Tick(object sender, EventArgs e)
        {

            string[] availablePorts = SerialPort.GetPortNames();

            // Update the PortComboBox if the list of ports has changed
            if (!availablePorts.SequenceEqual(PortComboBox.ItemsSource.Cast<string>()))
            {
                PortComboBox.ItemsSource = availablePorts;

                if (availablePorts.Length > 0)
                {
                    PortComboBox.SelectedIndex = PortComboBox.Items.Count - 1;
                }
                else
                {
                    PortComboBox.SelectedIndex = -1; // No selection if no items are available
                }
            }

            // Check if the currently connected port is still available
            if (_serialPort != null && !availablePorts.Contains(_lastSelectedPort) && _lastSelectedPort != null)
            {
                // Port has been disconnected
                Dispatcher.Invoke(() =>
                {
                SetErrorText($"{_lastSelectedPort}" + " " + (string)FindResource("disconnected")); 
                    _serialPort.Close();
                    _lastSelectedPort = null;
                    TogglePortButton.Content = (string)FindResource("Open Port"); // Update button content
                    _isPortOpen = false;
                    PortComboBox.IsEnabled = true; // Re-enable the ComboBox
                });
            }
        }


        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void TogglePortButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedPort = PortComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedPort))
            {
                SetErrorText((string)FindResource("Please select a COM port"));
                return;
            }

            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Close();
                    TogglePortButton.Content = (string)FindResource("Open Port"); 
                    _isPortOpen = false;
                    PortComboBox.IsEnabled = true; // Re-enable the ComboBox
                    BaudRateComboBox.IsEnabled = true;
                    DataBitsComboBox.IsEnabled = true;
                    ParityComboBox.IsEnabled = true;
                    StopBitsComboBox.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    SetErrorText("Error closing port");
                }
            }
            else
            {
                // Open the port if it's not open
                _serialPort = new SerialPort(selectedPort)
                {
                    BaudRate = (int)BaudRateComboBox.SelectedItem,
                    DataBits = (int)DataBitsComboBox.SelectedItem,
                    Parity = (Parity)ParityComboBox.SelectedItem,
                    StopBits = (StopBits)StopBitsComboBox.SelectedItem,                    
                    Handshake = Handshake.None,
                    ReadTimeout = 500,
                    WriteTimeout = 500
                }; 

                try
                {
                    _serialPort.Open();
                    _serialPort.DataReceived += OnDataReceived;
                    TogglePortButton.Content = (string)FindResource("Close Port");
                    _isPortOpen = true;
                    PortComboBox.IsEnabled = false; // Disable the ComboBox
                    BaudRateComboBox.IsEnabled = false;
                    DataBitsComboBox.IsEnabled = false;
                    ParityComboBox.IsEnabled = false;
                    StopBitsComboBox.IsEnabled = false;
                    _lastSelectedPort = selectedPort; // Save the selected port
                }
                catch (Exception ex)
                {
                    SetErrorText((string)FindResource("Cannot open Com port"));
                }
            }
        }
    }
}