using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MPDCtrl.ViewModels;

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
    }

}
