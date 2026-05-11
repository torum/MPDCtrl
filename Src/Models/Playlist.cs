using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MPDCtrl.Models;

public partial class Playlist : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastModifiedFormated))]
    public partial string LastModified { get; set; } = string.Empty;

    public string LastModifiedFormated
    {
        get
        {
            DateTime lastModifiedDateTime = default; //new DateTime(1998,04,30)

            if (!string.IsNullOrEmpty(LastModified))
            {
                try
                {
                    lastModifiedDateTime = DateTime.Parse(LastModified, null, System.Globalization.DateTimeStyles.RoundtripKind);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Wrong LastModified timestamp format. " + LastModified);
                }
            }

            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return lastModifiedDateTime.ToString(culture);
        }
    }
}
