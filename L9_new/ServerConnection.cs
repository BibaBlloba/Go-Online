using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fleck;

namespace L9_new
{
    public enum PlayerColor
    {
        None = 0,
        Black = 1,
        White = 2
    }

    public class RoundState
    {
        public int BoardSize { get; set; }
        public PlayerColor HostColor { get; set; }
        public PlayerColor GuestColor { get; set; }
        public PlayerColor CurrentTurn { get; set; }
        public int[,] Board { get; set; }
        public bool Active { get; set; }

        public RoundState(int boardSize, PlayerColor hostColor, PlayerColor guestColor)
        {
            BoardSize = boardSize;
            HostColor = hostColor;
            GuestColor = guestColor;
            CurrentTurn = PlayerColor.Black;
            Board = new int[boardSize, boardSize];
            Active = true;
        }
    }

    public class ServerConnection
    {
        private readonly WebSocketServer server;
        private readonly object clientLock = new object();
        private IWebSocketConnection hostSocket;
        private IWebSocketConnection guestSocket;
        private RoundState currentRound;
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
                        lock (clientLock)
                        {
                            if (hostSocket == null)
                            {
                                hostSocket = socket;
                                SendToSocket(socket, GameProtocol.Build("AssignRole", "Host"));
                                Console.WriteLine("Host connected");
                            }
                            else if (guestSocket == null)
                            {
                                guestSocket = socket;
                                SendToSocket(socket, GameProtocol.Build("AssignRole", "Guest"));
                                Console.WriteLine("Guest connected");
                            }
                            else
                            {
                                SendToSocket(socket, GameProtocol.Build("Error", "Сервер заполнен"));
                                socket.Close();
                                return;
                            }
                        }

                        OnClientConnected?.Invoke();
                        SendConnectionStatus();
                    };

                    socket.OnClose = () =>
                    {
                        lock (clientLock)
                        {
                            if (socket == hostSocket)
                            {
                                hostSocket = null;
                            }
                            else if (socket == guestSocket)
                            {
                                guestSocket = null;
                            }

                            if (currentRound != null)
                            {
                                currentRound.Active = false;
                            }

                            if (hostSocket != null)
                            {
                                SendToSocket(hostSocket, GameProtocol.Build("Error", "Противник отключился"));
                            }

                            if (guestSocket != null)
                            {
                                SendToSocket(guestSocket, GameProtocol.Build("Error", "Противник отключился"));
                            }
                        }

                        OnClientDisconnected?.Invoke();
                    };

