using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace ViewerLib
{
    public class Paint
    {
        private unsafe delegate void AssignPixel(byte* srcPixel, byte* dstPixel);
        public unsafe static void DrawImage(Bitmap srcImage, RectangleF srcRect, Rectangle dstRect, ref Bitmap dstImage)
        {
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
            float srcRectX = srcRect.X;
            float srcRectY = srcRect.Y;
            int srcWidth = srcImage.Width;
            int srcHeight = srcImage.Height;
            float ratioX = srcRect.Width / dstRect.Width;
            float ratioY = srcRect.Height / dstRect.Height;
            Action<int> action = new Action<int>(dstY =>
            {
                int srcY = (int)(dstY * ratioY + srcRectY);
                byte* srcOffset = src + srcY * srcBitmapData.Stride;
                byte* dstOffset = dst + dstY * dstBitmapData.Stride;
                if (!(0 <= srcY && srcY < srcHeight))
                    return;
                for (int dstX = 0; dstX < dstRect.Width; dstX++)
                {
                    int srcX = (int)(dstX * ratioX + srcRectX);
                    if (!(0 <= srcX && srcX < srcWidth))
                        continue;
                    byte* srcPixel = srcOffset + srcX * byteOfPixel;
                    byte* dstPixel = dstOffset + dstX * 4;
                    assignPixel(srcPixel, dstPixel);
                }
            });
            Parallel.For(0, dstRect.Height, action);

            // UnLockBits
            srcImage.UnlockBits(srcBitmapData);
            dstImage.UnlockBits(dstBitmapData);
        }
        public unsafe static void DrawTransparent(ref Bitmap inputOutputImage, Rectangle region, Color overlapColor, bool isInside = true)
        {
            BitmapData bmpData = inputOutputImage.LockBits(new Rectangle(0, 0, inputOutputImage.Width, inputOutputImage.Height), ImageLockMode.ReadWrite, inputOutputImage.PixelFormat);
            int xmin = region.Left;
            int ymin = region.Top;
            int xmax = region.Right;
            int ymax = region.Bottom;
            float A = 1 - overlapColor.A / 255f;
            float R = overlapColor.R * overlapColor.A / 255f;
            float G = overlapColor.G * overlapColor.A / 255f;
            float B = overlapColor.B * overlapColor.A / 255f;
            unsafe
            {
                byte* dst = (byte*)bmpData.Scan0;

                Action<int> action = new Action<int>(y =>
                {
                    byte* offset = dst + y * bmpData.Stride;
                    if ((ymin <= y && y < ymax) != isInside)
                        return;
                    for (int x = 0; x < bmpData.Width; x++)
                    {
                        if ((xmin <= x && x < xmax) == isInside)
                        {
                            byte* dstPixel = offset + x * 4;
                            *(dstPixel + 0) = (byte)(*(dstPixel + 0) * A + B);
                            *(dstPixel + 1) = (byte)(*(dstPixel + 1) * A + G);
                            *(dstPixel + 2) = (byte)(*(dstPixel + 2) * A + R);
                        }
                    }
                });
                Parallel.For(0, bmpData.Height, action);
            }
            inputOutputImage.UnlockBits(bmpData);
        }
        public unsafe static void Clean(ref Bitmap inputOutputImage, Rectangle rectangle)
        {
            BitmapData bmpData = inputOutputImage.LockBits(rectangle, ImageLockMode.ReadWrite, inputOutputImage.PixelFormat);
            byte[] zeros = new byte[bmpData.Stride];
            int stride = bmpData.Stride;
            unsafe
            {
                byte* dst = (byte*)bmpData.Scan0;
                for (int y = 0; y < bmpData.Height; y++)
                {
                    byte* offset = dst + y * stride;
                    fixed (byte* ptr = zeros)
                    {
                        Buffer.MemoryCopy(ptr, dst + y * stride, stride, stride);
                    }
                }
            }
            inputOutputImage.UnlockBits(bmpData);
        }
        public unsafe static void CopyTo(Bitmap srcImage, ref Bitmap dstImage, Rectangle rectangle)
        {
            int width = rectangle.Width;
            int height = rectangle.Height;
            if (dstImage.Width < width || dstImage.Height < height || srcImage.PixelFormat != dstImage.PixelFormat)
            {
                dstImage = new Bitmap(width, height, srcImage.PixelFormat);
            }

            // LockBits
            BitmapData srcBitmapData = srcImage.LockBits(rectangle, ImageLockMode.ReadOnly, srcImage.PixelFormat);
            BitmapData dstBitmapData = dstImage.LockBits(rectangle, ImageLockMode.WriteOnly, dstImage.PixelFormat);

            // Resize with nearest neighbor interpolation
            byte* src = (byte*)srcBitmapData.Scan0;
            byte* dst = (byte*)dstBitmapData.Scan0;
            for (int y = 0; y < height; y++)
            {
                Buffer.MemoryCopy(src + y * srcBitmapData.Stride,
                                  dst + y * dstBitmapData.Stride, 
                                  dstBitmapData.Stride, 
                                  srcBitmapData.Stride);
            }

            // UnLockBits
            srcImage.UnlockBits(srcBitmapData);
            dstImage.UnlockBits(dstBitmapData);
        }
    }
}
