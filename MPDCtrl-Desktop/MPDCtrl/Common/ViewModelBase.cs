using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace MPDCtrl.Common
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo
    {
        #region == INotifyPropertyChanged ==

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            //this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (Application.Current is not null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }

        #endregion

        #region == IDataErrorInfo ==

        private static readonly Dictionary<string, string> _ErrorMessages = new();

        string IDataErrorInfo.Error => (_ErrorMessages.Count > 0) ? "Has Error" : "";

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (_ErrorMessages.ContainsKey(columnName))
                    return _ErrorMessages[columnName];
                else
                    return "";
            }
        }

        protected static void SetError(string propertyName, string ErrorMessage)
        {
            _ErrorMessages[propertyName] = ErrorMessage;
        }

        protected static void ClearError(string propertyName)
        {
            if (_ErrorMessages.ContainsKey(propertyName))
                //_ErrorMessages.Remove(propertyName);
                _ErrorMessages[propertyName] = "";
        }

        #endregion
    }
}
