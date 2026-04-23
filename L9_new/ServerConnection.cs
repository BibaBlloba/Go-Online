using System;
using System.Threading.Tasks;
using Fleck;

namespace L9_new
{
    public class ServerConnection
    {
        private WebSocketServer server;
        private bool isRunning;

        public event Action OnClientConnected;
        public event Action OnClientDisconnected;
        public event Action<string> OnMessageReceived;

        public ServerConnection(string url)
        {
            server = new WebSocketServer(url);
            isRunning = false;
        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            Task.Run(() =>
            {
                server.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        Console.WriteLine("Client connected");
                        OnClientConnected?.Invoke();
                    };

                    socket.OnClose = () =>
                    {
                        Console.WriteLine("Client disconnected");
                        OnClientDisconnected?.Invoke();
                    };

                    socket.OnMessage = message =>
                    {
                        Console.WriteLine($"Received: {message}");
                        OnMessageReceived?.Invoke(message);
                        socket.Send("Echo: " + message);
                    };
                });

                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            });
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            server?.Dispose();
        }
    }
}
