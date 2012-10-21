using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Coding4Fun.Kinect.Common;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Research.Kinect.Nui;
using Point = System.Windows.Point;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Kinect_PointCloud
{
    public static class PointCloud
    {
        private static Single ToCentimetres(this Single s)
        {
            return s * 100;
        }

        public static List<Vector> ToPointCloud(this ImageFrame image, Runtime nui, int maxDepth)
        {
            // If there is no image, we cannot generate point cloud
            if (image == null)
                throw new ArgumentNullException("image");

            int width = image.Image.Width;
            int height = image.Image.Height;
            int greyIndex = 0;

            var points = new List<Vector>();

            // Process through each pixel in the field
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    short depth;
                    // The bits are processed differently if we are storing the player index with the depth
                    switch (image.Type)
                    {
                        case ImageType.DepthAndPlayerIndex:
                            depth = (short)((image.Image.Bits[greyIndex] >> 3) | (image.Image.Bits[greyIndex+1] << 5));
                            if (depth <= maxDepth)
                            {
                                points.Add(nui.SkeletonEngine.DepthImageToSkeleton(((float)x / image.Image.Width), ((float)y / image.Image.Height), (short) (depth << 3)));
                            }
                            break;
                        case ImageType.Depth:
                            depth = (short)((image.Image.Bits[greyIndex] | image.Image.Bits[greyIndex+1] << 8));
                            if (depth <= maxDepth)
                            {
                                points.Add(nui.SkeletonEngine.DepthImageToSkeleton(((float)(width - x - 1) / image.Image.Width), ((float)y / image.Image.Height), (short)(depth << 3)));
                            }
                            break;
                    }

                    greyIndex += 2;
                }
            }

            return points.Where(p => p.X != 0 || p.Y != 0 || p.Z != 0).ToList();
        }
        public static void Save(this List<Vector> points, string filename)
        {
            var ply = new StringBuilder();
            //var plyPoints = new StringBuilder();

            ply.AppendLine("ply");
            ply.AppendLine("format ascii 1.0");
            ply.AppendLine(String.Format("element vertex {0}", points.Count));
            ply.AppendLine("property float x\r\n" + "property float y\r\n" + "property float z\r\n" 
                + "property uchar red\r\n" + "property uchar green\r\n" + "property uchar blue\r\n" + "end_header");

            foreach (var point in points)
            {
                ply.AppendLine(String.Format("{0} {1} {2} 255 255 255", point.X.ToCentimetres(), point.Y.ToCentimetres(), point.Z.ToCentimetres()));
            }

            using (var filestream = new StreamWriter(filename))
            {
                filestream.Write(ply);
            }
        }
    }
}
