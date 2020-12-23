using System;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Models
{
    public class SongInfo : Song
    {
        // Queue specific

        public string Id { get; set; }

        private string _pos;
        public string Pos
        {
            get
            {
                return _pos;
            }
            set
            {
                if (_pos == value)
                    return;

                _pos = value;
                NotifyPropertyChanged("Pos");
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
                if (_isPlaying == value)
                    return;

                _isPlaying = value;
                NotifyPropertyChanged("IsPlaying");
            }
        }


    }


}
