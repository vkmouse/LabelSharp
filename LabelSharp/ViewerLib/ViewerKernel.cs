using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ViewerLib
{
    // TODO: Interface IDetectionLabeling, DetectionLabeling, UML, Small Map, Github

    public class ViewerKernel
    {
        private Bitmap srcImage = null;
        private Bitmap dstImage = null;
        private Rectangle srcRect;
        private Rectangle dstRect;
        private int zoomFactor;
        private Point panningFirstLocation;

        public ViewerKernel(Size size)
        {
            dstRect = new Rectangle(0, 0, size.Width, size.Height);
        }
        public Image image
        {
            get
            {
                // zoomFactor
                zoomFactor = Math.Max(zoomFactor, 1);

                // srcRect
                srcRect.Width = dstRect.Width * 100 / zoomFactor;
                srcRect.Height = dstRect.Height * 100 / zoomFactor;
                srcRect = RepairSrcRect(srcRect, srcImage.Size, dstRect.Size);

                // dstImage
                if (dstImage != null)
                    dstImage.Dispose();
                dstImage = DrawImage(srcImage, srcRect, dstRect);

                return dstImage;
            }
            set
            {
                // srcImage
                if (srcImage != null)
                    srcImage.Dispose();
                srcImage = value as Bitmap;

                // zoomFactor
                ZoomToFit();
            }
        }
        public Image ZoomIn(Point location)
        {
            Point realLocation = ToRealLocation(location);
            srcRect.X = (realLocation.X + srcRect.X) / 2;
            srcRect.Y = (realLocation.Y + srcRect.Y) / 2;
            zoomFactor *= 2;
            return image;
        }
        public Image ZoomOut(Point location)
        {
            Point realLocation = ToRealLocation(location);
            srcRect.X = srcRect.X * 2 - realLocation.X;
            srcRect.Y = srcRect.Y * 2 - realLocation.Y;
            zoomFactor /= 2;
            return image;
        }
        public Image Resize(Size size)
        {
            dstRect = new Rectangle(0, 0, size.Width, size.Height);
            return image;
        }
        public Image Panning(Point location, bool isFirst = false)
        {
            Point realLocation = ToRealLocation(location);

            if (isFirst)
            {
                panningFirstLocation = realLocation;
            }
            else
            {
                srcRect.X = srcRect.X + panningFirstLocation.X - realLocation.X;
                srcRect.Y = srcRect.Y + panningFirstLocation.Y - realLocation.Y;
            }

            return image;
        }

        private void ZoomToFit()
        {
            double zoomFactorX = dstRect.Width / (double)srcImage.Width;
            double zoomFactorY = dstRect.Height / (double)srcImage.Height;
            zoomFactor = zoomFactorX > zoomFactorY ? (int)(zoomFactorY * 100) : (int)(zoomFactorX * 100);
        }
        private Point ToRealLocation(Point location)
        {
            return new Point((int)Math.Round(srcRect.X + (double)srcRect.Width * location.X / dstRect.Width),
                             (int)Math.Round(srcRect.Y + (double)srcRect.Height * location.Y / dstRect.Height));
        }
        
        private unsafe delegate void AssignPixel(byte* srcPixel, byte* dstPixel);
        private unsafe static Bitmap DrawImage(Bitmap srcImage, Rectangle srcRect, Rectangle dstRect)
        {
            // LockBits
            Bitmap dstImage = new Bitmap(dstRect.Width, dstRect.Height, PixelFormat.Format32bppArgb);

            if (srcRect.Width >= srcImage.Width)
            {
                dstRect.Width = (int)Math.Round((double)srcImage.Width / srcRect.Width * dstRect.Width);
                srcRect.Width = srcImage.Width;
            }

            if (srcRect.Height >= srcImage.Height)
            {
                dstRect.Height = (int)Math.Round((double)srcImage.Height / srcRect.Height * dstRect.Height);
                srcRect.Height = srcImage.Height;
            }

            BitmapData srcBitmapData = srcImage.LockBits(srcRect, ImageLockMode.ReadOnly, srcImage.PixelFormat);
            BitmapData dstBitmapData = dstImage.LockBits(dstRect, ImageLockMode.WriteOnly, dstImage.PixelFormat);
            AssignPixel assignPixel = null;

            int byteOfPixel = 0;
            Color[] palette = srcImage.Palette.Entries;
            switch (srcBitmapData.PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    byteOfPixel = 1;
                    assignPixel = delegate (byte* srcPixel, byte* dstPixel)
                    {
                        *(int*)dstPixel = palette[*srcPixel].ToArgb();
                    };
                    break;

                case PixelFormat.Format24bppRgb:
                    byteOfPixel = 3;
                    assignPixel = delegate (byte* srcPixel, byte* dstPixel)
                    {
                        *(int*)dstPixel = *(int*)srcPixel;
                        *(dstPixel + 3) = 255;
                    };
                    break;

                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                    byteOfPixel = 4;
                    assignPixel = delegate (byte* srcPixel, byte* dstPixel)
                    {
                        *(int*)dstPixel = *(int*)srcPixel;
                    };
                    break;
            }

            // Resize with nearest neighbor interpolation
            byte* src = (byte*)srcBitmapData.Scan0;
            byte* dst = (byte*)dstBitmapData.Scan0;

            for (int dstY = 0; dstY < dstBitmapData.Height; dstY++)
            {
                int srcY = (int)((double)dstY / dstBitmapData.Height * srcBitmapData.Height);
                for (int dstX = 0; dstX < dstBitmapData.Width; dstX++)
                 {
                    int srcX = (int)((double)dstX / dstBitmapData.Width * srcBitmapData.Width);
                    byte* srcPixel = src + srcY * srcBitmapData.Stride + srcX * byteOfPixel;
                    byte* dstPixel = dst + dstY * dstBitmapData.Stride + dstX * 4;
                    assignPixel(srcPixel, dstPixel);
                }
            }

            // UnLockBits
            srcImage.UnlockBits(srcBitmapData);
            dstImage.UnlockBits(dstBitmapData);

            return dstImage;
        }
        private static Rectangle RepairSrcRect(Rectangle srcRect, Size srcSize, Size dstSize)
        {
            int minZoomFactor = 1;

            srcRect.Width = Math.Min(Math.Max(srcRect.Width, 1), dstSize.Width * 100 / minZoomFactor);
            srcRect.Height = Math.Min(Math.Max(srcRect.Height, 1), dstSize.Height * 100 / minZoomFactor);

            double scale = Math.Min((double)dstSize.Width / srcRect.Width, (double)dstSize.Height / srcRect.Height);
            srcRect.Width = (int)Math.Round(dstSize.Width / scale);
            srcRect.Height = (int)Math.Round(dstSize.Height / scale);

            srcRect.X = Math.Max(Math.Min(srcRect.X, srcSize.Width - srcRect.Width), 0);
            srcRect.Y = Math.Max(Math.Min(srcRect.Y, srcSize.Height - srcRect.Height), 0);

            return srcRect;
        }
    }
}
