using System;
using System.Drawing;

namespace ViewerLib
{
    // TODO: 1. Small Map
    // TODO: 2. Interface for ViewerKernel
    // TODO: 3. Cross 輔助線
    // TODO: 4. 效能、不重開 (new) 記憶體更新影像

    public class ViewerKernel
    {
        private enum ZoomFactorType { 
            ZOOM_FACTOR_TYPE_INC, 
            ZOOM_FACTOR_TYPE_DEC, 
            ZOOM_FACTOR_TYPE_FIT 
        };

        private float zoomFactor;
        private Bitmap srcImage = null;
        private Bitmap dstImage = null;
        private RectangleF srcRect;
        private Rectangle dstRect;
        private RectangleF? preSrcRect;
        private Rectangle? preDstRect;
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
                AdjustZoomFactor(ZoomFactorType.ZOOM_FACTOR_TYPE_FIT);
            }
        }
        public Image ZoomIn(Point location)
        {
            AdjustZoomFactor(ZoomFactorType.ZOOM_FACTOR_TYPE_INC);
            PointF realLocation = ToRealLocationF(location);
            srcRect.X = realLocation.X - dstRect.Width * 100 / zoomFactor * location.X / dstRect.Width;
            srcRect.Y = realLocation.Y - dstRect.Height * 100 / zoomFactor * location.Y / dstRect.Height;
            return image;
        }
        public Image ZoomOut(Point location)
        {
            AdjustZoomFactor(ZoomFactorType.ZOOM_FACTOR_TYPE_DEC);
            PointF realLocation = ToRealLocationF(location);
            srcRect.X = realLocation.X - dstRect.Width * 100 / zoomFactor * location.X / dstRect.Width;
            srcRect.Y = realLocation.Y - dstRect.Height * 100 / zoomFactor * location.Y / dstRect.Height;
            return image;
        }
        public Image Resize(Size size)
        {
            dstRect = new Rectangle(0, 0, size.Width, size.Height);
            return image;
        }
        public Image Panning(Point location, bool isFirst = false, bool isLast = false)
        {
            PointF realLocation = ToRealLocationF(location);

            if (isFirst)
            {
                panningFirstLocation = realLocation;
            }
            else if (panningFirstLocation != null)
            {
                PointF firstLocation = (PointF)panningFirstLocation;
                srcRect.X = srcRect.X + firstLocation.X - realLocation.X;
                srcRect.Y = srcRect.Y + firstLocation.Y - realLocation.Y;

                if (isLast)
                {
                    panningFirstLocation = null;
                }
            }

            return image;
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
        private void AdjustZoomFactor(ZoomFactorType zoomFactorType)
        {
            float minZoomFactor = Math.Max(1f / srcImage.Width * 100, 1f / srcImage.Width * 100);
            float maxZoomFactor = 6400;

            switch (zoomFactorType)
            {
                case ZoomFactorType.ZOOM_FACTOR_TYPE_INC:
                    zoomFactor *= 1.12f;
                    break;
                case ZoomFactorType.ZOOM_FACTOR_TYPE_DEC:
                    zoomFactor /= 1.12f;
                    break;
                case ZoomFactorType.ZOOM_FACTOR_TYPE_FIT:
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
        }
    }
}
