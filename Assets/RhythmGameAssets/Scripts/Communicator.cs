using MainMenu;
using NativeWebSocket;
using Newtonsoft.Json;
using System.IO;
using System;
using UnityEngine;
using TMPro;

namespace RhythmGameAssets.Scripts
{
    public enum Instrument
    {
        BASS,
        DRUMS,
        GUITAR,
        KEYS1,
        KEYS2
    }

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
        PING,
        PLACEMENT
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

    public class PingPacket : WebPacket
    {
        public long TimeReceived;
        public Instrument Instrument;

        public PingPacket(long timeReceived, Instrument instrument) : base(Sender.CLIENT, PacketType.PING)
        {
            TimeReceived = timeReceived;
            Instrument = instrument;
        }

        public new string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public new static PingPacket FromJSON(String jsonString)
        {
            return JsonConvert.DeserializeObject<PingPacket>(jsonString);
        }
    }

    public class PlacementPacket : WebPacket
    {
        public int Placement;
        public Instrument Instrument;

        public PlacementPacket(int placement, Instrument instrument) : base(Sender.CLIENT, PacketType.PLACEMENT)
        {
            Placement = placement;
            Instrument = instrument;
        }

        public new string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public new static PlacementPacket FromJSON(String jsonString)
        {
            return JsonConvert.DeserializeObject<PlacementPacket>(jsonString);
        }
    }

    public class Communicator : MonoBehaviour
    {
        [SerializeField] Metronome _metronome;

        [SerializeField] Instrument SelectedInstrument = Instrument.BASS;

        [Header("UI")]
        [SerializeField] TextMeshProUGUI PlacementText;

        private string WebsocketIP;
        private WebSocket _websocket;
        private long _lastPingSent = -1;

        private float _avgLatency = 0f;
        private float _totalLatency = 0f;
        private int _numPings = 0;
        private readonly int PINGS_TO_AVERAGE = 10;

        async void Start()
        {
            SetSelectedInstrument();

            LoadIPFromFile();

            _websocket = new WebSocket("ws://" + WebsocketIP);

            _websocket.OnOpen += () => { SendConnectionPacket(); };

            _websocket.OnClose += (e) => { Debug.Log("Web socket connection closed."); };

            _websocket.OnMessage += (bytes) =>
            {
                string packetJSON = System.Text.Encoding.Default.GetString(bytes);
                WebPacket basePacket = WebPacket.FromJSON(packetJSON);

                switch (basePacket.Type)
                {
                    case PacketType.SYNC:
                        SyncPacket sPacket = SyncPacket.FromJSON(packetJSON);
                        HandleClientSync(sPacket);
                        break;
                    case PacketType.PING:
                        PingPacket pPacket = PingPacket.FromJSON(packetJSON);
                        CalculateServerLatency(pPacket);
                        break;
                    case PacketType.PLACEMENT:
                        PlacementPacket plPacket = PlacementPacket.FromJSON(packetJSON);
                        UpdatePlacementUI(plPacket.Placement);
                        break;
                }
            };

            InvokeRepeating("SendPingPacket", 1, 0.5f);

            await _websocket.Connect();
        }

        void LoadIPFromFile()
        {
            string assetsPath = Path.GetFullPath(Application.streamingAssetsPath);
            string ipPath = Path.Join(assetsPath, "ip.txt");

            try
            {
                using (StreamReader reader = new StreamReader(ipPath))
                {
                    string line = reader.ReadLine();
                    this.WebsocketIP = line;
                }
            } catch (Exception e)
            {
                Debug.Log("Error reading IP from file: " + e);
            }
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

        async void SendPingPacket()
        {
            if (_websocket.State == WebSocketState.Open)
            {
                PingPacket pingPacket = new(-1, SelectedInstrument);
                _lastPingSent = pingPacket.TimeSent;
                await _websocket.SendText(pingPacket.ToJSON());
            }
        }

        void SetSelectedInstrument()
        {
            string instrument = SavedSettings.Instance.Instrument;

            this.SelectedInstrument = instrument switch
            {
                "SINGLE" => Instrument.GUITAR,
                "DRUMS" => Instrument.DRUMS,
                "DOUBLEBASS" => Instrument.BASS,
                "KEYBOARD" => Instrument.KEYS1,
                "DOUBLERHYTHM" => Instrument.KEYS2,
                _ => Instrument.BASS
            };
        }

        void UpdatePlacementUI(int placement)
        {
            switch (placement)
            {
                case 1:
                    this.PlacementText.text = "1st";
                    break;
                case 2:
                    this.PlacementText.text = "2nd";
                    break;
                case 3:
                    this.PlacementText.text = "3rd";
                    break;
                case 4:
                    this.PlacementText.text = "4th";
                    break;
                case 5:
                    this.PlacementText.text = "5th";
                    break;
                default:
                    break;
            }
        }

        void CalculateServerLatency(PingPacket packet)
        {
            _numPings += 1;

            long received = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _totalLatency += (received - _lastPingSent) / 2;

            if (_numPings % PINGS_TO_AVERAGE == 0)
            {
                _avgLatency = _totalLatency / PINGS_TO_AVERAGE;

                //Debug.Log("AVERAGE: " + _avgLatency);

                _totalLatency = 0f;
                _numPings = 0;
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
            _metronome.AdjustPlaybackTime(packet.SongIsPlaying, packet.SongTime + _avgLatency / 1000f);
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