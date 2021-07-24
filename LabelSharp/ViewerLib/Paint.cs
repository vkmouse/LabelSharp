using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace ViewerLib
{
    public class Paint
    {
        private unsafe delegate void AssignPixel(byte* srcPixel, byte* dstPixel);
        public unsafe static Bitmap DrawImage(Bitmap srcImage, RectangleF srcRect, Rectangle dstRect)
        {
            // Create an image as destination
            Bitmap dstImage = new Bitmap(dstRect.Width, dstRect.Height, PixelFormat.Format32bppArgb);

            // Limitation of srcRect.Width 
            if (srcRect.Width >= srcImage.Width)
            {
                dstRect.Width = (int)Math.Round((double)srcImage.Width / srcRect.Width * dstRect.Width);
                srcRect.Width = srcImage.Width;
            }

            // Limitation of srcRect.Height 
            if (srcRect.Height >= srcImage.Height)
            {
                dstRect.Height = (int)Math.Round((double)srcImage.Height / srcRect.Height * dstRect.Height);
                srcRect.Height = srcImage.Height;
            }

            // Set assign pixel method
            AssignPixel assignPixel = null;
            int byteOfPixel = 0;
            Color[] palette = srcImage.Palette.Entries;
            switch (srcImage.PixelFormat)
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
                        for (int i = 0; i < 3; i++)
                            *(dstPixel + i) = *(srcPixel + i);
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

            // LockBits
            Rectangle rect = new Rectangle(0, 0, srcImage.Width, srcImage.Height);
            BitmapData srcBitmapData = srcImage.LockBits(rect, ImageLockMode.ReadOnly, srcImage.PixelFormat);
            BitmapData dstBitmapData = dstImage.LockBits(dstRect, ImageLockMode.WriteOnly, dstImage.PixelFormat);

            // Resize with nearest neighbor interpolation
            byte* src = (byte*)srcBitmapData.Scan0;
            byte* dst = (byte*)dstBitmapData.Scan0;
            int srcWidth = srcImage.Width;
            int srcHeight = srcImage.Height;
            Action<int> action = new Action<int>(dstY =>
            {
                int srcY = (int)((double)dstY / dstRect.Height * srcRect.Height + srcRect.Y);
                if (0 <= srcY && srcY < srcHeight)
                {
                    for (int dstX = 0; dstX < dstRect.Width; dstX++)
                    {
                        int srcX = (int)((double)dstX / dstRect.Width * srcRect.Width + srcRect.X);
                        if (0 <= srcX && srcX < srcWidth)
                        {
                            byte* srcPixel = src + srcY * srcBitmapData.Stride + srcX * byteOfPixel;
                            byte* dstPixel = dst + dstY * dstBitmapData.Stride + dstX * 4;
                            assignPixel(srcPixel, dstPixel);
                        }
                    }
                }
            });
            Parallel.For(0, dstRect.Height, action);

            // UnLockBits
            srcImage.UnlockBits(srcBitmapData);
            dstImage.UnlockBits(dstBitmapData);

            return dstImage;
        }
        public unsafe static void DrawTransparent(ref Bitmap inputOutputImage, Rectangle region, Color overlapColor, bool isInside = true)
        {
            BitmapData bmpData = inputOutputImage.LockBits(new Rectangle(0, 0, inputOutputImage.Width, inputOutputImage.Height), ImageLockMode.ReadWrite, inputOutputImage.PixelFormat);
            unsafe
            {
                byte* dst = (byte*)bmpData.Scan0;
                int A = 255 - overlapColor.A;
                int R = overlapColor.R * overlapColor.A;
                int G = overlapColor.G * overlapColor.A;
                int B = overlapColor.B * overlapColor.A;

                Action<int> action = new Action<int>(y =>
                {
                    for (int x = 0; x < bmpData.Width; x++)
                    {
                        if (region.Contains(new Point(x, y)) == isInside)
                        {
                            byte* dstPixel = dst + y * bmpData.Stride + x * 4;
                            int r = (*(dstPixel + 2) * A + R) / 255;
                            int g = (*(dstPixel + 1) * A + G) / 255;
                            int b = (*(dstPixel + 0) * A + B) / 255;
                            *(int*)dstPixel = Color.FromArgb(r, g, b).ToArgb();
                        }
                    }
                });
                Parallel.For(0, bmpData.Height, action);
            }
            inputOutputImage.UnlockBits(bmpData);
        }
    }
}
