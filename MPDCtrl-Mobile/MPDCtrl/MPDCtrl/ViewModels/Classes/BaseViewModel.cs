using MPDCtrl.Models;
using MPDCtrl.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace MPDCtrl.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        public IDataStore<Item> DataStore => DependencyService.Get<IDataStore<Item>>();

        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            //changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Device.BeginInvokeOnMainThread(() =>
            {
                changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Device.BeginInvokeOnMainThread(() =>
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        #endregion

        #region == IDataErrorInfo ==

        private Dictionary<string, string> _ErrorMessages = new Dictionary<string, string>();

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

        protected void SetError(string propertyName, string errorMessage)
        {
            _ErrorMessages[propertyName] = errorMessage;
        }

        protected void ClearErrror(string propertyName)
        {
            if (_ErrorMessages.ContainsKey(propertyName))
                //_ErrorMessages.Remove(propertyName);
                _ErrorMessages[propertyName] = "";
        }

        #endregion
    }
}
