using CommunityToolkit.Mvvm.ComponentModel;
using MPDCtrl.ViewModels;
using System;
namespace MPDCtrl.Models;

/// <summary>
/// Generic song file class. (for listall)
/// </summary>
public partial class SongFile : ObservableObject
{
    public string File { get; set; } = string.Empty;
}

/// <summary>
/// SongInfo class. Extends SongFile. (for playlist or search result)
/// </summary>

public partial class SongInfo : SongFile
{
    // Workaround for WinUI3's limitation or lack of features. 
    public MainViewModel? ParentViewModel { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Track { get; set; } = string.Empty;
    public int TrackSort
    {
        get
        {
            int iTrack = 0;
            if (string.IsNullOrEmpty(Disc))
            {
                return iTrack;
            }
            try
            {
                iTrack = int.Parse(Track);
            }
            catch { }
            return iTrack;
        }
    }
    public string Disc { get; set; } = string.Empty;
    public int DiscSort
    {
        get
        {
            int iDisc = 0;
            if (string.IsNullOrEmpty(Disc))
            {
                return iDisc;
            }
            try
            {
                iDisc = int.Parse(Disc);
            }
            catch { }
            return iDisc;
        }
    }
    public string Time { get; set; } = string.Empty;
    public string TimeFormated
    {
        get
        {
            var timeFormatted = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(Time))
                {
                    int sec, min, hour, s;

                    double dtime = double.Parse(Time);
                    sec = Convert.ToInt32(dtime);

                    //sec = Int32.Parse(_time);
                    min = sec / 60;
                    s = sec % 60;
                    hour = min / 60;
                    min %= 60;

                    if ((hour == 0) && min == 0)
                    {
                        timeFormatted = $"{s}";
                    }
                    else if ((hour == 0) && (min != 0))
                    {
                        timeFormatted = $"{min}:{s:00}";
                    }
                    else if ((hour != 0) && (min != 0))
                    {
                        timeFormatted = $"{hour}:{min:00}:{s:00}";
                    }
                    else if (hour != 0)
                    {
                        timeFormatted = $"{hour}:{min:00}:{s:00}";
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Oops@TimeFormated: " + Time + " : " + hour.ToString() + " " + min.ToString() + " " + s.ToString());
                    }
                }
            }
            catch (FormatException e)
            {
                // Ignore.
                // System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine("Wrong Time format. " + Time + " " + e.Message);
            }

            return timeFormatted;
        }

    }
    public double TimeSort
    {
        get
        {
            var dtime = double.NaN;
            try
            {
                dtime = double.Parse(Time);
            }
            catch { }
            return dtime;
        }
    }
    public string Duration { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string AlbumArtist { get; set; } = string.Empty;
    public string Composer { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;

    public string LastModified
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(LastModifiedFormated));
        }
    } = string.Empty;

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
            else
            {
                return String.Empty;
            }

            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return lastModifiedDateTime.ToString(culture);
        }
    }

    // for sorting and (playlist pos)
    public int Index
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(IndexPlusOne));
        }
    }

    public bool IsSelected
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

    public int IndexPlusOne => Index + 1;
}

/// <summary>
/// Song class with some extra info. Extends SongInfo. (for queue)
/// </summary>
public partial class SongInfoEx : SongInfo
{
    // Queue specific

    public string Id { get; set; } = string.Empty;

    public string Pos
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

    public bool IsPlaying
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

    public bool IsAlbumCoverNeedsUpdate
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            OnPropertyChanged();
        }
    } = true;
}
