using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MPDCtrl.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public ViewModelBase()
        {
        }

        #region == INotifyPropertyChanged ==
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            /*
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
            */
        }

        /*
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        */

        #endregion
    }
}
