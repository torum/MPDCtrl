
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

/// <summary>
/// Base class for Treeview Node and Listview Item.
/// </summary>
public abstract partial class Node(string name) : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = name;

    [ObservableProperty]
    public partial string PathIcon { get; set; } = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
}

/// <summary>
/// Base class for Treeview Node.
/// </summary>
public partial class NodeTree : Node
{
    [ObservableProperty]
    public partial bool Selected { get; set; }

    [ObservableProperty]
    public partial bool Expanded { get; set; }

    [ObservableProperty]
    public partial string Tag { get; set; } = string.Empty;

    [ObservableProperty]
    public partial NodeTree? Parent { get; set; }

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
