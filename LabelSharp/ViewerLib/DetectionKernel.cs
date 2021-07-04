using System;
using System.Drawing;
using System.Collections.Generic;

namespace ViewerLib
{
    public class DetectionKernel : ViewerKernel
    {
        private List<Rectangle> rois;
        private Rectangle? labelingRoi = null;
        private Point? labelFirstLocation = null;
        private Point? moveRoiFirstLocation = null;
        private int selectedIndex;
        private int moveSelectedIndex;

        public DetectionKernel(Size size) : base(size)
        {
            rois = new List<Rectangle>();
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
            }

            return image;
        }

        public override void Clear()
        {
            base.Clear();
            rois.Clear();
            labelingRoi = null;
            labelFirstLocation = moveRoiFirstLocation = null;
            selectedIndex = -1;
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
                    rois.Add((Rectangle)labelingRoi);
                    selectedIndex = rois.Count - 1;
                    labelingRoi = null;
                    labelFirstLocation = null;
                }
            }
        }

        private void SelectRoi(Point location)
        {
            Point realLocation = ToRealLocation(location);
            for (int i = 0; i < rois.Count; i++)
            {
                if (rois[i].Contains(realLocation))
                {
                    selectedIndex = i;
                    return;
                }
            }
            selectedIndex = -1;
        }

        private void DeleteSelectedRoi()
        {
            if (selectedIndex >= 0 && rois.Count > selectedIndex)
                rois.RemoveAt(selectedIndex);
            selectedIndex = -1;
        }

        private void MoveSelectedRoi(OperateType type, Point location)
        {
            Point realLocation = ToRealLocation(location);

            if (0 <= selectedIndex && selectedIndex < rois.Count)
            {
                if (type is OperateType.DETECTION_MOVE_ROI_BEGIN && rois[selectedIndex].Contains(realLocation))
                {
                    labelFirstLocation = rois[selectedIndex].Location;
                    moveRoiFirstLocation = realLocation;
                    moveSelectedIndex = selectedIndex;
                }
                else if (moveSelectedIndex != -1 && labelFirstLocation != null && moveRoiFirstLocation != null)
                {
                    Rectangle roi = rois[moveSelectedIndex];
                    Point labelFirstLocation = (Point)this.labelFirstLocation;
                    Point moveRoiFirstLocation = (Point)this.moveRoiFirstLocation;
                    roi.X = labelFirstLocation.X + realLocation.X - moveRoiFirstLocation.X;
                    roi.Y = labelFirstLocation.Y + realLocation.Y - moveRoiFirstLocation.Y;
                    rois[moveSelectedIndex] = roi;
                }
            }
            if (type is OperateType.DETECTION_MOVE_ROI_END)
            {
                labelFirstLocation = moveRoiFirstLocation = null;
                moveSelectedIndex = -1;
            }
        }

        protected override Image GetImage()
        {
            Bitmap dstImage = base.GetImage() as Bitmap;
            Pen innerPen = new Pen(Color.FromArgb(234, 24, 33), 1);
            Pen outterPen = new Pen(Color.FromArgb(27, 29, 28), 3);
            byte alpha = 140;

            // Draw rectangle
            using (Graphics g = Graphics.FromImage(dstImage))
            {
                foreach (Rectangle roi in rois)
                {
                    g.DrawRectangle(outterPen, ToWindowRect(roi));
                    g.DrawRectangle(innerPen, ToWindowRect(roi));
                }

                // Draw labeling rectangle
                if (labelingRoi != null)
                {
                    g.DrawRectangle(outterPen, ToWindowRect((Rectangle)labelingRoi));
                    g.DrawRectangle(innerPen, ToWindowRect((Rectangle)labelingRoi));
                }
            }

            // Draw labeling transparent
            if (labelingRoi != null)
            {
                Rectangle windowRect = ToWindowRect((Rectangle)labelingRoi);
                Paint.DrawTransparent(ref dstImage, windowRect, alpha, isInside: false);
            }

            // Draw selected roi transparent
            if (selectedIndex >= 0 && rois.Count > selectedIndex)
            {
                int top, bottom, left, right;
                Rectangle windowRect = ToWindowRect(rois[selectedIndex]);

                right = Math.Min(Math.Max(windowRect.Right, 1), dstImage.Width);
                bottom = Math.Min(Math.Max(windowRect.Bottom, 1), dstImage.Height);
                top = Math.Max(Math.Min(windowRect.Top, bottom - 1), 0);
                left = Math.Max(Math.Min(windowRect.Left, right - 1), 0);

                if (top > dstImage.Width || left > dstImage.Width || bottom < 0 || right < 0)
                    return dstImage;

                windowRect = new Rectangle(left, top, right - left, bottom - top);
                Paint.DrawTransparent(ref dstImage, windowRect, alpha, isInside: true);
            }

            return dstImage;
        }
    }
}
