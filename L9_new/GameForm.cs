using System;
using System.Drawing;
using System.Windows.Forms;

namespace L9_new
{
    public partial class GameForm : Form
    {
        private Panel boardPanel;
        private Label statusLabel;
        private Label roleLabel;

        private ClientConnection clientConnection;
        private ServerConnection serverConnection;
        private bool isHost;

        private int boardSize;
        private int[,] board;
        private GoLogic gameLogic;
        private PlayerColor myColor = PlayerColor.None;
        private PlayerColor currentTurn = PlayerColor.None;
        private bool roundActive;
        private Button endGameButton;

        public GameForm(ClientConnection connection, ServerConnection server = null)
        {
            InitializeComponent();
            InitializeGameUI();

            clientConnection = connection;
            serverConnection = server;
            isHost = serverConnection != null;

            if (isHost)
            {
                statusLabel.Text = "Хост: ожидание второго игрока...";
                roleLabel.Text = "Роль: Хост";
            }
            else
            {
                statusLabel.Text = "Гость: ожидание настройки раунда...";
                roleLabel.Text = "Роль: Гость";
            }

            if (clientConnection != null)
            {
                clientConnection.OnMessageReceived += HandleMessageReceived;
                clientConnection.OnDisconnected += HandleDisconnected;
            }
        }

        private void InitializeGameUI()
        {
            Text = "Игра в Го";
            ClientSize = new Size(800, 850);
            StartPosition = FormStartPosition.CenterScreen;

            statusLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(760, 25),
                Text = "Подключение...",
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };
            Controls.Add(statusLabel);

            roleLabel = new Label
            {
                Location = new Point(10, 40),
                Size = new Size(360, 25),
                Text = "Роль: ?",
                Font = new Font(Font.FontFamily, 9, FontStyle.Regular)
            };
            Controls.Add(roleLabel);

            boardPanel = new Panel
            {
                Location = new Point(10, 80),
                Size = new Size(680, 680),
                BackColor = Color.BurlyWood
            };
            boardPanel.Paint += BoardPanel_Paint;
            boardPanel.MouseClick += BoardPanel_MouseClick;
            Controls.Add(boardPanel);

            endGameButton = new Button
            {
                Location = new Point(10, 770),
                Size = new Size(200, 35),
                Text = "Пропустить ход"
            };
            endGameButton.Click += (sender, args) => RequestPass();
            Controls.Add(endGameButton);
        }

