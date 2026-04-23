using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace L9_new
{
    public partial class GameForm : Form
    {
        private ClientConnection clientConnection;

        public GameForm(ClientConnection connection)
        {
            InitializeComponent();
            clientConnection = connection;

            if (clientConnection != null)
            {
                clientConnection.OnMessageReceived += HandleMessageReceived;
                clientConnection.OnDisconnected += HandleDisconnected;
            }
        }

        private void HandleMessageReceived(string message)
        {
            // Обработка сообщений от сервера
            Console.WriteLine($"GameForm received: {message}");
        }

        private void HandleDisconnected()
        {
            // Обработка отключения
            MessageBox.Show("Соединение с сервером разорвано");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (clientConnection != null)
            {
                clientConnection.OnMessageReceived -= HandleMessageReceived;
                clientConnection.OnDisconnected -= HandleDisconnected;
            }

            base.OnFormClosing(e);
        }
    }
}
