using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ViewerLib;

namespace LabelSharp
{
    class LabelSharpViewModel
    {
        enum LabelMode { LABEL_MODE_VIEW, LABEL_MODE_DETECTION };

        private LabelSharpView view;
        private IKernel kernel;
        private LabelMode mode;

        // For demo variable
        private string path = $"C:\\Users\\{Environment.UserName}\\Pictures";
        private IEnumerator<string> files;

        private Image pictureBox_Image
        {
            set
            {
                if (!(view.pictureBox.BackgroundImage is null))
                    view.pictureBox.BackgroundImage.Dispose();
                view.pictureBox.BackgroundImage = value;
            }
        }

        public LabelSharpViewModel(LabelSharpView view)
        {
            this.view = view;
            view.KeyPreview = true;

            mode = LabelMode.LABEL_MODE_VIEW;

            view.button.Click += new EventHandler(button_Click);
            view.pictureBox.MouseDown += new MouseEventHandler(pictureBox_MouseDown);
            view.pictureBox.MouseMove += new MouseEventHandler(pictureBox_MouseMove);
            view.pictureBox.MouseUp += new MouseEventHandler(pictureBox_MouseUp);
            view.pictureBox.MouseWheel += new MouseEventHandler(pictureBox_MouseWheel);
            view.pictureBox.MouseClick += new MouseEventHandler(pictureBox_MouseClick);
            view.pictureBox.Resize += new EventHandler(pictureBox_Resize);
            view.KeyPress += new KeyPressEventHandler(View_KeyPress);
            view.KeyDown += new KeyEventHandler(View_KeyDown);

            kernel = new DetectionKernel(view.pictureBox.Size);
            files = Directory.GetFiles(path).ToList().GetEnumerator();
        }

        private void button_Click(object sender, EventArgs e)
        {
            bool hasNext;
            do
            {
                hasNext = files.MoveNext();
                if (Path.GetExtension(files.Current) is ".png" ||
                    Path.GetExtension(files.Current) is ".bmp" ||
                    Path.GetExtension(files.Current) is ".jpg")
                {
                    kernel.Clear();
                    kernel.Image = Image.FromFile(files.Current);
                    pictureBox_Image = kernel.Image;
                    break;
                }
            } while (hasNext);

            if (!hasNext)
            {
                files.Dispose();
                files = Directory.GetFiles(path).ToList().GetEnumerator();
            }
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mode == LabelMode.LABEL_MODE_DETECTION)
                {
                    pictureBox_Image = kernel.Operate(OperateType.DETECTION_LABEL_BEGIN, e.Location);
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    pictureBox_Image = kernel.Operate(OperateType.DETECTION_MOVE_ROI_BEGIN, e.Location);
                }
                else
                {
                    pictureBox_Image = kernel.Operate(OperateType.VIEWER_PANNING_BEGIN, e.Location);
                    view.Cursor = Cursors.SizeAll;
                }
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mode == LabelMode.LABEL_MODE_DETECTION)
                {
                    pictureBox_Image = kernel.Operate(OperateType.DETECTION_LABEL_MOVE, e.Location);
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    pictureBox_Image = kernel.Operate(OperateType.DETECTION_MOVE_ROI_MOVE, e.Location);
                }
                else
                {
                    pictureBox_Image = kernel.Operate(OperateType.VIEWER_PANNING_MOVE, e.Location);
                }
            }

            view.pictureBox.Refresh();
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mode == LabelMode.LABEL_MODE_DETECTION)
                {
                    mode = LabelMode.LABEL_MODE_VIEW;
                    pictureBox_Image = kernel.Operate(OperateType.DETECTION_LABEL_END, e.Location);

                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    pictureBox_Image = kernel.Operate(OperateType.DETECTION_MOVE_ROI_END, e.Location);

                }
                else
                {
                    pictureBox_Image = kernel.Operate(OperateType.VIEWER_PANNING_END, e.Location);
                }
            }
            view.Cursor = Cursors.Default;
            view.pictureBox.Refresh();
        }

        private void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control && e.Delta > 0)
            {
                pictureBox_Image = kernel.Operate(OperateType.VIEWER_ZOOM_IN, e.Location);
            }
            else if (Control.ModifierKeys == Keys.Control && e.Delta < 0)
            {
                pictureBox_Image = kernel.Operate(OperateType.VIEWER_ZOOM_OUT, e.Location);
            }
        }

        private void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pictureBox_Image = kernel.Operate(OperateType.DETECTION_SELECT_ROI, e.Location);
            }
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            pictureBox_Image = kernel.Operate(OperateType.VIEWER_RESIZE, view.pictureBox.Size); 
        }

        private void View_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'w' && mode == LabelMode.LABEL_MODE_VIEW)
            {
                mode = LabelMode.LABEL_MODE_DETECTION;
                view.Cursor = Cursors.Cross;
            }
            else
            {
                mode = LabelMode.LABEL_MODE_VIEW;
                view.Cursor = Cursors.Default;
            }
        }
        
        private void View_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.S | Keys.Control))
            {
                MessageBox.Show("Ctrl+S");
            }
            else if (e.KeyData == Keys.Delete)
            {
                pictureBox_Image = kernel.Operate(OperateType.DETECTION_DELETE_ROI);
                view.pictureBox.Refresh();
            }
        }
    }
}
