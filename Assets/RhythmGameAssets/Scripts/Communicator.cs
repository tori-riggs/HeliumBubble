using NativeWebSocket;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RhythmGameAssets.Scripts
{
    public enum Sender
    {
        CLIENT,
        SCOREBOARD,
        SERVER
    }

    public enum PacketType
    {
        CONNECTION,
        SCORE,
        SOUND,
        SYNC,
    }

    public class WebPacket
    {
        public Sender Sender;
        public PacketType Type;
        public long TimeSent;

        public WebPacket(Sender sender, PacketType type)
        {
            Sender = sender;
            Type = type;
            TimeSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static WebPacket FromJSON(String jsonString)
        {
            return JsonConvert.DeserializeObject<WebPacket>(jsonString);
        }
    }

    public enum Instrument
    {
        BASS,
        DRUMS,
        GUITAR,
        KEYS1,
        KEYS2
    }

    public class ConnectionPacket : WebPacket
    {
        public bool IsConnecting;
        public Instrument Instrument;

        public ConnectionPacket(bool isConnecting, Instrument instrument) : base(Sender.CLIENT, PacketType.CONNECTION)
        {
            IsConnecting = isConnecting;
            Instrument = instrument;
        }

        public new string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public new static ConnectionPacket FromJSON(String jsonString)
        {
            return JsonConvert.DeserializeObject<ConnectionPacket>(jsonString);
        }
    }

    public class ScorePacket : WebPacket
    {
        public Instrument Instrument;
        public int Score;

        public ScorePacket(Instrument instrument, int score) : base(Sender.CLIENT, PacketType.SCORE)
        {
            Instrument = instrument;
            Score = score;
        }

        public new string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public new static ScorePacket FromJSON(String jsonString)
        {
            return JsonConvert.DeserializeObject<ScorePacket>(jsonString);
        }
    }

    public class SoundPacket : WebPacket
    {
        public Instrument Instrument;
        public float RecentNotePercentage; // how well did the player hit the last 10 notes

        public SoundPacket(Instrument instrument, float recentNotePercentage) : base(Sender.CLIENT, PacketType.SOUND)
        {
            Instrument = instrument;
            RecentNotePercentage = recentNotePercentage;
        }

        public new string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public new static SoundPacket FromJSON(String jsonString)
        {
            return JsonConvert.DeserializeObject<SoundPacket>(jsonString);
        }
    }

    public class SyncPacket : WebPacket
    {
        public bool SongIsPlaying;
        public float SongTime;

        public SyncPacket(bool songIsPlaying, float songTime) : base(Sender.CLIENT, PacketType.SYNC)
        {
            SongIsPlaying = songIsPlaying;
            SongTime = songTime;
        }

        public new string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public new static SyncPacket FromJSON(String jsonString)
        {
            return JsonConvert.DeserializeObject<SyncPacket>(jsonString);
        }
    }

    public class Communicator : MonoBehaviour
    {
        [SerializeField] Metronome _metronome;

        [SerializeField] string websocketIP = "localhost:8080";
        [SerializeField] Instrument SelectedInstrument = Instrument.BASS;

        private WebSocket _websocket;

        async void Start()
        {
            _websocket = new WebSocket("ws://" + websocketIP);

            _websocket.OnOpen += () => { SendConnectionPacket(); };

            _websocket.OnClose += (e) => { Debug.Log("Web socket connection closed."); };

            _websocket.OnMessage += (bytes) =>
            {
                string packetJSON = System.Text.Encoding.Default.GetString(bytes);
                WebPacket basePacket = WebPacket.FromJSON(packetJSON);

                if (basePacket.Type == PacketType.SYNC)
                {
                    SyncPacket sPacket = SyncPacket.FromJSON(packetJSON);
                    HandleClientSync(sPacket);
                }
            };

            await _websocket.Connect();
        }

        async void SendConnectionPacket()
        {
            if (_websocket.State == WebSocketState.Open)
            {
                ConnectionPacket cPacket = new(true, SelectedInstrument);
                await _websocket.SendText(cPacket.ToJSON());
            }
        }

        async void SendDisconnectPacket()
        {
            if (_websocket.State == WebSocketState.Open)
            {
                ConnectionPacket cPacket = new(false, SelectedInstrument);
                await _websocket.SendText(cPacket.ToJSON());
            }
        }

        public async void SendScorePacket(int score)
        {
            if (_websocket.State == WebSocketState.Open)
            {
                ScorePacket sPacket = new(SelectedInstrument, score);
                await _websocket.SendText(sPacket.ToJSON());
            }
        }

        public async void SendSoundPacket(float notePercentage)
        {
            if (_websocket.State == WebSocketState.Open)
            {
                SoundPacket sPacket = new(SelectedInstrument, notePercentage);
                await _websocket.SendText(sPacket.ToJSON());
            }
        }

        void HandleClientSync(SyncPacket packet)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            float delayInSeconds = (now - packet.TimeSent) / 1000.0f;

            float finalSongTime = packet.SongTime + delayInSeconds;
            float clientTime = (float) _metronome.GetPlaybackTime();
            _metronome.AdjustPlaybackTime(packet.SongIsPlaying, clientTime, finalSongTime);
        }

        void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
             _websocket.DispatchMessageQueue();
            #endif
        }

        private async void OnApplicationQuit()
        {
            SendDisconnectPacket();
            await _websocket.Close();
        }
    }
}