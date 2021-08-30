using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ViewerLib;

namespace LabelSharp
{
    class LabelSharpViewModel
    {
        enum LabelMode { LABEL_MODE_VIEW, LABEL_MODE_DETECTION };

        private LabelSharpView _view;
        private IKernel _kernel;
        private LabelMode _mode;
        private Image _srcImage;

        // For demo variable
        private static readonly List<string> _imageExts = new List<string> { 
            ".JPG", 
            ".JPEG", 
            ".BMP", 
            ".PNG" 
        };
        private string _path = $"C:\\Users\\{Environment.UserName}\\Pictures";
        private string _filename;

        public LabelSharpViewModel(LabelSharpView view)
        {
            _view = view;
            _view.pictureBox.MouseDown += new MouseEventHandler(pictureBox_MouseDown);
            _view.pictureBox.MouseMove += new MouseEventHandler(pictureBox_MouseMove);
            _view.pictureBox.MouseUp += new MouseEventHandler(pictureBox_MouseUp);
            _view.pictureBox.MouseWheel += new MouseEventHandler(pictureBox_MouseWheel);
            _view.pictureBox.MouseClick += new MouseEventHandler(pictureBox_MouseClick);
            _view.pictureBox.Resize += new EventHandler(pictureBox_Resize);
            _view.KeyPress += new KeyPressEventHandler(View_KeyPress);
            _view.KeyDown += new KeyEventHandler(View_KeyDown);
            _view.KeyPreview = true;
            _view.btnZoomFactor.TextChanged += new EventHandler(CustomTextBoxButton_TextChanged);

            _kernel = new DetectionKernel(_view.pictureBox.Size);
            _view.pictureBox.Image = _kernel.Image;

            _mode = LabelMode.LABEL_MODE_VIEW;
            _view.lstFileList.DataSource = Directory.GetFiles(_path).Where(x => _imageExts.Contains(Path.GetExtension(x).ToUpper()))
                                                                    .Select(x => Path.GetFileName(x))                                                                   
                                                                    .ToList();
            _view.lstFileList.SelectedValueChanged += new EventHandler(lstFileList_SelectedValueChanged);
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_mode == LabelMode.LABEL_MODE_DETECTION)
                {
                    _kernel.Operate(OperateType.DETECTION_LABEL_BEGIN, e.Location);
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    _kernel.Operate(OperateType.DETECTION_MOVE_ROI_BEGIN, e.Location);
                }
                else
                {
                    _kernel.Operate(OperateType.VIEWER_PANNING_BEGIN, e.Location);
                    _view.Cursor = Cursors.SizeAll;
                }
                _view.pictureBox.Refresh();
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_mode == LabelMode.LABEL_MODE_DETECTION)
                {
                    _kernel.Operate(OperateType.DETECTION_LABEL_MOVE, e.Location);
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    _kernel.Operate(OperateType.DETECTION_MOVE_ROI_MOVE, e.Location);
                }
                else
                {
                    _kernel.Operate(OperateType.VIEWER_PANNING_MOVE, e.Location);
                }
                _view.pictureBox.Refresh();
            }
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_mode == LabelMode.LABEL_MODE_DETECTION)
                {
                    _mode = LabelMode.LABEL_MODE_VIEW;
                    _kernel.Operate(OperateType.DETECTION_LABEL_END, e.Location);

                    bool isCancel = LabelWindowView.Show(_view.PointToScreen(_view.pictureBox.Location) + (Size)e.Location);
                    if (isCancel)
                    {
                        _kernel.Operate(OperateType.DETECTION_DELETE_ROI);
                    }
                    else
                    {
                        _kernel.Operate(OperateType.DETECTION_RENAME_ROI, LabelWindowView.ClassName);
                    }
                }
                else if (Control.ModifierKeys == Keys.Control)
                {
                    _kernel.Operate(OperateType.DETECTION_MOVE_ROI_END, e.Location);
                }
                else
                {
                    _kernel.Operate(OperateType.VIEWER_PANNING_END, e.Location);
                }
                _view.Cursor = Cursors.Default;
                _view.pictureBox.Refresh();
            }
        }

        private void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control && e.Delta > 0)
            {
                _kernel.Operate(OperateType.VIEWER_ZOOM_IN, e.Location);
            }
            else if (Control.ModifierKeys == Keys.Control && e.Delta < 0)
            {
                _kernel.Operate(OperateType.VIEWER_ZOOM_OUT, e.Location);
            }
            _view.pictureBox.Refresh();
        }

        private void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _kernel.Operate(OperateType.DETECTION_SELECT_ROI, e.Location);
                _view.pictureBox.Refresh();
            }
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            _kernel.Operate(OperateType.VIEWER_RESIZE, _view.pictureBox.Size);
            _view.pictureBox.Refresh();
        }

        private void View_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == 'w' || e.KeyChar == 'W') && _mode == LabelMode.LABEL_MODE_VIEW)
            {
                _mode = LabelMode.LABEL_MODE_DETECTION;
                _view.Cursor = Cursors.Cross;
            }
            else
            {
                _mode = LabelMode.LABEL_MODE_VIEW;
                _view.Cursor = Cursors.Default;
            }
        }
        
        private void View_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.S | Keys.Control))
            {
                Save();
            }
            else if (e.KeyData == (Keys.O | Keys.Control))
            {
                Load();
            }
            else if (e.KeyData == Keys.Delete)
            {
                _kernel.Operate(OperateType.DETECTION_DELETE_ROI);
            }
            _view.pictureBox.Refresh();
        }

        private void lstFileList_SelectedValueChanged(object sender, EventArgs e)
        {
            _filename = Path.Combine(_path, (string)_view.lstFileList.SelectedItem);
            _kernel.Clear();
            if (_srcImage != null)
                _srcImage.Dispose();
            _srcImage = Image.FromFile(_filename);
            _kernel.Image = _srcImage;
            _view.pictureBox.Refresh();
        }

        private void CustomTextBoxButton_TextChanged(object sender, EventArgs e)
        {
            int result = int.TryParse(_view.btnZoomFactor.LabelText, out result) ? result : 100;
            _view.btnZoomFactor.LabelText = $"{result}%";
        }

        private void Save()
        {
            DetectionFileInfo info = new DetectionFileInfo()
            {
                imageWidth = _srcImage.Width,
                imageHeight = _srcImage.Height,
                imageDepth = _srcImage.PixelFormat is PixelFormat.Format8bppIndexed ? 1 : 3,
                bboxes = (_kernel as DetectionKernel).GetBndBoxes()
            };
            info.imagePath = _filename;
            info.saveDir = Path.GetDirectoryName(_filename);
            DetectionFile.Save(info, AnnotationFormat.ANNOTATION_FORMAT_PASCAL_VOC);
            DetectionFile.Save(info, AnnotationFormat.ANNOTATION_FORMAT_TESSERACT);
        }

        private void Load()
        {
            DetectionFileInfo info = new DetectionFileInfo()
            {
                imageWidth = _srcImage.Width,
                imageHeight = _srcImage.Height,
                imageDepth = _srcImage.PixelFormat is PixelFormat.Format8bppIndexed ? 1 : 3,
                bboxes = (_kernel as DetectionKernel).GetBndBoxes()
            };
            info.imagePath = _filename;
            info.saveDir = Path.GetDirectoryName(_filename);
            //var bboxes = DetectionFile.Load(info, AnnotationFormat.ANNOTATION_FORMAT_TESSERACT);
            var bboxes = DetectionFile.Load(info, AnnotationFormat.ANNOTATION_FORMAT_PASCAL_VOC);
            (_kernel as DetectionKernel).SetBndBoxes(bboxes);
        }
    }
}
