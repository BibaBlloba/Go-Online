using System;
using WebSocketSharp;

namespace L9_new
{
    public class ClientConnection
    {
        private WebSocket ws;
        private bool isConnected;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnErrorOccurred;

        public ClientConnection(string url)
        {
            ws = new WebSocket(url);
            isConnected = false;
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            ws.OnOpen += (sender, e) =>
            {
                isConnected = true;
                Console.WriteLine("Connected to server");
                OnConnected?.Invoke();
            };

            ws.OnClose += (sender, e) =>
            {
                isConnected = false;
                Console.WriteLine("Disconnected from server");
                OnDisconnected?.Invoke();
            };

            ws.OnMessage += (sender, e) =>
            {
                Console.WriteLine($"Message from server: {e.Data}");
                OnMessageReceived?.Invoke(e.Data);
            };

            ws.OnError += (sender, e) =>
            {
                Console.WriteLine($"Error: {e.Message}");
                OnErrorOccurred?.Invoke(e.Message);
            };
        }

        public void Connect()
        {
            if (isConnected) return;

            try
            {
                ws.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                OnErrorOccurred?.Invoke(ex.Message);
            }
        }

        public void Disconnect()
        {
            if (!isConnected) return;

            ws?.Close();
        }

        public void SendMessage(string message)
        {
            if (!isConnected)
            {
                Console.WriteLine("Not connected to server");
                return;
            }

            ws?.Send(message);
        }

        public bool IsConnected => isConnected;
    }
}
