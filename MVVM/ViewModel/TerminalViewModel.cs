using Escort_Tool.Core;
using Escort_Tool.MVVM.View;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Escort_Tool.MVVM.ViewModel
{
    public class TerminalViewModel : ObservableObject
    {
        public ObservableCollection<CommandElementViewModel> CommandElements { get; set; }
        public ObservableCollection<string> SendingModes { get; set; }
        
        private string _selectedSendingMode;

        public string SelectedSendingMode
        {
            get => _selectedSendingMode;
            set
            {
                if (_selectedSendingMode != value)
                {
                    _selectedSendingMode = value;

                    OnPropertyChanged();
                }
            }
        }

        public ICommand AddCommandElementCommand { get; }
        public ICommand RemoveCommandElementCommand { get; }

        public TerminalViewModel()
        {
            SendingModes = new ObservableCollection<string>
            {
                 "Send on Timer",
                 "Send on Prompt"
            };

            SelectedSendingMode = SendingModes[0]; // "Send on Timer"

            CommandElements = new ObservableCollection<CommandElementViewModel>();

            AddCommandElementCommand = new RelayCommand(_ => AddCommandElement());
            RemoveCommandElementCommand = new RelayCommand(obj => RemoveCommandElement((CommandElementViewModel)obj));
        }

        private void AddCommandElement()
        {
            var newElement = new CommandElementViewModel
            {
                SelectedSendingMode = SelectedSendingMode
            };
            CommandElements.Add(newElement);

        }

        private void RemoveCommandElement(CommandElementViewModel element)
        {
            element.Dispose();
            CommandElements.Remove(element);
        }
    }

}
