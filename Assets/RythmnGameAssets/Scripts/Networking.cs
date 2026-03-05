using UnityEngine;

using NativeWebSocket;

public class Networking : MonoBehaviour
{
    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:8080");

        websocket.OnOpen += () => {
            Debug.Log("Web socket connection established.");
        };

        websocket.OnClose += (e) => {
            Debug.Log("Web socket connection closed.");
        };

        websocket.OnMessage += (bytes) => {
            Debug.Log("Message received:");
            Debug.Log(bytes);
        };

        await websocket.Connect();
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
        #endif
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}