        private void RequestPass()
        {
            if (!roundActive) return;
            clientConnection.SendMessage(GameProtocol.Build("RequestPass"));
            SetStatus("Пас отправлен...", Color.Black);
        }
        private void HandleMessageReceived(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => HandleMessageReceived(message)));
                return;
            }

            Console.WriteLine($"GameForm received: {message}");
            var parts = message.Split('|');
            if (parts.Length == 0) return;

            switch (parts[0])
            {
                case "AssignRole":
                    HandleAssignRole(parts);
                    break;
                case "PeerConnected":
                    HandlePeerConnected();
                    break;
                case "WaitingForSettings":
                    statusLabel.Text = "Ожидание настроек раунда от хоста...";
                    break;
                case "RoundSettings":
                    ApplyRoundSettings(parts);
                    break;
                case "MoveAccepted":
                    ProcessMoveAccepted(parts);
                    break;
                case "PlayerPassed":
                    HandlePlayerPassed(parts);
                    break;
                case "MoveRejected":
                    SetStatus(parts.Length > 1 ? parts[1] : "Ход отклонен", Color.DarkRed);
                    break;
                case "Error":
                    SetStatus(parts.Length > 1 ? parts[1] : "Произошла ошибка", Color.DarkRed);
                    break;
                case "GameEnded":
                    HandleGameEnded(parts);
                    break;
                case "GameMessage":
                    if (parts.Length > 1)
                    {
                        statusLabel.Text = parts[1];
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown protocol message: {message}");
                    break;
            }
        }

        private void HandleAssignRole(string[] parts)
        {
            if (parts.Length < 2) return;
            if (parts[1].Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                isHost = true;
            }
            else if (parts[1].Equals("Guest", StringComparison.OrdinalIgnoreCase))
            {
                isHost = false;
            }
        }

        private void HandlePeerConnected()
        {
            statusLabel.Text = "Второй игрок подключился. Настройте раунд.";
            if (isHost)
            {
                using (var settingsForm = new RoundSettingsForm())
                {
                    if (settingsForm.ShowDialog(this) == DialogResult.OK)
                    {
                        clientConnection.SendMessage(GameProtocol.Build("RequestSettings", settingsForm.BoardSize.ToString(), settingsForm.SelectedColor));
                        SetStatus("Отправлены настройки раунда. Ожидание ответа сервера...", Color.Black);
                    }
                }
            }
        }

        private void ApplyRoundSettings(string[] parts)
        {
            if (parts.Length < 4)
            {
                SetStatus("Некорректные настройки раунда", Color.DarkRed);
                return;
            }

            if (!int.TryParse(parts[1], out var size))
            {
                SetStatus("Некорректный размер карты", Color.DarkRed);
                return;
            }

            var hostColor = GameProtocol.ParseColor(parts[2]);
            var guestColor = GameProtocol.ParseColor(parts[3]);
            boardSize = size;
            InitializeBoard(size);

            myColor = isHost ? hostColor : guestColor;
            currentTurn = PlayerColor.Black;
            roundActive = true;

            roleLabel.Text = isHost ? $"Роль: Хост ({GameProtocol.ColorToString(myColor)})" : $"Роль: Гость ({GameProtocol.ColorToString(myColor)})";
            statusLabel.Text = IsMyTurn() ? "Ваш ход" : "Ожидайте ход соперника";
            boardPanel.Invalidate();
        }

        private void ProcessMoveAccepted(string[] parts)
        {
            if (parts.Length < 5)
            {
                SetStatus("Некорректный ответ сервера на ход", Color.DarkRed);
                return;
            }

            if (!int.TryParse(parts[1], out var x) || !int.TryParse(parts[2], out var y))
            {
                SetStatus("Некорректные координаты хода", Color.DarkRed);
                return;
            }

            var placedColor = GameProtocol.ParseColor(parts[3]);
            currentTurn = GameProtocol.ParseColor(parts[4]);
            if (placedColor == PlayerColor.None)
            {
                SetStatus("Неизвестный цвет хода", Color.DarkRed);
                return;
            }

            if (gameLogic != null)
            {
                gameLogic.TryPlaceStone(x, y, (int)placedColor);
                board = gameLogic.Board;
            }

            SetStatus(IsMyTurn() ? "Ваш ход" : "Ожидайте ход соперника", Color.Black);
            endGameButton.Enabled = IsMyTurn();
            boardPanel.Invalidate();
        }

        private void SetStatus(string message, Color color)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = color;
        }

        private void HandleGameEnded(string[] parts)
        {
            if (parts.Length < 3 || !int.TryParse(parts[1], out var blackScore) || !int.TryParse(parts[2], out var whiteScore))
            {
                SetStatus("Ошибка при получении результатов игры", Color.DarkRed);
                return;
            }

            roundActive = false;
            var winner = blackScore > whiteScore ? "Чёрные" : whiteScore > blackScore ? "Белые" : "Ничья";
            var message = $"Игра закончена! Чёрные: {blackScore}, Белые: {whiteScore}. Победитель: {winner}";
            SetStatus(message, Color.Green);
        }

        private void HandlePlayerPassed(string[] parts)
        {
            if (parts.Length < 4)
            {
                return;
            }

            var passedColor = GameProtocol.ParseColor(parts[1]);
            currentTurn = GameProtocol.ParseColor(parts[2]);

            if (!int.TryParse(parts[3], out var passCount))
            {
                passCount = 0;
            }

            var passMessage = passCount == 1 
                ? $"{GameProtocol.ColorToString(passedColor)} спасовал. Ваш ход."
                : $"{GameProtocol.ColorToString(passedColor)} спасовал (2 подряд). Игра завершается...";
            
            SetStatus(passMessage, Color.DarkGoldenrod);
            endGameButton.Enabled = IsMyTurn();
        }

        private void BoardPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (!roundActive)
            {
                SetStatus("Ожидание второго игрока...", Color.DarkRed);
                return;
            }

            if (!IsMyTurn())
            {
                SetStatus("Сейчас не ваш ход.", Color.DarkRed);
                return;
            }

            if (!GetBoardCoordinates(e.Location, out var x, out var y))
            {
                return;
            }

            if (board[x, y] != 0)
            {
                SetStatus("Клетка уже занята.", Color.DarkRed);
                return;
            }

            clientConnection.SendMessage(GameProtocol.Build("RequestMove", x.ToString(), y.ToString()));
            endGameButton.Enabled = IsMyTurn();
        }

        private bool GetBoardCoordinates(Point click, out int x, out int y)
        {
            x = -1;
            y = -1;
            if (boardSize < 2)
            {
                return false;
            }

            const int margin = 20;
            var spacing = (boardPanel.Width - margin * 2) / (float)(boardSize - 1);
            var fx = (click.X - margin) / spacing;
            var fy = (click.Y - margin) / spacing;
            var ix = (int)Math.Round(fx);
            var iy = (int)Math.Round(fy);

            if (ix < 0 || ix >= boardSize || iy < 0 || iy >= boardSize)
            {
                return false;
            }

            var dx = Math.Abs(fx - ix) * spacing;
            var dy = Math.Abs(fy - iy) * spacing;

            if (dx > spacing * 0.4f || dy > spacing * 0.4f)
            {
                return false;
            }

            x = ix;
            y = iy;
            return true;
        }

        private bool IsMyTurn()
        {
            return roundActive && myColor != PlayerColor.None && myColor == currentTurn;
        }

        private void InitializeBoard(int size)
        {
            boardSize = size;
            board = new int[size, size];
            gameLogic = new GoLogic(size);
            roundActive = true;
            boardPanel.Invalidate();
        }

        private void BoardPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.BurlyWood);

            if (board == null || boardSize < 2)
            {
                return;
            }

            const int margin = 20;
            var spacing = (boardPanel.Width - margin * 2) / (float)(boardSize - 1);
            using (var pen = new Pen(Color.Black, 1))
            {
                for (int i = 0; i < boardSize; i++)
                {
                    var offset = margin + i * spacing;
                    e.Graphics.DrawLine(pen, margin, offset, boardPanel.Width - margin, offset);
                    e.Graphics.DrawLine(pen, offset, margin, offset, boardPanel.Width - margin);
                }
            }

            for (int x = 0; x < boardSize; x++)
            {
                for (int y = 0; y < boardSize; y++)
                {
                    if (board[x, y] == 0)
                    {
                        continue;
                    }

                    var centerX = margin + x * spacing;
                    var centerY = margin + y * spacing;
                    var radius = spacing * 0.4f;
                    var stoneRect = new RectangleF(centerX - radius, centerY - radius, radius * 2, radius * 2);
                    using (var brush = new SolidBrush(board[x, y] == (int)PlayerColor.Black ? Color.Black : Color.White))
                    {
                        e.Graphics.FillEllipse(brush, stoneRect);
                    }

                    e.Graphics.DrawEllipse(Pens.Black, stoneRect);
                }
            }
        }

        private void HandleDisconnected()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(HandleDisconnected));
                return;
            }

            SetStatus("Соединение с сервером разорвано", Color.DarkRed);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (clientConnection != null)
            {
                clientConnection.OnMessageReceived -= HandleMessageReceived;
                clientConnection.OnDisconnected -= HandleDisconnected;
                clientConnection.Disconnect();
            }

            if (serverConnection != null)
            {
                serverConnection.Stop();
            }

            System.Threading.Thread.Sleep(500);

            base.OnFormClosing(e);
            Application.Exit();
        }
    }
}
