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
        private LabelSharpView view;
        private ViewerKernel viewerKernel;

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

            view.button.Click += new EventHandler(button_Click);
            view.pictureBox.MouseDown += new MouseEventHandler(pictureBox_MouseDown);
            view.pictureBox.MouseMove += new MouseEventHandler(pictureBox_MouseMove);
            view.pictureBox.MouseUp += new MouseEventHandler(pictureBox_MouseUp);
            view.pictureBox.MouseWheel += new MouseEventHandler(pictureBox_MouseWheel);
            view.pictureBox.Resize += new EventHandler(pictureBox_Resize);

            viewerKernel = new ViewerKernel(view.pictureBox.Size);
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
                    viewerKernel.image = Image.FromFile(files.Current);
                    pictureBox_Image = viewerKernel.image;
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
                viewerKernel.Panning(e.Location, true);
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pictureBox_Image = viewerKernel.Panning(e.Location);
                view.pictureBox.Refresh();
            }
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pictureBox_Image = viewerKernel.Panning(e.Location);
                view.pictureBox.Refresh();
            }
        }

        private void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control && e.Delta > 0)
            {
                pictureBox_Image = viewerKernel.ZoomIn(e.Location);
            }
            else if (Control.ModifierKeys == Keys.Control && e.Delta < 0)
            {
                pictureBox_Image = viewerKernel.ZoomOut(e.Location);
            }
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            pictureBox_Image = viewerKernel.Resize(view.pictureBox.Size);
        }
    }
}
