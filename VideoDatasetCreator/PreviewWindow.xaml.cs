using Emgu.CV;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VideoDatasetCreator
{
    /// <summary>
    /// Interaction logic for PreviewWindow.xaml
    /// </summary>
    public partial class PreviewWindow : Window, INotifyPropertyChanged
    {
        private ImageSource previewImage;
        VideoCapture videoCapture;
        private int i = 0;
        private int framesCount;
        private int desiredResoluton;

        private DispatcherTimer timer
          = new DispatcherTimer(DispatcherPriority.Render);
        private List<Mat> previewFrames;

        public void feedFrames(List<Mat> inputFrames)
        {
            for (int i = 0; i < inputFrames.Count; i++)
            {
                PreviewImage = Utils.BitmapSourceConvert.ToBitmapSource(inputFrames[i]);
                canvasPreview.InvalidateVisual();
                Thread.Sleep(30);
            }
        }
        string videoFileName;
        int frameStart;
        List<System.Windows.Point> roiCoords;

        public void preview(string videoFileName, int frameStart, int framesCount, List<System.Windows.Point> roiCoords, int desiredResoluton)
        {
            previewFrames = new List<Mat>(framesCount);
            videoCapture = new VideoCapture(videoFileName);
            this.framesCount = framesCount;
            this.videoFileName = videoFileName;
            this.frameStart = frameStart;
            this.roiCoords = roiCoords;
            this.desiredResoluton = desiredResoluton;

            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.IsEnabled = true;
            timer.Start();
        }

        int tickVal = 0;
        void timer_Tick(object sender, EventArgs e)
        {
            videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, this.frameStart + tickVal);
            System.Drawing.Rectangle roi = new System.Drawing.Rectangle();
            roi.X = Math.Min((int)roiCoords[0].X, (int)roiCoords[1].X);
            roi.Y = Math.Min((int)roiCoords[0].Y, (int)roiCoords[1].Y);
            roi.Width = (int)Math.Abs(roiCoords[0].X - roiCoords[1].X);
            roi.Height = (int)Math.Abs(roiCoords[0].Y - roiCoords[1].Y);
            using (Mat framePreview = videoCapture.QueryFrame())
            {
                if (framePreview != null)
                {
                    Mat croppedImage = new Mat(framePreview, roi);
                    if (croppedImage.Width != desiredResoluton)
                        CvInvoke.Resize(croppedImage, croppedImage, new System.Drawing.Size(desiredResoluton, desiredResoluton), 0, 0, Emgu.CV.CvEnum.Inter.Lanczos4);

                    canvasPreview.Height = croppedImage.Height;
                    canvasPreview.Width = croppedImage.Width;
                    PreviewImage = Utils.BitmapSourceConvert.ToBitmapSource(croppedImage);

                }
            }

            //Mat framePreview = videoCapture.QueryFrame();
            




            //PreviewImage = Utils.BitmapSourceConvert.ToBitmapSource(previewFrames[tickVal]);
            //CvInvoke.Imshow("Preview", previewFrames[tickVal]);
            //CvInvoke.WaitKey(100);
            //OnPropertyChanged("PreviewImage");
            if (++tickVal >= framesCount)
            {
                timer.Stop();
                tickVal = 0;
                videoCapture = null;
                //videoCapture.Dispose();
                GC.Collect();
                this.Close();

            }
        }

        public ImageSource PreviewImage
        {
            get { return previewImage; }
            set
            {
                previewImage = value;
                OnPropertyChanged("PreviewImage");
            }
        }

        public PreviewWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
