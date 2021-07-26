using System;
using System.Drawing;

namespace ViewerLib
{
    // TODO: 1. 儲存 ROI 成各種格式
    // TODO: 2. 小地圖
    // TODO: 3. 標註模式輔助線
    // TODO: 4. 更新 UML

    public class ViewerKernel : IKernel
    {
        protected float ZoomFactor;
        protected Bitmap SrcImage = null;
        protected Bitmap DstImage = null;
        protected Bitmap PreDstImage = null;
        protected RectangleF SrcRect;
        protected Rectangle DstRect;
        protected RectangleF? PreSrcRect = null;
        protected Rectangle? PreDstRect = null;
        private PointF? _panningFirstLocation = null;
        
        public ViewerKernel(Size size)
        {
            DstRect = new Rectangle(0, 0, size.Width, size.Height);
            DstImage = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                                  System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height,
                                  System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            PreDstImage = new Bitmap(DstImage.Width, DstImage.Height, DstImage.PixelFormat);
        }

        public Image Image
        {
            get => DstImage;
            set
            {
                if (SrcImage != null)
                    SrcImage.Dispose();
                SrcImage = value as Bitmap;
                PreSrcRect = null;
                PreDstRect = null;
                Operate(OperateType.VIEWER_ZOOM_FIT);
            }
        }

        public virtual void Operate(OperateType type, params object[] values)
        {
            switch (type)
            {
                case OperateType.VIEWER_ZOOM_IN:
                case OperateType.VIEWER_ZOOM_OUT:
                    Zoom(type, (Point?)values[0]);
                    break;

                case OperateType.VIEWER_ZOOM_FIT:
                    Zoom(type);
                    break;

                case OperateType.VIEWER_RESIZE:
                    Resize((Size)values[0]);
                    break;

                case OperateType.VIEWER_PANNING_BEGIN:
                case OperateType.VIEWER_PANNING_MOVE:
                case OperateType.VIEWER_PANNING_END:
                    Panning(type, (Point)values[0]);
                    break;
            }

            GetImage();
        }

        public virtual void Clear()
        {
            SrcImage = null;
            PreSrcRect = PreDstRect = null;
            _panningFirstLocation = null;
            Paint.Clean(ref DstImage, DstRect);
            Paint.Clean(ref PreDstImage, DstRect);
            return;
        }

        private void Zoom(OperateType type, Point? location = null)
        {
            float minZoomFactor = Math.Max(1f / SrcImage.Width * 100, 1f / SrcImage.Width * 100);
            float maxZoomFactor = 6400;

            switch (type)
            {
                case OperateType.VIEWER_ZOOM_IN:
                    ZoomFactor *= 1.12f;
                    break;
                case OperateType.VIEWER_ZOOM_OUT:
                    ZoomFactor /= 1.12f;
                    break;
                case OperateType.VIEWER_ZOOM_FIT:
                    float zoomFactorX = DstRect.Width / (float)SrcImage.Width;
                    float zoomFactorY = DstRect.Height / (float)SrcImage.Height;
                    ZoomFactor = zoomFactorX > zoomFactorY ? zoomFactorY * 100 : zoomFactorX * 100;
                    Math.Min(Math.Max(ZoomFactor, minZoomFactor), maxZoomFactor);
                    return;
            }

            // Adjust strategy in 100% to 500%
            for (int i = 100; i <= 500; i = i + 100)
                if (i * 0.9 < ZoomFactor && ZoomFactor < i * 1.1)
                    ZoomFactor = i;

            // Adjust strategy 500% to Max
            if (ZoomFactor > 500)
                ZoomFactor = (float)Math.Round(ZoomFactor / 100, 0) * 100;

            // Limitation of zoom factor
            ZoomFactor = Math.Min(Math.Max(ZoomFactor, minZoomFactor), maxZoomFactor);

            // Offset to match previous location
            Point _location = location ?? new Point(0, 0);
            PointF realLocation = ToRealLocationF(_location);
            SrcRect.X = realLocation.X - DstRect.Width * 100 / ZoomFactor * _location.X / DstRect.Width;
            SrcRect.Y = realLocation.Y - DstRect.Height * 100 / ZoomFactor * _location.Y / DstRect.Height;
        }

        private void Resize(Size size)
        {
            DstRect = new Rectangle(0, 0, size.Width, size.Height);
        }

        private void Panning(OperateType type, Point location)
        {
            PointF realLocation = ToRealLocationF(location);

            if (type is OperateType.VIEWER_PANNING_BEGIN)
            {
                _panningFirstLocation = realLocation;
            }
            else if (_panningFirstLocation != null)
            {
                PointF firstLocation = (PointF)_panningFirstLocation;
                SrcRect.X = SrcRect.X + firstLocation.X - realLocation.X;
                SrcRect.Y = SrcRect.Y + firstLocation.Y - realLocation.Y;

                if (type is OperateType.VIEWER_PANNING_END)
                {
                    _panningFirstLocation = null;
                }
            }
        }

        protected PointF ToRealLocationF(Point location)
        {
            return new PointF(SrcRect.X + SrcRect.Width *  location.X / DstRect.Width,
                              SrcRect.Y + SrcRect.Height * location.Y / DstRect.Height);
        }

        protected Point ToRealLocation(Point location)
        {
            return new Point((int)(SrcRect.X + SrcRect.Width * location.X / DstRect.Width),
                             (int)(SrcRect.Y + SrcRect.Height * location.Y / DstRect.Height));
        }

        protected Point ToWindowLocation(PointF location)
        {
            return new Point((int)((location.X - SrcRect.X) * DstRect.Width / SrcRect.Width),
                             (int)((location.Y - SrcRect.Y) * DstRect.Height / SrcRect.Height));
        }

        protected Rectangle ToWindowRect(RectangleF rect)
        {
            Point location = ToWindowLocation(rect.Location);
            Size size = new Size((int)(rect.Width * ZoomFactor / 100), (int)(rect.Height * ZoomFactor / 100));
            return new Rectangle(location, size);
        }

        protected virtual void GetImage()
        {
            if (SrcImage == null)
                return;

            // Update srcRect
            SrcRect.Width = DstRect.Width * 100 / ZoomFactor;
            SrcRect.Height = DstRect.Height * 100 / ZoomFactor;
            SrcRect.X = Math.Max(Math.Min(SrcRect.X, SrcImage.Width - SrcRect.Width), 0);
            SrcRect.Y = Math.Max(Math.Min(SrcRect.Y, SrcImage.Height - SrcRect.Height), 0);

            // Update dstImage
            if (PreSrcRect != SrcRect || PreDstRect != DstRect)
            {
                Paint.Clean(ref DstImage, DstRect);
                Paint.DrawImage(SrcImage, SrcRect, DstRect, ref DstImage);
                PreSrcRect = SrcRect;
                PreDstRect = DstRect;
                Paint.CopyTo(DstImage, ref PreDstImage, DstRect);
            }
            else
            {
                Paint.CopyTo(PreDstImage, ref DstImage, DstRect);
            }
        }
    }
}
