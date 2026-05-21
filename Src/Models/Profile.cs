
using CommunityToolkit.Mvvm.ComponentModel;

namespace MPDCtrl.Models;

public sealed partial class Profile : ObservableObject
{
    [ObservableProperty]
    public partial string Host { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Port { get; set; } = 6600;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDefault { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; } = 50;
}
