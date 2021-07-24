using System;
using System.Windows.Forms;

namespace LabelSharp
{
    class LabelWindowViewModel
    {
        private LabelWindowView _view;
        private bool _isCancel;

        public bool IsCancel 
        {
            get => _isCancel;
        }

        public LabelWindowViewModel(LabelWindowView view)
        {
            _view = view;
            _view.KeyPreview = true;

            _view.Shown += new EventHandler(view_Shown);
            _view.KeyDown += new KeyEventHandler(view_KeyDown);
            _view.btnConfirm.Click += new EventHandler(btnConfirm_Click);
            _view.lstOtherName.SelectedIndexChanged += new EventHandler(lstOtherName_SelectedIndexChanged);
            _view.lstOtherName.MouseDoubleClick += new MouseEventHandler(lstOtherName_MouseDoubleClick);
            _view.lstOtherName.KeyDown += new KeyEventHandler(lstOtherName_KeyDown);
        }

        private void view_Shown(object sender, EventArgs e)
        {
            // Initial variable
            _isCancel = true;

            // Set focus to TextBox
            _view.txtName.Focus();

            // Set fixed size
            _view.FormBorderStyle = FormBorderStyle.FixedSingle;
            _view.MinimizeBox = false;
            _view.MaximizeBox = false;
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            ConfirmAndClose();
        }

        private void view_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                ConfirmAndClose();
            }
            else if (e.KeyData == Keys.Escape)
            {
                _view.Close();
            }
            else if (e.KeyData == Keys.Up || e.KeyData == Keys.Down)
            {
                // Set focus to ListBox
                if (!_view.lstOtherName.Focused)
                {
                    _view.lstOtherName.Focus();
                    lstOtherName_KeyDown(sender, e);
                }
            }
            else if (e.KeyData == Keys.Left || e.KeyData == Keys.Right)
            {
                // Set focus to TextBox
                if (!_view.txtName.Focused)
                {
                    _view.txtName.Focus();
                }
            }
        }

        private void lstOtherName_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set selected item to TextBox
            if (_view.lstOtherName.SelectedIndex != -1)
                _view.txtName.Text = _view.lstOtherName.SelectedItem.ToString();
        }

        private void lstOtherName_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Location is matched
            if (_view.lstOtherName.IndexFromPoint(e.Location) != ListBox.NoMatches)
            {
                ConfirmAndClose();
            }
        }

        private void lstOtherName_KeyDown(object sender, KeyEventArgs e)
        {
            int count = _view.lstOtherName.Items.Count;

            // Rotation selected for ListBox
            if (count > 0)
            {
                e.Handled = true;
                
                int shift = 0;
                if (e.KeyData == Keys.Up) 
                    shift = -1;
                else if (e.KeyData == Keys.Down) 
                    shift = 1;

                int index = _view.lstOtherName.SelectedIndex;
                _view.lstOtherName.SelectedIndex = (Math.Max(index, 0) + count + shift) % count;
            }
        }

        private void ConfirmAndClose()
        {
            if (_view.txtName.Text != string.Empty)
            {
                _isCancel = false;
                _view.Close();
            }
        }
    }
}
