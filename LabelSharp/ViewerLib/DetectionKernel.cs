using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace ViewerLib
{
    public class DetectionKernel : ViewerKernel
    {
        private List<DetectionUnit> bboxes;
        private Rectangle? labelingRoi = null;
        private Point? labelFirstLocation = null;
        private Point? moveRoiFirstLocation = null;
        private int selectedIndex;
        private int moveSelectedIndex;

        private int _penWidth = 3;
        private Font _font = new Font("Arial", 12);
        private int _fontHeight = 16;
        private Color _overlapColor = Color.FromArgb(150, 0, 127, 255);
        private Dictionary<string, Tuple<Color, Color>> _borderAndStringColors;

        public DetectionKernel(Size size) : base(size)
        {
            bboxes = new List<DetectionUnit>();
        }

        public override Image Operate(OperateType type, params object[] values)
        {
            base.Operate(type, values);

            switch (type)
            {
                case OperateType.DETECTION_LABEL_BEGIN:
                case OperateType.DETECTION_LABEL_MOVE:
                case OperateType.DETECTION_LABEL_END:
                    Label(type, (Point)values[0]);
                    break;

                case OperateType.DETECTION_SELECT_ROI:
                    SelectRoi((Point)values[0]);
                    break;

                case OperateType.DETECTION_DELETE_ROI:
                    DeleteSelectedRoi();
                    break;

                case OperateType.DETECTION_MOVE_ROI_BEGIN:
                case OperateType.DETECTION_MOVE_ROI_MOVE:
                case OperateType.DETECTION_MOVE_ROI_END:
                    MoveSelectedRoi(type, (Point)values[0]);
                    break;

                case OperateType.DETECTION_RENAME_ROI:
                    RenameSelectedRoi((string)values[0]);
                    break;
            }

            return Image;
        }

        public override void Clear()
        {
            base.Clear();
            bboxes.Clear();
            labelingRoi = null;
            labelFirstLocation = moveRoiFirstLocation = null;
            selectedIndex = -1;
        }

        public List<DetectionUnit> GetBndBoxes()
        {
            return bboxes;
        }

        public Image SetBndBoxes(List<DetectionUnit> bboxes)
        {
            this.bboxes = bboxes;
            return Image;
        }

        private void Label(OperateType type, Point location)
        {
            Point realLocation = ToRealLocation(location);
            selectedIndex = -1;

            if (type is OperateType.DETECTION_LABEL_BEGIN)
            {
                labelFirstLocation = realLocation;
            }
            else if (labelFirstLocation != null)
            {
                Point firstLocation = (Point)labelFirstLocation;
                labelingRoi = new Rectangle(Math.Min(firstLocation.X, realLocation.X),
                                            Math.Min(firstLocation.Y, realLocation.Y),
                                            Math.Abs(firstLocation.X - realLocation.X) + 1,
                                            Math.Abs(firstLocation.Y - realLocation.Y) + 1);

                if (type is OperateType.DETECTION_LABEL_END)
                {
                    bboxes.Add(new DetectionUnit((Rectangle)labelingRoi, " "));
                    selectedIndex = bboxes.Count - 1;
                    labelingRoi = null;
                    labelFirstLocation = null;
                }
            }
        }

        private void SelectRoi(Point location)
        {
            Point realLocation = ToRealLocation(location);
            for (int i = 0; i < bboxes.Count; i++)
            {
                if (bboxes[i].Rect.Contains(realLocation))
                {
                    selectedIndex = i;
                    return;
                }
            }
            selectedIndex = -1;
        }

        private void DeleteSelectedRoi()
        {
            if (selectedIndex >= 0 && bboxes.Count > selectedIndex)
                bboxes.RemoveAt(selectedIndex);
            selectedIndex = -1;
        }

        private void MoveSelectedRoi(OperateType type, Point location)
        {
            Point realLocation = ToRealLocation(location);

            if (0 <= selectedIndex && selectedIndex < bboxes.Count)
            {
                if (type is OperateType.DETECTION_MOVE_ROI_BEGIN && bboxes[selectedIndex].Rect.Contains(realLocation))
                {
                    labelFirstLocation = bboxes[selectedIndex].Rect.Location;
                    moveRoiFirstLocation = realLocation;
                    moveSelectedIndex = selectedIndex;
                }
                else if (moveSelectedIndex != -1 && labelFirstLocation != null && moveRoiFirstLocation != null)
                {
                    Rectangle roi = bboxes[moveSelectedIndex].Rect;
                    Point labelFirstLocation = (Point)this.labelFirstLocation;
                    Point moveRoiFirstLocation = (Point)this.moveRoiFirstLocation;
                    roi.X = labelFirstLocation.X + realLocation.X - moveRoiFirstLocation.X;
                    roi.Y = labelFirstLocation.Y + realLocation.Y - moveRoiFirstLocation.Y;
                    bboxes[moveSelectedIndex].Rect = roi;
                }
            }
            if (type is OperateType.DETECTION_MOVE_ROI_END)
            {
                labelFirstLocation = moveRoiFirstLocation = null;
                moveSelectedIndex = -1;
            }
        }

        private void RenameSelectedRoi(string name)
        {
            bboxes[selectedIndex].ClassName = name;
        }

        protected override Image GetImage()
        {
            Bitmap dstImage = base.GetImage() as Bitmap;
            if (dstImage == null)
                return null;

            // Draw rectangle
            DrawBndBoxes(ref dstImage);

            // Draw labeling transparent
            if (labelingRoi != null)
            {
                DrawLabelingRoi(ref dstImage);
            }

            // Draw selected roi transparent
            if (selectedIndex >= 0 && bboxes.Count > selectedIndex)
            {
                DrawSelectedRoi(ref dstImage);
            }

            return dstImage;
        }

        private void DrawBndBoxes(ref Bitmap inputOutputImage)
        {
            if (_borderAndStringColors == null)
                _borderAndStringColors = new Dictionary<string, Tuple<Color, Color>>();

            // Prepare boader and string color
            foreach (DetectionUnit box in bboxes)
            {
                if (_borderAndStringColors.ContainsKey(box.ClassName))
                    continue;

                int argb = 1;
                foreach (int value in new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(box.ClassName)))
                {
                    argb = (int)(argb * (value + 1) | 0xFF000000);
                }

                Color borderColor = Color.FromArgb(argb);
                Color stringColor = borderColor.GetBrightness() > 0.6 ? Color.Black : Color.White;
                _borderAndStringColors.Add(box.ClassName, new Tuple<Color, Color>(borderColor, stringColor));
            }

            // Draw rectangle
            using (Graphics g = Graphics.FromImage(inputOutputImage))
            {
                foreach (DetectionUnit box in bboxes)
                {
                    Color borderColor = _borderAndStringColors[box.ClassName].Item1;
                    Color stringColor = _borderAndStringColors[box.ClassName].Item2;

                    Rectangle roi = ToWindowRect(box.Rect);
                    g.DrawRectangle(new Pen(borderColor, _penWidth), roi);
                    g.FillRectangle(new SolidBrush(borderColor), roi.X - _penWidth / 2, roi.Y - _fontHeight, roi.Width + _penWidth, _fontHeight);
                    g.DrawString(box.ClassName, _font, new SolidBrush(stringColor), roi.X, roi.Y - _fontHeight);
                }
            }
        }

        private void DrawLabelingRoi(ref Bitmap inputOutputImage)
        {
            // Draw labeling rectangle
            using (Graphics g = Graphics.FromImage(inputOutputImage))
            {
                g.DrawRectangle(new Pen(Color.Black, _penWidth), ToWindowRect((Rectangle)labelingRoi));
            }

            // Draw labeling transparent
            Rectangle windowRect = ToWindowRect((Rectangle)labelingRoi);
            windowRect.Location = windowRect.Location - new Size((int)Math.Round(_penWidth / 2f) - 1, (int)Math.Round(_penWidth / 2f) - 1);
            windowRect.Size = windowRect.Size + new Size(_penWidth, _penWidth);
            Paint.DrawTransparent(ref inputOutputImage, windowRect, _overlapColor, isInside: false);
        }
    
        private void DrawSelectedRoi(ref Bitmap inputOutputImage)
        {
            // Check transparent region
            Rectangle windowRect = ToWindowRect(bboxes[selectedIndex].Rect);
            int right = Math.Min(Math.Max(windowRect.Right, 1), inputOutputImage.Width);
            int bottom = Math.Min(Math.Max(windowRect.Bottom, 1), inputOutputImage.Height);
            int top = Math.Max(Math.Min(windowRect.Top, bottom - 1), 0);
            int left = Math.Max(Math.Min(windowRect.Left, right - 1), 0);
            if (top > inputOutputImage.Height || left > inputOutputImage.Width || bottom < 0 || right < 0)
                return;

            // Draw transparent
            windowRect = new Rectangle(x: left + (int)Math.Round(_penWidth / 2f),
                                       y: top + (int)Math.Round(_penWidth / 2f),
                                       width: right - left - _penWidth,
                                       height: bottom - top - _penWidth);
            Paint.DrawTransparent(ref inputOutputImage, windowRect, _overlapColor, isInside: true);
        }
    }
}
