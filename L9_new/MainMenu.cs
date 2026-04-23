using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fleck;
using WebSocketSharp;

namespace L9_new
{
    public partial class MainMenu : Form
    {
        private ServerConnection serverConnection;
        private ClientConnection clientConnection;

        public MainMenu()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Подключение как клиент к удаленному серверу
            string serverAddress = textBox1.Text;
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                MessageBox.Show("Пожалуйста, введите адрес сервера");
                return;
            }

            clientConnection = new ClientConnection($"ws://{serverAddress}");
            clientConnection.OnConnected += () =>
            {
                MessageBox.Show("Успешно подключено к серверу");
            };
            clientConnection.OnErrorOccurred += (error) =>
            {
                MessageBox.Show($"Ошибка подключения: {error}");
            };

            clientConnection.Connect();
            OpenGameForm();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Запуск сервера и подключение как клиент локально
            serverConnection = new ServerConnection("ws://0.0.0.0:8181");
            serverConnection.Start();

            // Небольшая задержка чтобы сервер успел запуститься
            System.Threading.Thread.Sleep(500);

            clientConnection = new ClientConnection("ws://localhost:8181");
            clientConnection.OnConnected += () =>
            {
                MessageBox.Show("Успешно подключено к локальному серверу");
            };
            clientConnection.OnErrorOccurred += (error) =>
            {
                MessageBox.Show($"Ошибка подключения: {error}");
            };

            clientConnection.Connect();
            OpenGameForm();
        }

        private void OpenGameForm()
        {
            GameForm gameForm = new GameForm(clientConnection);
            gameForm.FormClosed += (s, e) =>
            {
                // Когда GameForm закрывается, очищаем ресурсы
                clientConnection?.Disconnect();
                serverConnection?.Stop();
            };

            // Скрываем MainMenu вместо закрытия
            this.Hide();
            gameForm.Show();
        }
    }
}