using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

/// <summary>
/// Base class for Treeview Node.
/// </summary>
public partial class NodeTree : Node
{
    protected NodeTree(string name) : base(name)
    {

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

    [ObservableProperty]
    public partial bool Selected { get; set; }

    [ObservableProperty]
    public partial bool Expanded { get; set; }

    [ObservableProperty]
    public partial string Tag { get; set; } = string.Empty;

    [ObservableProperty]
    public partial NodeTree? Parent { get; set; }

}

