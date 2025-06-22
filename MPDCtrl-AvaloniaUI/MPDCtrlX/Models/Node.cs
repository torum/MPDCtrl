using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace MPDCtrlX.Models;

/// <summary>
/// Base class for Treeview Node and Listview Item.
/// </summary>
abstract public class Node : INotifyPropertyChanged
{
    private string _name;
    public string Name
    {
        get
        {
            return _name;
        }
        set
        {
            if (_name == value)
                return;

            _name = value;

            NotifyPropertyChanged(nameof(Name));
        }
    }

    private string _pathData = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
    public string PathIcon
    {
        get
        {
            return _pathData;
        }
        protected set
        {
            if (_pathData == value)
                return;
            _pathData = value;

            NotifyPropertyChanged(nameof(PathIcon));
        }
    }

    protected Node(string name)
    {
        _name = name;
    }

    #region == INotifyPropertyChanged ==

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void NotifyPropertyChanged(string propertyName)
    {
        //Application.Current.Dispatcher.Invoke(() =>
        Dispatcher.UIThread.Post(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }

    #endregion
}

/// <summary>
/// Base class for Treeview Node.
/// </summary>
public class NodeTree : Node
{
    private bool _selected;
    public bool Selected
    {
        get
        {
            return _selected;
        }
        set
        {
            if (_selected == value)
                return;

            _selected = value;

            NotifyPropertyChanged(nameof(Selected));
        }
    }

    private bool _expanded;
    public bool Expanded
    {
        get
        {
            return _expanded;
        }
        set
        {
            if (_expanded == value)
                return;

            _expanded = value;

            NotifyPropertyChanged(nameof(Expanded));
        }
    }

    private NodeTree? _parent;
    public NodeTree? Parent
    {
        get
        {
            return _parent;
        }

        set
        {
            if (_parent == value)
                return;


            _parent = value;

            NotifyPropertyChanged(nameof(Parent));
        }
    }

    private ObservableCollection<NodeTree> _children = [];
    public ObservableCollection<NodeTree> Children
    {
        get
        {
            return _children;
        }
        set
        {
            _children = value;

            NotifyPropertyChanged(nameof(Children));
        }
    }

    protected NodeTree(string name) : base(name)
    {
        //BindingOperations.EnableCollectionSynchronization(_children, new object());
    }

}
