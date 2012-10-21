// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Coding4Fun.Kinect.Common;
using Coding4Fun.Kinect.Wpf.Geometry;
using Microsoft.Research.Kinect.Nui;
using Point = System.Windows.Point;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

namespace Coding4Fun.Kinect.Wpf
{
    public static class ImageFrameExtensions
    {
        public static short[][] ToDepthArray2D(this ImageFrame image)
        {
            return ImageFrameCommonExtensions.ToDepthArray2D(image);
        }

        public static short[] ToDepthArray(this ImageFrame image)
        {
            return ImageFrameCommonExtensions.ToDepthArray(image);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "x"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "y")]
        public static short GetDistance(this ImageFrame image, int x, int y)
        {
            return ImageFrameCommonExtensions.GetDistance(image, x, y);
        }

        public static Point GetMidpoint(this short[] depthData, int width, int height, int startX, int startY, int endX, int endY, int minimumDistance)
        {
            double x;
            double y;
            depthData.GetMidpoint(width, height, startX, startY, endX, endY, minimumDistance, out x, out y);

            return new Point(x, y);
        }

        public static BitmapSource ToBitmapSource(this short[] depthData, int width, int height, int minimumDistance, Color highlightColor)
        {
            if (depthData != null)
            {
                var colorFrame = new byte[height * width * 4];

                for (int colorIndex = 0, depthIndex = 0; colorIndex < colorFrame.Length; colorIndex += 4, depthIndex++)
                {
                    var intensity = ImageFrameCommonExtensions.CalculateIntensityFromDepth(depthData[depthIndex]);

                    colorFrame[colorIndex + ImageFrameCommonExtensions.RedIndex] = intensity;
                    colorFrame[colorIndex + ImageFrameCommonExtensions.GreenIndex] = intensity;
                    colorFrame[colorIndex + ImageFrameCommonExtensions.BlueIndex] = intensity;

                    if (depthData[depthIndex] <= minimumDistance && depthData[depthIndex] > 0)
                    {
                        var color = Color.Multiply(highlightColor, intensity / 255f);

                        colorFrame[colorIndex + ImageFrameCommonExtensions.RedIndex] = color.R;
                        colorFrame[colorIndex + ImageFrameCommonExtensions.GreenIndex] = color.G;
                        colorFrame[colorIndex + ImageFrameCommonExtensions.BlueIndex] = color.B;
                    }
                }

                return colorFrame.ToBitmapSource(width, height);
            }

            return null;
        }

        public static BitmapSource ToBitmapSource(this ImageFrame image)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            switch (image.Type)
            {
                case ImageType.Color:
                    {
                        return image.Image.Bits.ToBitmapSource(image.Image.Width, image.Image.Height);
                    }
                case ImageType.Depth:
                    {
                        return ImageFrameCommonExtensions.ConvertDepthFrameDataToBitmapData(image.Image.Bits, image.Image.Width, image.Image.Height).ToBitmapSource(image.Image.Width, image.Image.Height);
                    }
                case ImageType.DepthAndPlayerIndex:
                    {
                        return
                            ImageFrameCommonExtensions.ConvertDepthFrameDataWithSkeletonToBitmapData(image.Image.Bits, image.Image.Width, image.Image.Height).ToBitmapSource(image.Image.Width, image.Image.Height);
                    }
                default:
                    return null;
            }
        }
    }
}
