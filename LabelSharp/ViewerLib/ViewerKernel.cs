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
        private float zoomFactor;
        private Bitmap srcImage = null;
        private Bitmap dstImage = null;
        private RectangleF srcRect;
        private Rectangle dstRect;
        private RectangleF? preSrcRect = null;
        private Rectangle? preDstRect = null;
        private PointF? panningFirstLocation = null;

        public ViewerKernel(Size size)
        {
            dstRect = new Rectangle(0, 0, size.Width, size.Height);
        }

        public Image image
        {
            get => GetImage();
            set
            {
                if (srcImage != null)
                    srcImage.Dispose();
                srcImage = value as Bitmap;
                preSrcRect = null;
                preDstRect = null;
                Zoom(OperateType.VIEWER_ZOOM_FIT);
            }
        }

        public virtual Image Operate(OperateType type, params object[] values)
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

            return image;
        }

        public virtual void Clear()
        {
            srcImage = dstImage = null;
            preSrcRect = preDstRect = null;
            panningFirstLocation = null;
            return;
        }

        private void Zoom(OperateType type, Point? location = null)
        {
            float minZoomFactor = Math.Max(1f / srcImage.Width * 100, 1f / srcImage.Width * 100);
            float maxZoomFactor = 6400;

            switch (type)
            {
                case OperateType.VIEWER_ZOOM_IN:
                    zoomFactor *= 1.12f;
                    break;
                case OperateType.VIEWER_ZOOM_OUT:
                    zoomFactor /= 1.12f;
                    break;
                case OperateType.VIEWER_ZOOM_FIT:
                    float zoomFactorX = dstRect.Width / (float)srcImage.Width;
                    float zoomFactorY = dstRect.Height / (float)srcImage.Height;
                    zoomFactor = zoomFactorX > zoomFactorY ? zoomFactorY * 100 : zoomFactorX * 100;
                    Math.Min(Math.Max(zoomFactor, minZoomFactor), maxZoomFactor);
                    return;
            }

            // Adjust strategy in 100% to 500%
            for (int i = 100; i <= 500; i = i + 100)
                if (i * 0.9 < zoomFactor && zoomFactor < i * 1.1)
                    zoomFactor = i;

            // Adjust strategy 500% to Max
            if (zoomFactor > 500)
                zoomFactor = (float)Math.Round(zoomFactor / 100, 0) * 100;

            // Limitation of zoom factor
            zoomFactor = Math.Min(Math.Max(zoomFactor, minZoomFactor), maxZoomFactor);

            // Offset to match previous location
            Point _location = location ?? new Point(0, 0);
            PointF realLocation = ToRealLocationF(_location);
            srcRect.X = realLocation.X - dstRect.Width * 100 / zoomFactor * _location.X / dstRect.Width;
            srcRect.Y = realLocation.Y - dstRect.Height * 100 / zoomFactor * _location.Y / dstRect.Height;
        }

        private void Resize(Size size)
        {
            dstRect = new Rectangle(0, 0, size.Width, size.Height);
        }

        private void Panning(OperateType type, Point location)
        {
            PointF realLocation = ToRealLocationF(location);

            if (type is OperateType.VIEWER_PANNING_BEGIN)
            {
                panningFirstLocation = realLocation;
            }
            else if (panningFirstLocation != null)
            {
                PointF firstLocation = (PointF)panningFirstLocation;
                srcRect.X = srcRect.X + firstLocation.X - realLocation.X;
                srcRect.Y = srcRect.Y + firstLocation.Y - realLocation.Y;

                if (type is OperateType.VIEWER_PANNING_END)
                {
                    panningFirstLocation = null;
                }
            }
        }

        protected PointF ToRealLocationF(Point location)
        {
            return new PointF(srcRect.X + srcRect.Width *  location.X / dstRect.Width,
                              srcRect.Y + srcRect.Height * location.Y / dstRect.Height);
        }

        protected Point ToRealLocation(Point location)
        {
            return new Point((int)(srcRect.X + srcRect.Width * location.X / dstRect.Width),
                             (int)(srcRect.Y + srcRect.Height * location.Y / dstRect.Height));
        }

        protected Point ToWindowLocation(PointF location)
        {
            return new Point((int)((location.X - srcRect.X) * dstRect.Width / srcRect.Width),
                             (int)((location.Y - srcRect.Y) * dstRect.Height / srcRect.Height));
        }

        protected Rectangle ToWindowRect(RectangleF rect)
        {
            Point location = ToWindowLocation(rect.Location);
            Size size = new Size((int)(rect.Width * zoomFactor / 100), (int)(rect.Height * zoomFactor / 100));
            return new Rectangle(location, size);
        }

        protected virtual Image GetImage()
        {
            // Update srcRect
            srcRect.Width = dstRect.Width * 100 / zoomFactor;
            srcRect.Height = dstRect.Height * 100 / zoomFactor;
            srcRect.X = Math.Max(Math.Min(srcRect.X, srcImage.Width - srcRect.Width), 0);
            srcRect.Y = Math.Max(Math.Min(srcRect.Y, srcImage.Height - srcRect.Height), 0);

            // Update dstImage
            if (dstImage == null || preSrcRect != srcRect || preDstRect != dstRect)
            {
                if (dstImage != null)
                    dstImage.Dispose();
                dstImage = Paint.DrawImage(srcImage, srcRect, dstRect);
                preSrcRect = srcRect;
                preDstRect = dstRect;
            }

            return dstImage.Clone(new Rectangle(0, 0, dstImage.Width, dstImage.Height), dstImage.PixelFormat);
        }
    }
}
