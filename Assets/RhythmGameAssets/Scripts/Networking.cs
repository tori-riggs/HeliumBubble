using Newtonsoft.Json;

using UnityEngine;

using NativeWebSocket;
using System;

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
}

public enum Instrument
{
    BASS,
    GUITAR,
    KEYS,
    VOCALS
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

    public string ToJSON()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static ConnectionPacket FromJSON(String jsonString)
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

    public string ToJSON()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static ScorePacket FromJSON(String jsonString)
    {
        return JsonConvert.DeserializeObject<ScorePacket>(jsonString);
    }
}

public class Networking : MonoBehaviour
{
    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:8080");

        websocket.OnOpen += () => {
            Debug.Log("Web socket connection established.");
            SendConnectionPacket();
        };

        websocket.OnClose += (e) => {
            Debug.Log("Web socket connection closed.");
        };

        websocket.OnMessage += (bytes) => {
            Debug.Log("Message received.");
            //string message = System.Text.Encoding.Default.GetString(bytes);
            //Debug.Log(message);
        };

        await websocket.Connect();
    }

    async void SendConnectionPacket()
    {
        if (websocket.State == WebSocketState.Open)
        {
            // TODO: Change this later to be selected instrument instead
            // of bass default
            ConnectionPacket cPacket = new(true, Instrument.BASS);
            await websocket.SendText(cPacket.ToJSON());
        }
    }

    async void SendDisconnectPacket()
    {
        if (websocket.State == WebSocketState.Open)
        {
            // TODO: Change this later to be selected instrument instead
            // of bass default
            ConnectionPacket cPacket = new(false, Instrument.BASS);
            await websocket.SendText(cPacket.ToJSON());
        }
    }

    async void SendScorePacket(int score)
    {
        if (websocket.State == WebSocketState.Open)
        {
            // TODO: Change this later to be selected instrument instead
            // of bass default
            ScorePacket sPacket = new(Instrument.BASS, score);
            await websocket.SendText(sPacket.ToJSON());
        }
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
        #endif
    }

    private async void OnApplicationQuit()
    {
        SendDisconnectPacket();
        await websocket.Close();
    }
}
