using System.Diagnostics;

namespace MPDCtrl.Models;

public class Status
{
    public enum MpdPlayState
    {
        Play, Pause, Stop
    };

    public MpdPlayState MpdState { get; set; }

    public int MpdVolume { get; set; } = 20;

    public bool MpdVolumeIsReturned { get; set; }

    public bool MpdVolumeIsSet { get; set; } = false;

    public bool MpdRepeat { get; set; }

    public bool MpdRandom { get; set; }

    public bool MpdConsume { get; set; }

    public bool MpdSingle { get; set; }

    public string MpdSongID { get; set; } = string.Empty;

    public double MpdSongTime { get; set; } = 0;

    public double MpdSongElapsed { get; set; } = 0;

    public string MpdError { get; set; } = string.Empty;

    public void Reset()
    {
        MpdVolume = 20;
        MpdVolumeIsSet = false;
        MpdVolumeIsReturned = false;
        MpdRepeat = false;
        MpdRandom = false;
        MpdConsume = false;
        MpdSongID = "";
        MpdSongTime = 0;
        MpdSongElapsed = 0;
        MpdError = "";
    }
}
