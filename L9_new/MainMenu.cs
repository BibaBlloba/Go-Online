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
        
        // Подключение как клиент к удаленному серверу
        private void button1_Click(object sender, EventArgs e)
        {
            string serverAddress = textBox1.Text;
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                errorLabel.Visible = true;
                errorLabel.Text = "Не удалось подключиться к серверу.";
                return;
            }

            clientConnection = new ClientConnection($"ws://{serverAddress}");
            clientConnection.OnConnected += () =>
            {
                //
            };
            clientConnection.OnErrorOccurred += (error) =>
            {
                errorLabel.Visible = true;
                errorLabel.Text = "Не удалось подключиться к серверу.";
            };

            clientConnection.Connect();
            OpenGameForm();
        }
        
        // Запуск сервера и подключение как клиент локально
        private void button2_Click(object sender, EventArgs e)
        {
            serverConnection = new ServerConnection("ws://0.0.0.0:8181");
            serverConnection.Start();

            System.Threading.Thread.Sleep(500);

            clientConnection = new ClientConnection("ws://localhost:8181");
            clientConnection.OnConnected += () =>
            {
                //
            };
            clientConnection.OnErrorOccurred += (error) =>
            {
                errorLabel.Visible = true;
                errorLabel.Text = "Не удалось подключиться к серверу.";
            };

            clientConnection.Connect();
            OpenGameForm();
        }

        private void OpenGameForm()
        {
            GameForm gameForm = new GameForm(clientConnection, serverConnection);

            this.Hide();
            gameForm.Show();
        }
    }
}