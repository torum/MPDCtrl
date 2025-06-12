using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MPDCtrlX.Models;

/// <summary>
/// Generic song file class. (for listall)
/// </summary>
public partial class SongFile : ObservableObject
{
    public string File { get; set; } = "";
}

/// <summary>
/// SongInfo class. Extends SongFile. (for playlist or search result)
/// </summary>

public partial class SongInfo : SongFile
{
    public string Title { get; set; } = "";
    public string Track { get; set; } = "";
    public string Disc { get; set; } = "";
    public string Time { get; set; } = "";
    public string TimeFormated
    {
        get
        {
            string _timeFormatted = "";
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
                        _timeFormatted = String.Format("{0}", s);
                    }
                    else if ((hour == 0) && (min != 0))
                    {
                        _timeFormatted = String.Format("{0}:{1:00}", min, s);
                    }
                    else if ((hour != 0) && (min != 0))
                    {
                        _timeFormatted = String.Format("{0}:{1:00}:{2:00}", hour, min, s);
                    }
                    else if (hour != 0)
                    {
                        _timeFormatted = String.Format("{0}:{1:00}:{2:00}", hour, min, s);
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

            return _timeFormatted;
        }

    }
    public double TimeSort
    {
        get
        {
            double dtime = double.NaN;
            try
            {
                dtime = double.Parse(Time);
            }
            catch { }
            return dtime;
        }
    }
    public string Duration { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public string AlbumArtist { get; set; } = "";
    public string Composer { get; set; } = "";
    public string Date { get; set; } = "";
    public string Genre { get; set; } = "";
    
    private string _lastModified = "";
    public string LastModified
    {
        get
        {
            return _lastModified;
        }
        set
        {
            if (_lastModified == value)
                return;

            _lastModified = value;
        }
    }

    public string LastModifiedFormated
    {
        get
        {
            DateTime _lastModifiedDateTime = default; //new DateTime(1998,04,30)

            if (!string.IsNullOrEmpty(_lastModified))
            {
                try
                {
                    _lastModifiedDateTime = DateTime.Parse(_lastModified, null, System.Globalization.DateTimeStyles.RoundtripKind);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Wrong LastModified timestamp format. " + _lastModified);
                }
            }

            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return _lastModifiedDateTime.ToString(culture);
        }
    }

    // for sorting and (playlist pos)
    private int _index;
    public int Index
    {
        get
        {
            return _index;
        }
        set
        {
            if (SetProperty(ref _index, value))
            {
                //
            }
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get
        {
            return _isSelected;
        }
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                //
            }
        }
    }

    public int IndexPlusOne
    {
        get
        {
            return _index+1;
        }
    }
}

/// <summary>
/// Song class with some extra info. Extends SongInfo. (for queue)
/// </summary>
public partial class SongInfoEx : SongInfo
{
    // Queue specific

    public string Id { get; set; } = "";

    private string _pos = "";
    public string Pos
    {
        get
        {
            return _pos;
        }
        set
        {
            if (SetProperty(ref _pos, value))
            {
                //
            }
        }
    }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get
        {
            return _isPlaying;
        }
        set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                //
            }
        }
    }
}
