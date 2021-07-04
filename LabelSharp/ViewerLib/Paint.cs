﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace ViewerLib
{
    class Paint
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

            // LockBits
            Rectangle rect = new Rectangle(0, 0, srcImage.Width, srcImage.Height);
            BitmapData srcBitmapData = srcImage.LockBits(rect, ImageLockMode.ReadOnly, srcImage.PixelFormat);
            BitmapData dstBitmapData = dstImage.LockBits(dstRect, ImageLockMode.WriteOnly, dstImage.PixelFormat);

            // Resize with nearest neighbor interpolation
            byte* src = (byte*)srcBitmapData.Scan0;
            byte* dst = (byte*)dstBitmapData.Scan0;
            for (int dstY = 0; dstY < dstRect.Height; dstY++)
            {
                int srcY = (int)((double)dstY / dstRect.Height * srcRect.Height + srcRect.Y);
                for (int dstX = 0; dstX < dstRect.Width; dstX++)
                {
                    int srcX = (int)((double)dstX / dstRect.Width * srcRect.Width + srcRect.X);
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
        public unsafe static void DrawTransparent(ref Bitmap inputOutputImage, Rectangle region, byte alpha, bool isInside = true)
        {
            BitmapData bmpData = inputOutputImage.LockBits(new Rectangle(0, 0, inputOutputImage.Width, inputOutputImage.Height), ImageLockMode.ReadWrite, inputOutputImage.PixelFormat);
            unsafe
            {
                byte* dst = (byte*)bmpData.Scan0;
                for (int y = 0; y < bmpData.Height; y++)
                {
                    for (int x = 0; x < bmpData.Width; x++)
                    {
                        if (region.Contains(new Point(x, y)) == isInside)
                        {
                            byte* dstPixel = dst + y * bmpData.Stride + x * 4;
                            *(dstPixel + 3) = alpha;
                        }
                    }
                }
            }
            inputOutputImage.UnlockBits(bmpData);
        }
    }
}