namespace MPDCtrlX.Models;

public class Status
{
    public enum MpdPlayState
    {
        Play, Pause, Stop
    };

    private MpdPlayState _ps;
    private int _volume = 50;
    private bool _volumeIsSet;
    private bool _repeat;
    private bool _random;
    private bool _consume;
    private bool _single;
    private string _songID = "";
    private double _songTime = 0;
    private double _songElapsed = 0;
    private string _error = "";

    public MpdPlayState MpdState
    {
        get { return _ps; }
        set { _ps = value; }
    }

    public int MpdVolume
    {
        get { return _volume; }
        set
        {
            _volume = value;
        }
    }

    public bool MpdVolumeIsSet
    {
        get { return _volumeIsSet; }
        set
        {
            _volumeIsSet = value;
        }
    }

    public bool MpdRepeat
    {
        get { return _repeat; }
        set
        {
            _repeat = value;
        }
    }

    public bool MpdRandom
    {
        get { return _random; }
        set
        {
            _random = value;
        }
    }
    public bool MpdConsume
    {
        get { return _consume; }
        set
        {
            _consume = value;
        }
    }

    public bool MpdSingle
    {
        get { return _single; }
        set
        {
            _single = value;
        }
    }

    public string MpdSongID
    {
        get { return _songID; }
        set
        {
            _songID = value;
        }
    }

    public double MpdSongTime
    {
        get { return _songTime; }
        set
        {
            _songTime = value;
        }
    }

    public double MpdSongElapsed
    {
        get { return _songElapsed; }
        set
        {
            _songElapsed = value;
        }
    }
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
        _volume = 50;
        _volumeIsSet = false;
        _repeat = false;
        _random = false;
        _consume = false;
        _songID = "";
        _songTime = 0;
        _songElapsed = 0;
        _error = "";
    }
}
