using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace LabelSharp.CustomUserControl
{
    public partial class CustomTextBoxButton : CustomButton
    {
        private Form _popupTextBoxForm = null;
        private TextBox _popupTextBox = null;

        public void InitPopupForm()
        {
            _popupTextBox = new TextBox();
            _popupTextBox.Dock = DockStyle.Fill;
            _popupTextBox.Location = new System.Drawing.Point(0, 0);
            _popupTextBox.Margin = new Padding(0, 0, 0, 0);
            _popupTextBox.BorderStyle = BorderStyle.None;
            _popupTextBox.KeyDown += new KeyEventHandler(PopupTextBox_KeyDown);

            _popupTextBoxForm = new Form();
            _popupTextBoxForm.Text = string.Empty;
            _popupTextBoxForm.ControlBox = false;
            _popupTextBoxForm.MaximizeBox = false;
            _popupTextBoxForm.MinimizeBox = false;
            _popupTextBoxForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            _popupTextBoxForm.TopMost = true;
            _popupTextBoxForm.ShowInTaskbar = false;
            _popupTextBoxForm.Controls.Add(_popupTextBox);
            _popupTextBoxForm.StartPosition = FormStartPosition.Manual;
        }

        public CustomTextBoxButton()
        {
            InitializeComponent();
            InitPopupForm();
        }

        [Browsable(true)]
        public string LabelText
        {
            get => label.Text;
            set => label.Text = value;
        }

        public event EventHandler TextChanged;

        private void CustomTextBoxButton_MouseClick(object sender, MouseEventArgs e)
        {
            var size = Size;
            size.Height = 22;
            var location = label.PointToScreen(label.Location);
            location.Y = location.Y - 22;

            _popupTextBoxForm.Size = _popupTextBoxForm.MinimumSize = _popupTextBoxForm.MaximumSize = size;
            _popupTextBoxForm.Location = location;
            _popupTextBox.Text = label.Text;
            _popupTextBox.Font = Font;

            _popupTextBoxForm.ShowDialog();
        }

        private void label_MouseLeave(object sender, EventArgs e)
        {
            CustomButton_MouseLeave(sender, e);
        }

        private void label_MouseUp(object sender, MouseEventArgs e)
        {
            CustomButton_MouseUp(sender, e);
        }

        private void label_MouseEnter(object sender, EventArgs e)
        {
            CustomButton_MouseEnter(sender, e);
        }

        private void label_MouseDown(object sender, MouseEventArgs e)
        {
            CustomButton_MouseDown(sender, e);
        }

        private void PopupTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                label.Text = _popupTextBox.Text;
                _popupTextBoxForm.Close();

                EventHandler handler = TextChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _popupTextBoxForm.Close();
            }
        }
    }
}
