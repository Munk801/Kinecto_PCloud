using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using System.IO;

namespace Kinect_PointCloud
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            depthText.Text = "Depth Value: " + depthValue.Value.ToString();
        }

        // Initialize NUI runtime
        Runtime nui;

        // Keep track of total frames and last frame for FPS
        int totalFrames = 0;
        int lastFrame = 0;
        DateTime lastTime = DateTime.MaxValue;

        ImageFrame depthImage;
        BitmapSource colorImage;
        BitmapSource dImage;
        byte[] depthFrame32 = new byte[320 * 240 * 4];
        const int RED_IDX = 2;
        const int GREEN_IDX = 1;
        const int BLUE_IDX = 0;
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //nui = new Runtime();
            nui = new Runtime(0);

            // Initialize Runtime.  If Kinect is not connected, throw exception
            try
            {
                nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime Init Failed.  Make sure Kinect is connected and working properly.");
                return;
            }

            // Initialize Video and Depth Streams.  Throw exception if image does not show
            try
            {
                nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution1280x1024, ImageType.Color);
                nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Failed to open streams.  Make sure Kinect is connected and image resolution is supported.");
                return;
            }

            // Calculate last time
            lastTime = DateTime.Now;

            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_ColorFrameReady);
        }

        void nui_ColorFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            //throw new NotImplementedException();
            videoStream.Source = e.ImageFrame.ToBitmapSource();
            PlanarImage myImage = e.ImageFrame.Image;
            BitmapSource myBitmapColor = BitmapSource.Create(myImage.Width, myImage.Height, 96, 96, PixelFormats.Bgr32, null, myImage.Bits, myImage.Width * myImage.BytesPerPixel);
            colorImage = myBitmapColor;
            depthText.Text = "Depth Value: " + depthValue.Value.ToString();
        }

        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            //throw new NotImplementedException();
            //depthStream.Source = e.ImageFrame.ToBitmapSource();
            PlanarImage Image = e.ImageFrame.Image;
            //depthImage = e.ImageFrame;
            byte[] convertedDepthFrame = convertDepthFrame(Image.Bits);
            depthStream.Source = BitmapSource.Create(Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, Image.Width * 4);
            dImage = BitmapSource.Create(Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, Image.Width * 4);
            totalFrames++;
            depthImage = e.ImageFrame;
            
            // Calculate the frame rate
            DateTime currentTime = DateTime.Now;
            if (currentTime.Subtract(lastTime) > TimeSpan.FromSeconds(1))
            {
                int fDiff = totalFrames - lastFrame;
                lastFrame = totalFrames;
                lastTime = currentTime;
                frameRate.Text = fDiff.ToString() + " FPS";
            }

        }

        private byte[] convertDepthFrame(byte[] depthFrame16)
        {
            for (int i16 = 0, i32 = 0; i16 < depthFrame16.Length && i32 < depthFrame32.Length; i16 += 2, i32 += 4)
            {
                int player = depthFrame16[i16] & 0x07;
                int realDepth = (depthFrame16[i16 + 1] << 5) | (depthFrame16[i16] >> 3);
                byte intensity = (byte)(255 - (255 * realDepth / 0x0fff));
                depthFrame32[i32 + RED_IDX] = 0;
                depthFrame32[i32 + GREEN_IDX] = 0;
                depthFrame32[i32 + BLUE_IDX] = 0;

                switch (player)
                {
                    case 0:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 2);
                        break;
                    case 1:
                        depthFrame32[i32 + RED_IDX] = intensity;
                        break;
                    case 2:
                        depthFrame32[i32 + GREEN_IDX] = intensity;
                        break;
                    case 3:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 4);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 4:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 4);
                        break;
                    case 5:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 4);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 6:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 7:
                        depthFrame32[i32 + RED_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(255 - intensity);
                        break;
                }
            }
            return depthFrame32;
        }

        private void tiltUp_Click(object sender, RoutedEventArgs e)
        {
            if (nui.NuiCamera.ElevationAngle >= 15)
                nui.NuiCamera.ElevationAngle = 20;

            else nui.NuiCamera.ElevationAngle += 5;
        }

        private void tiltDown_Click(object sender, RoutedEventArgs e)
        {
            if (nui.NuiCamera.ElevationAngle <= -15)
                nui.NuiCamera.ElevationAngle = -20;
            else nui.NuiCamera.ElevationAngle -= 5;
        }

        private void toPointCloud_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;
            this.Cursor = Cursors.Wait;

            depthImage.ToPointCloud(nui, (int)depthValue.Value).Save(@"C:\temp\PointCloud.ply");
            this.Cursor = cursor;
        }

        private void saveImage_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;
            this.Cursor = Cursors.Wait;

            FileStream streamColor = new FileStream(@"C:\temp\myImage.bmp", FileMode.Create);
            BmpBitmapEncoder encoderColor = new BmpBitmapEncoder();
            TextBlock myTextBlockColor = new TextBlock();
            myTextBlockColor.Text = "Codec Author is: Stephen" + encoderColor.CodecInfo.Author.ToString();
            encoderColor.Frames.Add(BitmapFrame.Create(colorImage));
            encoderColor.Save(streamColor);
            this.Cursor = cursor;
        }

        private void saveDImage_Click(object sender, RoutedEventArgs e)
        {
            var cursor = this.Cursor;
            this.Cursor = Cursors.Wait;

            FileStream streamColor = new FileStream(@"C:\temp\myDepthImage.bmp", FileMode.Create);
            BmpBitmapEncoder encoderColor = new BmpBitmapEncoder();
            TextBlock myTextBlockColor = new TextBlock();
            myTextBlockColor.Text = "Codec Author is: Stephen" + encoderColor.CodecInfo.Author.ToString();
            encoderColor.Frames.Add(BitmapFrame.Create(dImage));
            encoderColor.Save(streamColor);
            this.Cursor = cursor;
        }
   

    }
}
