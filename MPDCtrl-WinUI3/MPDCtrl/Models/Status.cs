using System.Diagnostics;

namespace MPDCtrl.Models;

public class Status
{
    public enum MpdPlayState
    {
        Play, Pause, Stop
    };

    private MpdPlayState _ps;
    public MpdPlayState MpdState
    {
        get { return _ps; }
        set { _ps = value; }
    }

    private int _volume = 20;
    public int MpdVolume
    {
        get { return _volume; }
        set
        {
            _volume = value;
        }
    }

    private bool _volumeIsReturned;
    public bool MpdVolumeIsReturned
    {
        get { return _volumeIsReturned; }
        set
        {
            _volumeIsReturned = value;
        }
    }

    private bool _volumeIsSet = false;
    public bool MpdVolumeIsSet
    {
        get { return _volumeIsSet; }
        set
        {
            _volumeIsSet = value;
        }
    }

    private bool _repeat;
    public bool MpdRepeat
    {
        get { return _repeat; }
        set
        {
            _repeat = value;
        }
    }

    private bool _random;
    public bool MpdRandom
    {
        get { return _random; }
        set
        {
            _random = value;
        }
    }

    private bool _consume;
    public bool MpdConsume
    {
        get { return _consume; }
        set
        {
            _consume = value;
        }
    }

    private bool _single;
    public bool MpdSingle
    {
        get { return _single; }
        set
        {
            _single = value;
        }
    }

    private string _songID = string.Empty;
    public string MpdSongID
    {
        get { return _songID; }
        set
        {
            _songID = value;
        }
    }

    private double _songTime = 0;
    public double MpdSongTime
    {
        get { return _songTime; }
        set
        {
            _songTime = value;
        }
    }

    private double _songElapsed = 0;
    public double MpdSongElapsed
    {
        get { return _songElapsed; }
        set
        {
            _songElapsed = value;
        }
    }

    private string _error = string.Empty;
    public string MpdError
    {
        get { return _error; }
        set
        {
            _error = value;
        }
    }

    public void Reset()
    {
        _volume = 20;
        _volumeIsSet = false;
        _volumeIsReturned = false;
        _repeat = false;
        _random = false;
        _consume = false;
        _songID = "";
        _songTime = 0;
        _songElapsed = 0;
        _error = "";
    }
}
