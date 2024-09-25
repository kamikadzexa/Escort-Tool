using Escort_Tool.Core;
using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace Escort_Tool.MVVM.ViewModel
{
    public class CommandElementViewModel : ObservableObject, IDisposable
    {
        private string _commandText;
        private string _TimeOrPromptText;
        private string _promptText;
        private bool _isSendingEnabled;
        private string _selectedSendingMode;
        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        public CommandElementViewModel()
        {
            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;

            // Initialize the timer with a default period (e.g., 1 second)
            TimeOrPromptText = "1000";
            UpdateTimerInterval();
        }
        

        public string CommandText
        {
            get => _commandText;
            set { _commandText = value; OnPropertyChanged(); }
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
                _selectedSendingMode = value;
                IsSendingEnabled = false;
                HandleSendingStateChanged();

                if (_selectedSendingMode == "Send on Prompt")
                {
                    TimeOrPromptText = "";

                }
                if (_selectedSendingMode == "Send on Timer")
                {
                    TimeOrPromptText = "1000";
                }

                OnPropertyChanged(); 

            }
        }

        public string TimeOrPromptText
        {
            get => _TimeOrPromptText;
            set
            {
                if (_TimeOrPromptText != value)
                {
                    _TimeOrPromptText = value;
                    OnPropertyChanged();
                    UpdateTimerInterval();
                }
            }
        }



        private void HandleSendingStateChanged()
        {
            if (IsSendingEnabled)
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
            if (CommandText != null)
            {
                MainWindow.Instance.ProcessAndSendCommand(CommandText);
            }
        }

        private void UpdateTimerInterval()
        {
            if (_selectedSendingMode == "Send on Timer")
            {
                if (int.TryParse(TimeOrPromptText, out int interval))
                {
                    _timer.Interval = TimeSpan.FromMilliseconds(interval);
                }
                else
                {
                    MainWindow.Instance.SetErrorText("Invalid period");
                    TimeOrPromptText = "1000";
                    _timer.Interval = TimeSpan.FromMilliseconds(1000);
                }
            }
            if(_selectedSendingMode == "Send on Prompt")
            {
                _timer.Interval = TimeSpan.FromMilliseconds(10000);
                _timer.Stop();
            }
            if (_selectedSendingMode == null)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(10000);
                _timer.Stop();
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
