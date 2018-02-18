using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WpfMPD
{
    public class MPC
    {
        /// <summary>
        /// Song Class 
        /// </summary>
        /// 
        public class Song
        {
            public string ID
            { get; set; }
            public string Title
            { get; set; }
        }

        /// <summary>
        /// Status Class 
        /// </summary>
        /// 
        public class Status
        {
            public enum MPDPlayState
            {
                Play, Pause, Stop
            };

            private MPDPlayState _ps;
            private int _volume;
            private bool _repeat;
            private bool _random;
            private string _songID;

            public MPDPlayState MPDState
            {
                get { return _ps; }
                set { _ps = value; }
            }
            public int MPDVolume
            {
                get { return _volume; }
                set
                {
                    //todo check value. "0-100 or -1 if the volume cannot be determined"
                    _volume = value;
                }
            }
            public bool MPDRepeat
            {
                get { return _repeat; }
                set
                {
                    _repeat = value;
                }
            }
            public bool MPDRandom
            {
                get { return _random; }
                set
                {
                    _random = value;
                }
            }

            public string MPDSongID
            {
                get { return _songID; }
                set
                {
                    _songID = value;
                }
            }

            public Status()
            {
                //constructor
            }
        }

        /// <summary>
        /// MPC (MPD Client) Class 
        /// </summary>

        #region MPC PRIVATE FIELD declaration

        private Status _st;
        private Song _currentSong;
        private ObservableCollection<Song> _songs = new ObservableCollection<Song>();
        private ObservableCollection<String> _playLists = new ObservableCollection<String>();

        #endregion END of MPC PRIVATE FIELD declaration

        #region MPC PUBLIC PROPERTY FIELD

        public Status MPDStatus
        {
            get { return _st; }
        }

        public Song MPDCurrentSong
        {
            get
            {
                return _currentSong;
            }
            set
            {
                _currentSong = value;
            }
        }

        public ObservableCollection<Song> CurrentQueue
        {
            get { return this._songs; }
        }

        public ObservableCollection<String> Playlists
        {
            get { return this._playLists; }
        }

        #endregion END of MPC PUBLIC PROPERTY FIELD


        // MPC Constructor
        public MPC()
        {
            _st = new Status();

            //Enable multithreaded sync
            BindingOperations.EnableCollectionSynchronization(this._songs, new object());

        }
        
        #region MPC METHODS

        private static async Task<List<string>> SendRequest(string server, int port, string mpdCommand)
        {
            IPAddress ipAddress = null;
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(server), port);
            ipAddress = ep.Address;

            try
            {
                TcpClient client = new TcpClient();
                await client.ConnectAsync(ep.Address, port);
                System.Diagnostics.Debug.WriteLine("\n\n" + "Server " + server + " connected.");

                NetworkStream networkStream = client.GetStream();
                StreamWriter writer = new StreamWriter(networkStream);
                StreamReader reader = new StreamReader(networkStream);
                writer.AutoFlush = true;

                //await writer.WriteLineAsync(requestData);
                //System.Diagnostics.Debug.WriteLine("Request: " + requestData);

                //first check MPD's initial response on connect.
                string responseLine = await reader.ReadLineAsync();
                System.Diagnostics.Debug.WriteLine("Connected response: " + responseLine);

                //TODO check if it starts with "OK MPD"

                //if it's ok, then request command.
                string requestData = mpdCommand;
                await writer.WriteLineAsync(requestData);
                System.Diagnostics.Debug.WriteLine("Request: " + requestData);

                //read a single line.
                //responseLine = await reader.ReadLineAsync();
                //System.Diagnostics.Debug.WriteLine("Response: " + responseLine);

                //read multiple lines untill "OK".
                List<string> MPDResponse = new List<string>();
                string responseMultline = "";
                while (!reader.EndOfStream)
                {
                    responseLine = await reader.ReadLineAsync();
                    //System.Diagnostics.Debug.WriteLine("Response loop: " + responseLine);

                    if ((responseLine != "OK") && (responseLine != ""))  
                    {
                        responseMultline = responseMultline + responseLine + "\n";
                        MPDResponse.Add(responseLine);
                    }
                    else
                    {
                        responseMultline = responseMultline + responseLine;
                        break;
                    }
                }

                //debug output
                System.Diagnostics.Debug.WriteLine("Response lines: " + responseMultline);

                client.Close();
                System.Diagnostics.Debug.WriteLine("Connection closed.");

                return MPDResponse;//responseMultline;//responseLine;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                //TODO:
                return null; //ex.Message;
            }
        }

        public async Task<bool> MPDQueryStatus()
        {
            //connect and update Status object instance.
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string mpdCommand = "status";

                Task<List<string>> tsResponse = SendRequest(server, port, mpdCommand);
                await tsResponse;
                return ParseStatusResponse(tsResponse.Result);
            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryStatus(): " + ex.Message);
            }
            return false;
        }

        private bool ParseStatusResponse(List<string> sl)
        {
            Dictionary<string, string> MPDStatusValues = new Dictionary<string, string>();
            foreach (string value in sl)
            {
                string[] StatusValuePair = value.Split(':');
                if (StatusValuePair.Length > 1)
                {
                    if (MPDStatusValues.ContainsKey(StatusValuePair[0].Trim()))
                    {
                        MPDStatusValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                    }
                    else
                    {
                        MPDStatusValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                    }
                }
                else
                {
                    //shuldn't be happening.
                }
            }

            //state
            if (MPDStatusValues.ContainsKey("state"))
            {
                switch (MPDStatusValues["state"])
                {
                    case "play":
                        {
                            _st.MPDState = Status.MPDPlayState.Play;
                            break;
                        }
                    case "pause":
                        {
                            _st.MPDState = Status.MPDPlayState.Pause;
                            break;
                        }
                    case "stop":
                        {
                            _st.MPDState = Status.MPDPlayState.Stop;
                            break;
                        }
                }
            }

            //volume
            if (MPDStatusValues.ContainsKey("volume"))
            {
                try
                {
                    _st.MPDVolume = Int32.Parse(MPDStatusValues["volume"]);
                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            //songID
            _st.MPDSongID = "";
            if (MPDStatusValues.ContainsKey("songid"))
            {
                _st.MPDSongID = MPDStatusValues["songid"];
            }

            //repeat opt bool
            if (MPDStatusValues.ContainsKey("repeat"))
            {
                try
                {
                    //if (Int32.Parse(MPDStatusValues["repeat"]) > 0)
                    if (MPDStatusValues["repeat"] == "1")
                    {
                        _st.MPDRepeat = true;
                    }
                    else
                    {
                        _st.MPDRepeat = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            //random opt bool
            if (MPDStatusValues.ContainsKey("random"))
            {
                try
                {
                    if (Int32.Parse(MPDStatusValues["random"]) > 0)
                    {
                        _st.MPDRandom = true;
                    }
                    else
                    {
                        _st.MPDRandom = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            //more?



            var listItem = _songs.Where(i => i.ID == _st.MPDSongID);
            if (listItem != null)
            {
                foreach (var item in listItem)
                {
                    _currentSong = item as Song;
                    System.Diagnostics.Debug.WriteLine("StatusResponse linq: _songs.Where?="+ _currentSong.Title);
                }
            }
            
            return true;
        }

        public async Task<bool> MPDQueryCurrentPlaylist()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string mpdCommand = "playlistinfo";

                Task<List<string>> tsResponse = SendRequest(server, port, mpdCommand);
                await tsResponse;

                return ParsePlaylistInfoResponse(tsResponse.Result);


            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryCurrentPlaylist(): " + ex.Message);
            }
            return false;
        }

        private bool ParsePlaylistInfoResponse(List<string> sl)
        {
            _songs.Clear();

            Dictionary<string, string> SongValues = new Dictionary<string, string>();
            foreach (string value in sl)
            {
                //System.Diagnostics.Debug.WriteLine("@ParsePlaylistInfoResponse(): " + value);

                string[] StatusValuePair = value.Split(':');
                if (StatusValuePair.Length > 1)
                {
                    if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                    {
                        SongValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                    }
                    else
                    {
                        SongValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                    }

                }
                else
                {
                    //shuldn't be happening.
                }

                if (SongValues.ContainsKey("Id"))
                {

                    Song sng = new Song();
                    sng.ID = SongValues["Id"];

                    if (SongValues.ContainsKey("Title"))
                    {
                        sng.Title = SongValues["Title"];
                    }
                    else
                    {
                        sng.Title = "- no title";
                        if (SongValues.ContainsKey("file"))
                        {
                            sng.Title = Path.GetFileName(SongValues["file"]);
                        }
                    }

                    if (sng.ID == _st.MPDSongID)
                    {
                        _currentSong = sng;
                        //debug
                        System.Diagnostics.Debug.WriteLine(sng.ID + ":" + sng.Title + " - is current.");
                    }

                    _songs.Add(sng);

                    SongValues.Clear();

                }


            }

            return true;
        }

        public async Task<bool> MPDQueryPlaylists()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string mpdCommand = "listplaylists";

                Task<List<string>> tsResponse = SendRequest(server, port, mpdCommand);
                await tsResponse;

                /*
                playlist: Blues
                Last-Modified: 2018-01-26T12:12:10Z
                playlist: Jazz
                Last-Modified: 2018-01-26T12:12:37Z
                OK
                 */

                return ParsePlaylistsResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryPlaylists(): " + ex.Message);
            }
            return false;
        }

        private bool ParsePlaylistsResponse(List<string> sl)
        {
            _playLists.Clear();

            foreach (string value in sl)
            {

                //System.Diagnostics.Debug.WriteLine("@ParsePlaylistsResponse(): " + value + "");

                if (value.StartsWith("playlist:")) {
                    if (value.Split(':').Length > 1) { 
                    _playLists.Add(value.Split(':')[1].Trim());
                    }
                }
                else if (value.StartsWith("Last-Modified: ") || (value.StartsWith("OK"))) 
                {
                    //ignore
                }
                else
                {
                    //ignore
                }
            }
            return true;
        }

        public async Task<bool> MPDPlaybackPlay(string songID = "")
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                if (songID != "")
                {
                    data = data + "playid " + songID + "\n";
                }
                else
                {
                    data = data + "play" + "\n";
                }

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;


                //Alternatively just
                //string sResponse = await SendRequest(server, port, data);
                //"Received response: " + tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPlay: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MPDPlaybackPause()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";
                /*
                switch (st.MPDState)
                {
                    case Status.MPDPlayState.Play:
                        {
                            //PlayButton.Content = "Pause";
                            data = data + "pause 1" + "\n"; //or "stop"
                            break;
                        }
                    case Status.MPDPlayState.Pause:
                        {
                            //PlayButton.Content = "Start";
                            data = data + "pause 0" + "\n";
                            break;
                        }
                    case Status.MPDPlayState.Stop:
                        {
                            //PlayButton.Content = "Play";
                            data = data + "play" + "\n";
                            break;
                        }
                }
                */
                data = data + "pause 1" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;


                //Alternatively just
                //string sResponse = await SendRequest(server, port, data);
                //"Received response: " + tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPause: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MPDPlaybackResume()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";
                /*
                switch (st.MPDState)
                {
                    case Status.MPDPlayState.Play:
                        {
                            //PlayButton.Content = "Pause";
                            data = data + "pause 1" + "\n"; //or "stop"
                            break;
                        }
                    case Status.MPDPlayState.Pause:
                        {
                            //PlayButton.Content = "Start";
                            data = data + "pause 0" + "\n";
                            break;
                        }
                    case Status.MPDPlayState.Stop:
                        {
                            //PlayButton.Content = "Play";
                            data = data + "play" + "\n";
                            break;
                        }
                }
                */
                data = data + "pause 0" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;


                //Alternatively just
                //string sResponse = await SendRequest(server, port, data);
                //"Received response: " + tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackResume: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MPDPlaybackStop()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";
                /*
                switch (st.MPDState)
                {
                    case Status.MPDPlayState.Play:
                        {
                            //PlayButton.Content = "Pause";
                            data = data + "pause 1" + "\n"; //or "stop"
                            break;
                        }
                    case Status.MPDPlayState.Pause:
                        {
                            //PlayButton.Content = "Start";
                            data = data + "pause 0" + "\n";
                            break;
                        }
                    case Status.MPDPlayState.Stop:
                        {
                            //PlayButton.Content = "Play";
                            data = data + "play" + "\n";
                            break;
                        }
                }
                */
                data = data + "stop" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;


                //Alternatively just
                //string sResponse = await SendRequest(server, port, data);
                //"Received response: " + tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackStop: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MPDPlaybackNext()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "next" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackNext: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MPDPlaybackPrev()
        {
            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "previous" + "\n";

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPrev: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MPDSetVolume(int v)
        {
            if (v == _st.MPDVolume)
            {
                return true;
            }

            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "setvol " + v.ToString() + "\n";

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetVol: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MPDSetRepeat(bool on)
        {
            if (_st.MPDRepeat == on)
            {
                return true;
            }

            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                if (on) { 
                    data = data + "repeat 1" + "\n";
                }
                else
                {
                    data = data + "repeat 0" + "\n";
                }
                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRepeat: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MPDSetRandom(bool on)
        {
            if (_st.MPDRandom == on)
            {
                return true;
            }

            try
            {
                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                if (on)
                {
                    data = data + "random 1" + "\n";
                }
                else
                {
                    data = data + "random 0" + "\n";
                }
                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                //error
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRandom: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MPDChangePlaylist(string PlaylistName)
        {
            if (PlaylistName != "")
            {

                //todo settings form.
                string server = "192.168.3.123";
                int port = 6600;
                string data = "command_list_begin" + "\n";

                data = data + "clear" +  "\n";

                data = data + "load " + PlaylistName + "\n";

                if (_st.MPDState == Status.MPDPlayState.Play) { 
                    data = data + "play" + "\n";
                }

                data = data + "status" + "\n" + "command_list_end";

                //send task
                Task<List<string>> tsResponse = SendRequest(server, port, data);

                //send task excution and wait.
                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);
            }
            else
            {
                return false;
            }
        }

        #endregion END of MPD METHODS


        /// END OF MPC Client Class 
    }
}
