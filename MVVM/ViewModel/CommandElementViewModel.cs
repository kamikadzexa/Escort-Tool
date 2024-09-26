using Escort_Tool.Core;
using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace Escort_Tool.MVVM.ViewModel
{
    public class CommandElementViewModel : ObservableObject, IDisposable
    {
        private bool _CalcCRC;
        private string _CommandText;
        private string _VTimeText;
        private string _VPromptText;
        private string _TimeText;
        private string _PromptText;
        private bool _isSendingEnabled;
        private string _selectedSendingMode;
        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        public CommandElementViewModel()
        {
            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            TimeText = "1000";
            UpdateTimerInterval();
        }

        public bool CalcCRC
        {
            get => _CalcCRC;
            set
            {
                if (_CalcCRC != value)
                {
                    _CalcCRC = value;
                    OnPropertyChanged();
                }
            }
        }


        public string VTimeText
        {
            get => _VTimeText;
            set
            {
                if (_VTimeText != value)
                {
                    _VTimeText = value;
                    OnPropertyChanged();
                    if (_VTimeText == "Collapsed") { _timer.Stop(); }
                }
            }
        }

        public string VPromptText
        {
            get => _VPromptText;
            set { _VPromptText = value; OnPropertyChanged(); }
        }

        public string CommandText
        {
            get => _CommandText;
            set
            {
                if (_CommandText != value)
                {
                    _CommandText = MainWindow.Instance.HexStringToByteArrayAndBack(value, CalcCRC);
                    OnPropertyChanged();
                }
            }
        }

        public string PromptText
        {
            get => _PromptText;
            set 
            {
                if (_PromptText != value)
                {
                    _PromptText = MainWindow.Instance.HexStringToByteArrayAndBack(value, CalcCRC);
                }
                OnPropertyChanged(); 
            }
        }

        public string TimeText
        {
            get => _TimeText;
            set
            {
                if (_TimeText != value)
                {
                    _TimeText = value;
                    OnPropertyChanged();
                    UpdateTimerInterval();
                }
            }
        }
        public bool IsSendingEnabled
        {
            get => _isSendingEnabled;
            set
            {
                if (_isSendingEnabled != value)
                {
                    _isSendingEnabled = value;
                    OnPropertyChanged();
                    HandleSendingStateChanged();
                }
            }
        }

        public string SelectedSendingMode
        {
            get => _selectedSendingMode;
            set 
            {
                if (_selectedSendingMode != value)
                {
                    _selectedSendingMode = value;
                    IsSendingEnabled = false;

                    if (_selectedSendingMode == "Send on Prompt")
                    {
                        VTimeText = "Collapsed";
                        VPromptText = "Visible";

                    }
                    if (_selectedSendingMode == "Send on Timer")
                    {
                        VPromptText = "Collapsed";
                        VTimeText = "Visible";
                        TimeText = "1000";
                    }

                    OnPropertyChanged();
                }
            }
        }


        private void HandleSendingStateChanged()
        {
            if (IsSendingEnabled && SelectedSendingMode == "Send on Timer")
            {

                UpdateTimerInterval();
                // Start the timer
                _timer.Start();
            }
            else
            {
                // Stop the timer
                _timer.Stop();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Send();
            UpdateTimerInterval();
        }

        private void Send()
        {
             if (!string.IsNullOrEmpty(CommandText))
                {
                    MainWindow.Instance.ProcessAndSendCommand(CommandText);
                }
        }

        private void UpdateTimerInterval()
        {
                if (int.TryParse(TimeText, out int interval))
                {
                    _timer.Interval = TimeSpan.FromMilliseconds(interval);
                }
                else
                {
                    MainWindow.Instance.SetErrorText("Invalid period");
                    TimeText = "1000";
                    _timer.Interval = TimeSpan.FromMilliseconds(1000);
                }

        }

        // Implement IDisposable
        public void Dispose()
        {
            // Stop the timer and unsubscribe from events
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            // Dispose other resources if needed
            _stopwatch = null;
        }


    }
}
