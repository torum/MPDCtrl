namespace MPDCtrl.Models;

public sealed class MpdStatus
{
    public MpdPlayState CurrentPlayState { get; set; }

    public int CurrentVolume { get; set; } = 20;

    public bool IsVolumeReturned { get; set; }

    public bool IsVolumeSet { get; set; } = false;

    public bool IsRepeat { get; set; }

    public bool IsRandom { get; set; }

    public bool IsConsume { get; set; }

    public bool IsSingle { get; set; }

    public string CurrentSongID { get; set; } = string.Empty;

    public double CurrentSongTime { get; set; } = 0;

    public double CurrentSongElapsed { get; set; } = 0;

    public string CurrentError { get; set; } = string.Empty;

    public void Reset()
    {
        CurrentVolume = 20;
        IsVolumeSet = false;
        IsVolumeReturned = false;
        IsRepeat = false;
        IsRandom = false;
        IsConsume = false;
        CurrentSongID = string.Empty;
        CurrentSongTime = 0;
        CurrentSongElapsed = 0;
        CurrentError = string.Empty;
    }
}
