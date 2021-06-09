using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Models
{
    public class Playlist
    {
        public string Name { get; set; } = "";

        private string _lastModified;
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

    }
}