                    socket.OnMessage = message =>
                    {
                        Console.WriteLine($"Received: {message}");
                        OnMessageReceived?.Invoke(message);
                        HandleClientMessage(socket, message);
                    };
                });

                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            });
        }

        private void SendConnectionStatus()
        {
            lock (clientLock)
            {
                if (hostSocket != null && guestSocket != null)
                {
                    SendToSocket(hostSocket, GameProtocol.Build("PeerConnected"));
                    SendToSocket(guestSocket, GameProtocol.Build("WaitingForSettings"));
                }
                else if (hostSocket != null)
                {
                    SendToSocket(hostSocket, GameProtocol.Build("GameMessage", "Ожидание второго игрока..."));
                }
            }
        }

        private void HandleClientMessage(IWebSocketConnection socket, string message)
        {
            var parts = GameProtocol.Parse(message);
            if (parts.Length == 0) return;

            switch (parts[0])
            {
                case "RequestSettings":
                    HandleSettingsRequest(socket, parts);
                    break;
                case "RequestMove":
                    HandleMoveRequest(socket, parts);
                    break;
                default:
                    SendToSocket(socket, GameProtocol.Build("Error", "Неизвестная команда"));
                    break;
            }
        }

        private void HandleSettingsRequest(IWebSocketConnection socket, string[] parts)
        {
            if (socket != hostSocket)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Только хост может настроить раунд"));
                return;
            }

            if (parts.Length < 3)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Некорректные настройки раунда"));
                return;
            }

            if (!int.TryParse(parts[1], out var size))
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Некорректный размер карты"));
                return;
            }

            var hostColor = GameProtocol.ParseColor(parts[2]);
            if (hostColor == PlayerColor.None)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Выберите цвет: Black или White"));
                return;
            }

            var guestColor = GameProtocol.Opponent(hostColor);
            currentRound = new RoundState(size, hostColor, guestColor);
            lock (clientLock)
            {
                if (hostSocket != null)
                {
                    SendToSocket(hostSocket, GameProtocol.Build("RoundSettings", size.ToString(), GameProtocol.ColorToString(hostColor), GameProtocol.ColorToString(guestColor)));
                }

                if (guestSocket != null)
                {
                    SendToSocket(guestSocket, GameProtocol.Build("RoundSettings", size.ToString(), GameProtocol.ColorToString(hostColor), GameProtocol.ColorToString(guestColor)));
                }
            }
        }

        private void HandleMoveRequest(IWebSocketConnection socket, string[] parts)
        {
            if (currentRound == null || !currentRound.Active)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Раунд не начат"));
                return;
            }

            if (parts.Length < 3 || !int.TryParse(parts[1], out var x) || !int.TryParse(parts[2], out var y))
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Некорректный ход"));
                return;
            }

            var playerColor = GetPlayerColor(socket);
            if (playerColor == PlayerColor.None)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Игрок не находится в раунде"));
                return;
            }

            if (playerColor != currentRound.CurrentTurn)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Сейчас не ваш ход"));
                return;
            }

            if (x < 0 || x >= currentRound.BoardSize || y < 0 || y >= currentRound.BoardSize)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Ход за границами доски"));
                return;
            }

            if (currentRound.Board[x, y] != 0)
            {
                SendToSocket(socket, GameProtocol.Build("MoveRejected", "Клетка уже занята"));
                return;
            }

            currentRound.Board[x, y] = (int)playerColor;
            currentRound.CurrentTurn = GameProtocol.Opponent(playerColor);

            Broadcast(GameProtocol.Build(
                "MoveAccepted",
                x.ToString(),
                y.ToString(),
                GameProtocol.ColorToString(playerColor),
                GameProtocol.ColorToString(currentRound.CurrentTurn)));
        }

        private PlayerColor GetPlayerColor(IWebSocketConnection socket)
        {
            if (currentRound == null)
            {
                return PlayerColor.None;
            }

            if (socket == hostSocket)
            {
                return currentRound.HostColor;
            }

            if (socket == guestSocket)
            {
                return currentRound.GuestColor;
            }

            return PlayerColor.None;
        }

        private void Broadcast(string message)
        {
            lock (clientLock)
            {
                if (hostSocket != null)
                {
                    SendToSocket(hostSocket, message);
                }

                if (guestSocket != null)
                {
                    SendToSocket(guestSocket, message);
                }
            }
        }

        private void SendToSocket(IWebSocketConnection socket, string message)
        {
            try
            {
                socket?.Send(message);
            }
            catch
            {
                // Игнорируем ошибки отправки.
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            server?.Dispose();
        }
    }

    internal static class GameProtocol
    {
        public static string Build(params string[] parts)
        {
            return string.Join("|", parts.Select(p => p.Replace("|", "")));
        }

        public static string[] Parse(string message)
        {
            return string.IsNullOrEmpty(message) ? Array.Empty<string>() : message.Split('|');
        }

        public static PlayerColor ParseColor(string value)
        {
            if (string.Equals(value, "Black", StringComparison.OrdinalIgnoreCase))
            {
                return PlayerColor.Black;
            }

            if (string.Equals(value, "White", StringComparison.OrdinalIgnoreCase))
            {
                return PlayerColor.White;
            }

            return PlayerColor.None;
        }

        public static string ColorToString(PlayerColor color)
        {
            return color == PlayerColor.Black ? "Black" : color == PlayerColor.White ? "White" : "None";
        }

        public static PlayerColor Opponent(PlayerColor color)
        {
            return color == PlayerColor.Black ? PlayerColor.White : color == PlayerColor.White ? PlayerColor.Black : PlayerColor.None;
        }
    }
}
