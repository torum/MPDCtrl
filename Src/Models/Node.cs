
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

/// <summary>
/// Base class for Treeview Node and Listview Item.
/// </summary>
public abstract class Node(string name) : ObservableObject
{
    public string Name
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    } = name;

    public string PathIcon
    {
        get;
        protected set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    } = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
}

/// <summary>
/// Base class for Treeview Node.
/// </summary>
public partial class NodeTree : Node
{
    public bool Selected
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    }

    public bool Expanded
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    }

    public string Tag
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    } = string.Empty;

    public NodeTree? Parent
    {
        get;

        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<NodeTree> Children
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    } = [];

    protected NodeTree(string name) : base(name)
    {

    }

}
