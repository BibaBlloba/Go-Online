using System;
using System.Drawing;
using System.Windows.Forms;

namespace L9_new
{
    public class RoundSettingsForm : Form
    {
        private ComboBox boardSizeCombo;
        private RadioButton blackRadio;
        private RadioButton whiteRadio;
        private Button startButton;

        public RoundSettingsForm()
        {
            InitializeComponents();
        }

        public int BoardSize => boardSizeCombo.SelectedItem is int value ? value : 19;
        public string SelectedColor => blackRadio.Checked ? "Black" : "White";

        private void InitializeComponents()
        {
            Text = "Настройки раунда";
            ClientSize = new Size(300, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            Label boardSizeLabel = new Label
            {
                Text = "Размер карты:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            boardSizeCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 45),
                Size = new Size(240, 28)
            };
            boardSizeCombo.Items.Add(9);
            boardSizeCombo.Items.Add(13);
            boardSizeCombo.Items.Add(19);
            boardSizeCombo.SelectedIndex = 2;

            Label colorLabel = new Label
            {
                Text = "Ваша команда:",
                Location = new Point(20, 90),
                AutoSize = true
            };

            blackRadio = new RadioButton
            {
                Text = "Черные",
                Location = new Point(20, 115),
                AutoSize = true,
                Checked = true
            };

            whiteRadio = new RadioButton
            {
                Text = "Белые",
                Location = new Point(120, 115),
                AutoSize = true
            };

            startButton = new Button
            {
                Text = "Начать раунд",
                Location = new Point(20, 155),
                Size = new Size(240, 35)
            };
            startButton.Click += (sender, args) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.Add(boardSizeLabel);
            Controls.Add(boardSizeCombo);
            Controls.Add(colorLabel);
            Controls.Add(blackRadio);
            Controls.Add(whiteRadio);
            Controls.Add(startButton);
        }
    }
}
