using System.Drawing;
using System.Windows.Forms;

namespace L9_new
{
    partial class GameForm
    {
        private Panel boardPanel;
        private Label statusLabel;
        private Button endGameButton;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GameForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "GameForm";
            this.Text = "GameForm";
            this.ResumeLayout(false);
            this.MaximumSize = new System.Drawing.Size(800, 900);
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

        #endregion
    }
}