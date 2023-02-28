using ReactiveUI;
using System.Collections.Generic;
using System.ComponentModel;

namespace MPDCtrl.ViewModels
{
    public class ViewModelBase : ReactiveObject, IDataErrorInfo
    {
        #region == IDataErrorInfo ==

        private static readonly Dictionary<string, string> _ErrorMessages = new();

        string IDataErrorInfo.Error
        {
            get { return (_ErrorMessages.Count > 0) ? "Has Error" : null; }
        }

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