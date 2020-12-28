using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MPDCtrl.ViewModels.Classes;
using MPDCtrl.Common;

namespace MPDCtrl.ViewModels
{
    /// <summary>
    /// Debug Window ViewModel
    /// </summary>
    public class DebugViewModel : ViewModelBase
    {
        private string _debugText;
        public string DebugText
        {
            get
            {
                return _debugText;
            }
            set
            {
                if (_debugText == value)
                    return;

                _debugText = value;

                NotifyPropertyChanged(nameof(DebugText));
            }
        }

        public void OnDebugOutput(string data)
        {
            data = data.Trim('\r', '\n');
            data = data.Trim();

            if (!string.IsNullOrEmpty(data))
                DebugText += data + Environment.NewLine + Environment.NewLine;
        }

        public DebugViewModel()
        {
            ClearCommand = new RelayCommand(ClearCommandExecute, ClearCommand_CanExecute);
        }

        public ICommand ClearCommand { get; }
        public bool ClearCommand_CanExecute()
        {
            if (DebugText == "")
                return false;

            return true;
        }
        public void ClearCommandExecute()
        {
            DebugText = "";
        }

    }

}
