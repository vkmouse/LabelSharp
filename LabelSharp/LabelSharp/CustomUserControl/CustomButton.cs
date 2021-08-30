using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LabelSharp.CustomUserControl
{
    public partial class CustomButton : UserControl
    {
        private Color _hoverBackColor = Color.FromArgb(0, 0, 0, 0);
        private Color _pressedBackColor = Color.FromArgb(0, 0, 0, 0);
        private bool _isHover, _isPressed;

        [Browsable(true)]
        public Color HoverBackColor
        {
            get => _hoverBackColor;
            set => _hoverBackColor = value;
        }

        [Browsable(true)]
        public Color PressedBackColor
        {
            get => _pressedBackColor;
            set => _pressedBackColor = value;
        }

        public CustomButton()
        {
            InitializeComponent();
            _isHover = _isPressed = false;
        }

        protected void CustomButton_MouseDown(object sender, MouseEventArgs e)
        {
            _isPressed = true;
            UpdateBackColor();
        }

        protected void CustomButton_MouseUp(object sender, MouseEventArgs e)
        {
            _isPressed = false;
            UpdateBackColor();
        }

        protected void CustomButton_MouseEnter(object sender, EventArgs e)
        {
            _isHover = true;
            UpdateBackColor();
        }

        protected void CustomButton_MouseLeave(object sender, EventArgs e)
        {
            _isHover = false;
            UpdateBackColor();
        }

        private void UpdateBackColor()
        {
            if (_isPressed)
                BackColor = _pressedBackColor;
            else if (_isHover)
                BackColor = _hoverBackColor;
            else
                BackColor = Color.FromArgb(0, 0, 0, 0);
        }
    }
}
