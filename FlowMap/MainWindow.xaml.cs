using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Kinect;
using Noise_Removal;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math;

namespace FlowMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Map hmap;
        private KinectSensor _kinect;
        private WriteableBitmap _colorbmp;
        private Int32Rect _colorbmprect;
        private int _colorStride;
        private Noise_Removal.Remover noise_remover;
        private short[] pData;
        private int width;
        private int height;
        private int bytesperpixel;
        private int fCount = 0;

        public KinectSensor Kinect
        {
            get { return this._kinect; }
            set
            {
                if (this._kinect != value)
                {
                    if (this._kinect != null)
                    {
                        UninitializeSensor(this._kinect);
                    }
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this._kinect = value;
                        InitializeSensor(this._kinect);
                    }
                }
            }
        }

        private void InitializeSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {

                //display ColorStream
                //ColorImageStream cStream = kinectSensor.ColorStream;
                //cStream.Enable();
                //this._colorbmp = new WriteableBitmap(cStream.FrameWidth, cStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                //this._colorbmprect = new Int32Rect(0, 0, cStream.FrameWidth, cStream.FrameHeight);
                //this._colorStride = cStream.FrameWidth * cStream.FrameBytesPerPixel;
                //sensorImage.Source = this._colorbmp;
                //kinectSensor.ColorFrameReady += kinect_ColorFrameReady;
                //display DepthStream
                this._kinect.DepthStream.Enable();
                this._kinect.DepthFrameReady += kinect_DepthFrameReady;
                kinectSensor.Start();
            }
        }

        private void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame f = e.OpenDepthImageFrame())
            {
                if (f != null)
                {
                    this.fCount++;
                    this.bytesperpixel = f.BytesPerPixel;
                    this.width = f.Width;
                    this.height = f.Height;
                    if (this.pData == null) 
                        pData = new short[f.PixelDataLength];
                    BitmapSource bmps = this.hmap.RenderFlowMap(f, 90);
                    //Layer.Source = bmps;
                    sensorLayer.Source = bmps;
                    
                    f.CopyPixelDataTo(this.pData);
                    this.pData = this.noise_remover.applyPixelFilter(this.pData, f.Width, f.Height);
                    depthImage.Source = this.hmap.detectPersonBlob(this.pData, 95, this.width, this.height);
                    //depthImage.Source = this.noise_remover.BitmapSourceFromBitmap(this.hmap.findPaths());
                      //Bitmap bitmap = this.hmap.findPaths();
                      //Layer.Source = this.noise_remover.BitmapSourceFromBitmap(bitmap);
                     // bitmap.Dispose();

                }
            }
        }

    

        //private void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        //{
        //    using (ColorImageFrame f = e.OpenColorImageFrame())
        //    {
        //        if (f != null)
        //        {
        //            byte[] pixelData = new byte[f.PixelDataLength];
        //            f.CopyPixelDataTo(pixelData);

        //            this._colorbmp.WritePixels(this._colorbmprect, pixelData, this._colorStride, 0);
        //        }
        //    }
        //}

        private void UninitializeSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                kinectSensor.Stop();
               // kinectSensor.ColorFrameReady -= kinect_ColorFrameReady;
                
            }
        }

        //constructor for the main window
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Unloaded += (s, e) => { this.Kinect = null; };
            this.noise_remover = new Remover();
            double w = mw.Width;
            double h = mw.Height;
            this.hmap = new Map((int)w, (int) h, 30);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.Kinect == null)
                    {
                        this.Kinect = e.Sensor;
                    }
                    break;
                
                case KinectStatus.Disconnected:
                    if (this.Kinect == e.Sensor)
                    {
                        this.Kinect = null;
                        this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        if (this.Kinect == null)
                        {
                            Console.WriteLine("Sensor Disconnected");
                        }
                    }
                    break;
            }
        }



        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int stride = this.width * this.bytesperpixel;
            blobLayer.Source = this.hmap.detectSpaceBlobs(this.pData, 60, this.width, this.height);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            pathLayer.Source = this.hmap.findPaths();
            return;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.hmap.resetCounts();
            return;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            this.hmap.clearMapPaths();
            return;
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var s = sender as Slider;
            this.hmap.setSpaceDetectorThreshold((int)s.Value);
            return;
        }
    }
}
