using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace MPDCtrlX.ViewModels;

public class ViewModelBase : INotifyPropertyChanged//ObservableObject
{

    #region == INotifyPropertyChanged ==

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void NotifyPropertyChanged(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (PropertyChanged != null)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });

    }

    #endregion
}
