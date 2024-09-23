using System;
using Escort_Tool.Core;
using Escort_Tool.MVVM.View;

namespace Escort_Tool.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {

        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand GenViewCommand { get; set; }
        public RelayCommand TerminalViewCommand { get; set; }

        public HomeViewModel HomeVm { get; set; }
        public TerminalViewModel TerminalVm { get; set; }
        public GenViewModel GenVm { get; set; }

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set 
            { 
                _currentView = value;
                OnPropertyChanged();
                    
            }
        }

        public MainViewModel()
        {
            HomeVm = new HomeViewModel();
            GenVm = new GenViewModel();
            TerminalVm = new TerminalViewModel();

            CurrentView = TerminalVm;

            HomeViewCommand = new RelayCommand(o => 
            {
                CurrentView = HomeVm;
            });

            GenViewCommand = new RelayCommand(o =>
            {
                CurrentView = GenVm;
            });
            TerminalViewCommand = new RelayCommand(o =>
            {
                CurrentView = TerminalVm;
            });
        }
    }
}
