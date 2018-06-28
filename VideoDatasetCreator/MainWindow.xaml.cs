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
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.Util;
using Microsoft.Win32;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.IO;
using System.Xml;

namespace VideoDatasetCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ImageViewer viewer = new ImageViewer();
        List<Mat> outputFrames = new List<Mat>();
        VideoCapture videoCapture;
        //private int currentFramePos = 1;
        int previewFramesShown = 0;
        Mat originalFrame;
        Mat framePretty;
        List<System.Windows.Point> roiCoords;
        ImageWindow imgWindow;
        DynamicData parameters;
        Style buttonDefaultStyle;
        Style elementGoodStyle;
        Style elementBadStyle;


        #region Properties

        #endregion

        public MainWindow()
        {
            InitializeComponent();
      
            parameters = new DynamicData();
            this.DataContext = parameters;
            parameters.OutputSequenceName = Properties.Settings.Default.OutputSequenceName;
            parameters.NumFrames = Properties.Settings.Default.NumFrames;
            parameters.Resolution = Properties.Settings.Default.Resolution;
            parameters.IsAutoIncrement = Properties.Settings.Default.IsAutoIncrement;
            parameters.OutputDatasetFolder = Properties.Settings.Default.OutputDatasetFolder;
            parameters.CurrentFramePos = 0;
            parameters.ClassName = Properties.Settings.Default.ClassName;
            initializeStyles();
            initializeElementsStyles();

            EventManager.RegisterClassHandler(typeof(Window),
     Keyboard.KeyDownEvent, new System.Windows.Input.KeyEventHandler(pressedKey), true);

            initializeImageWindow();

           

            roiCoords = new List<System.Windows.Point>(2);
            roiCoords.Add(new System.Windows.Point(0, 0));
            roiCoords.Add(new System.Windows.Point(0, 0));

            this.Closing += new System.ComponentModel.CancelEventHandler(Window_Closing);

            bNextFrame.Content = ">";
            bPreviousFrame.Content = "<";
            bSkipPrevious.Content = "<<100";
            bSkipNext.Content = ">>100";


        }



        private void initializeElementsStyles()
        {
            bSkipPrevious.Style = buttonDefaultStyle;
            bPreviousFrame.Style = buttonDefaultStyle;
            bNextFrame.Style = buttonDefaultStyle;
            bSkipNext.Style = buttonDefaultStyle;
            bPreviewOutput.Style = buttonDefaultStyle;
            bGenerateOutput.Style = buttonDefaultStyle;

            updateElementsStyles();
        }

        private void updateElementsStyles()
        {
            btnOpenVideo.Style = (parameters.InputFilePath == null || parameters.InputFilePath == "") ? elementBadStyle : elementGoodStyle;
            lblInputVideo.Style = (parameters.InputFilePath == null || parameters.InputFilePath == "") ? elementBadStyle : elementGoodStyle;

            btnChooseOutputFolder.Style = (parameters.OutputDatasetFolder == null || parameters.OutputDatasetFolder == "") ? elementBadStyle : elementGoodStyle;
            lblDatasetFolder.Style = (parameters.OutputDatasetFolder == null || parameters.OutputDatasetFolder == "") ? elementBadStyle : elementGoodStyle;

            tbSequenceName.Style = (parameters.OutputSequenceName == null || parameters.OutputDatasetFolder == "") ? elementBadStyle : elementGoodStyle;

            tbNumFrames.Style = (parameters.NumFrames < 1 || parameters.NumFrames > 100000) ? elementBadStyle : elementGoodStyle;

            tbResolution.Style = (parameters.Resolution < 1) ? elementBadStyle : elementGoodStyle;


        }

        private void initializeStyles()
        {
            buttonDefaultStyle = (Style)System.Windows.Application.Current.FindResource("ButtonDefaultStyle");
            elementBadStyle = (Style)System.Windows.Application.Current.FindResource("ButtonBadStyle");
            elementGoodStyle = (Style)System.Windows.Application.Current.FindResource("ButtonGoodStyle");
        }

        private void initializeImageWindow()
        {
            if (imgWindow != null)
            {
                imgWindow.enableClosing();
                imgWindow.Close();
            }
                
            imgWindow = new ImageWindow();
            imgWindow.canvasMain.MouseDown += new System.Windows.Input.MouseButtonEventHandler(image_MouseDown);
            imgWindow.imageMain.MouseMove += new System.Windows.Input.MouseEventHandler(this.image_MouseMove);
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if (Utils.IsWindowOpen<Window>("ImageWindow"))
            try
            {
                imgWindow.enableClosing();
                imgWindow.Close();
            }
            catch (Exception ex)
            {
            }

            Properties.Settings.Default.OutputSequenceName = parameters.OutputSequenceName;
            Properties.Settings.Default.NumFrames = parameters.NumFrames;
            Properties.Settings.Default.Resolution = parameters.Resolution;
            Properties.Settings.Default.IsAutoIncrement = parameters.IsAutoIncrement;
            Properties.Settings.Default.OutputDatasetFolder = parameters.OutputDatasetFolder;
            Properties.Settings.Default.Save();
        }

        void image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Update the mouse path with the mouse information
            System.Windows.Point mouseDownLocation = e.GetPosition(imgWindow.canvasMain);
            //System.Windows.Forms.Control src = e.Source as System.Windows.Forms.Control;

            
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    roiCoords[0] = mouseDownLocation;
                    roiCoords[1] = new System.Windows.Point(0, 0);
                    break;
                case MouseButton.Right:
                    if (roiCoords[0].X != 0 && roiCoords[0].Y != 0)
                    {
                        roiCoords[1] = mouseDownLocation;
                        roiCoords = adjustCoorinatesByResolution(roiCoords);
                        makeFramePretty(roiCoords);
                    }
                        break;
                default:
                    break;
            }
            
        }

        private List<Point> adjustCoorinatesByResolution(List<Point> inputRoi)
        {
            List<Point> newRoi = new List<Point>(2);
            newRoi.Add(inputRoi[0]);
            int newX = 0;
            int newY = 0;
            if (Math.Abs(inputRoi[0].X - inputRoi[1].X) < parameters.Resolution)
            {
                
                //newRoi.Add(inputRoi[0]);
                if (inputRoi[0].X < inputRoi[1].X)
                    newX = (int)newRoi[0].X + parameters.Resolution;
                else
                    newX = (int)newRoi[0].X - parameters.Resolution;
                if (inputRoi[0].Y < inputRoi[1].Y)
                    newY = (int)newRoi[0].Y + parameters.Resolution;
                else
                    newY = (int)newRoi[0].Y - parameters.Resolution;
                newRoi.Add(new Point(newX, newY));
            }
            else
            {
                newX = (int)inputRoi[1].X;
                if (inputRoi[0].Y < inputRoi[1].Y)
                    newY = (int)(inputRoi[0].Y + Math.Abs(inputRoi[1].X - inputRoi[0].X));
                else
                    newY = (int)(inputRoi[0].Y - Math.Abs(inputRoi[1].X - inputRoi[0].X));
                newRoi.Add(new Point(newX, newY));
            }
            return newRoi;
        }

        private void image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Update the mouse path with the mouse information
            System.Windows.Point mouseMoveLocation = e.GetPosition(imgWindow.canvasMain);
            //System.Drawing.Point mouseMoveLocation = new System.Drawing.Point(e.X, e.Y);
            if (roiCoords[0].X != 0 && roiCoords[0].Y != 0 && roiCoords[1].X == 0 && roiCoords[1].Y == 0)
            {
                System.Windows.Point p0 = roiCoords[0];
                System.Windows.Point p1 = mouseMoveLocation;
                List<Point> roi = new List<Point>(2);
                roi.Add(p0);
                roi.Add(p1);
                roi = adjustCoorinatesByResolution(roi);

                makeFramePretty(roi);
            }

        }

        private void pressedKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((e.Key == Key.Up))
            {
                skipFrames(1);
                return;
            }
            else if ((e.Key == Key.Down))
            {
                skipFrames(-1);
                return;
            }
            else if ((e.Key == Key.Right))
            {
                skipFrames(100);
                return;
            }
            else if ((e.Key == Key.Left))
            {
                skipFrames(-100);
                return;
            }
        }

        

    private void showImage(Mat image)
        {
            if (imgWindow != null && imgWindow.Visibility != Visibility.Visible)
            {
                Screen[] screens = Screen.AllScreens;
                if (screens.Length > 1)
                {
                    //imgWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    WindowExt.MaximizeToSecondaryMonitor(imgWindow);
                }
                imgWindow.Show();
            }
            //if (!Utils.IsWindowOpen<ImageWindow>("ImageWindow"))
               
            imgWindow.canvasMain.Width = originalFrame.Width;
            imgWindow.canvasMain.Height = originalFrame.Height;
            imgWindow.imageMain.Source = Utils.BitmapSourceConvert.ToBitmapSource(image);
        }

        //private void showPreview()
        //{
        //    previewFramesShown = 0;
        //    if (prvWindow != null && prvWindow.Visibility != Visibility.Visible)
        //    {
        //        prvWindow.Show();
        //    }
        //    List<Mat> temp = new List<Mat>(parameters.NumFrames);
        //    for (int i = 0; i < parameters.NumFrames; i++)
        //    {
        //        System.Drawing.Rectangle roi = new System.Drawing.Rectangle();
        //        roi.X = (int)roiCoords[0].X;
        //        roi.Y = (int)roiCoords[0].Y;
        //        roi.Width = (int)Math.Abs(roiCoords[0].X - roiCoords[1].X);
        //        roi.Height = (int)Math.Abs(roiCoords[0].Y - roiCoords[1].Y);

        //        videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, currentFramePos + previewFramesShown++);

        //        Mat framePreview = videoCapture.QueryFrame();
        //        if (framePreview != null)
        //        {
        //            Mat croppedImage = new Mat(framePreview, roi);
        //            temp.Add(croppedImage);
        //        }
        //        prvWindow.feedFrames(temp);
        //    }
        //}

        private void initializeParameters()
        {
            parameters.CurrentFramePos = 0;
            roiCoords[0] = new Point(0,0);
            roiCoords[1] = new Point(0, 0);
            initializeImageWindow();

        }

        private void btnOpenVideo_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                initializeParameters();
                lblInputVideo.Content = openFileDialog.FileName;
                // Cache the video into the memory
                videoCapture = new VideoCapture(lblInputVideo.Content.ToString());
                double numFrames = videoCapture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
                videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, parameters.CurrentFramePos);
                originalFrame = videoCapture.QueryFrame();
                showImage(originalFrame);
            }
            updateElementsStyles();
        }

        private void btnChooseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    //string[] files = Directory.GetFiles(fbd.SelectedPath);
                    lblDatasetFolder.Content = fbd.SelectedPath;
                }
            }
            updateElementsStyles();
        }

        private void bNextFrame_Click(object sender, RoutedEventArgs e)
        {
            skipFrames(1);
        }

        private void bPreviousFrame_Click(object sender, RoutedEventArgs e)
        {
            skipFrames(-1);
        }

        private void bSkipPrevious_Click(object sender, RoutedEventArgs e)
        {
            skipFrames(-100);
        }

        private void bSkipNext_Click(object sender, RoutedEventArgs e)
        {
            skipFrames(100);
        }

        private void skipFrames(int delta)
        {
            parameters.CurrentFramePos += delta;
            if (parameters.CurrentFramePos < 1)
                parameters.CurrentFramePos = 1;
            videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, parameters.CurrentFramePos);
            Mat frame = videoCapture.QueryFrame();
            if (frame != null)
            {
                originalFrame = frame.Clone();
                makeFramePretty(roiCoords);
                showImage(framePretty);
            }
        }

        private void makeFramePretty(List<Point> roi)
        {
            framePretty = originalFrame.Clone();
            if (!roiChosen())
            {
                System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                rect.Stroke = new SolidColorBrush(Colors.Green);
                //rect.Fill = new SolidColorBrush(Colors.Black);
                rect.Width = Math.Abs(roi[0].X - roi[1].X);
                rect.Height = Math.Abs(roi[0].Y - roi[1].Y);
                rect.StrokeThickness = 1;
                Canvas.SetZIndex(rect, 1);
                Canvas.SetLeft(rect, Math.Min(roi[0].X, roi[1].X));
                Canvas.SetTop(rect, Math.Min(roi[0].Y, roi[1].Y));
                int num = imgWindow.canvasMain.Children.Count;
                if (num > 1)
                {
                    imgWindow.canvasMain.Children.RemoveAt(1);
                }
                imgWindow.canvasMain.Children.Add(rect);
            }
        }

  
        Thread previewThread;
        private void bPreviewOutput_Click(object sender, RoutedEventArgs e)
        {
            if (parameters.InputFilePath == null)
            {
                System.Windows.MessageBox.Show("You have not chosen the input video file");
                //bPreviewOutput.IsEnabled = false;
                return;
            }
            previewThread = new Thread(delegate ()
            {
                
                PreviewWindow prvWindow = new PreviewWindow();
                prvWindow.Show();
                prvWindow.preview(parameters.InputFilePath, parameters.CurrentFramePos, parameters.NumFrames, roiCoords, parameters.Resolution);
                System.Windows.Threading.Dispatcher.Run();
            });
            previewThread.IsBackground = true;
            previewThread.SetApartmentState(ApartmentState.STA); // needs to be STA or throws exception
            previewThread.Start();
        }

        private bool roiChosen()
        {
            return (roiCoords[0].X != 0 && roiCoords[0].Y != 0 && roiCoords[1].X != 0 && roiCoords[1].Y != 0);
        }

        private bool checksGenerateOutput()
        {
            if (parameters.InputFilePath == null)
            {
                System.Windows.MessageBox.Show("You have not chosen the input video file");
                return false;
            }
            if (!roiChosen())
            {
                System.Windows.MessageBox.Show("You have not chosen the cropping region\nTo choose it:\n- Left click to start drawing\n- Right click to finish drawing");
                return false;
            }
            if (parameters.NumFrames < 1 || parameters.NumFrames > 100000)
            {
                System.Windows.MessageBox.Show("Number of frames if not in the range [1,100000]");
                return false;
            }
            if (parameters.OutputDatasetFolder == null)
            {
                System.Windows.MessageBox.Show("Where to save the sequence? Please choose the dataset folder");
                return false;
            }
            if (parameters.Resolution < 1 || parameters.Resolution > Math.Min(originalFrame.Width, originalFrame.Height))
            {
                System.Windows.MessageBox.Show("Cropping region is larger than input video dimensions");
                return false;
            }
            return true;
        }

        private string generateSequenceDirectoryName(int delta)
        {
            List<string> sequencePath = new List<string>();
            // Generate output sequence folder name
            sequencePath.Add(parameters.OutputDatasetFolder);
            sequencePath.Add(parameters.OutputSequenceName);

            if (parameters.IsAutoIncrement)
            {
                var subdirectories = Directory.GetDirectories(parameters.OutputDatasetFolder);
                sequencePath[1] = sequencePath.ElementAt(1) + "_" + (subdirectories.Length + 1 + delta).ToString();
            }

            return System.IO.Path.Combine(sequencePath.ToArray());
        }

        private bool createSequenceDirectory(ref string sequenceFolder)
        {
            int delta = 0;
            sequenceFolder = generateSequenceDirectoryName(delta++);
            bool foundName = false;
            
            while(!foundName)
            {
                if (Directory.Exists(sequenceFolder))
                {
                    if (!parameters.IsAutoIncrement)
                    {
                        System.Windows.MessageBox.Show(String.Format("Directory {0} exists\nPlease change the name", sequenceFolder));
                        return false;
                    }
                    sequenceFolder = generateSequenceDirectoryName(delta++);
                }
                else
                {
                    Directory.CreateDirectory(sequenceFolder);
                    foundName = true;
                }
            }
            
            return true;
        }

        private void generateSequenceImages(string sequenceFolder)
        {
            int frameZeroedId = 0;
            string fileName;
            for (int currenFrameId = parameters.CurrentFramePos; currenFrameId < parameters.CurrentFramePos + parameters.NumFrames; currenFrameId++)
            {
                videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames, currenFrameId);
                System.Drawing.Rectangle roi = new System.Drawing.Rectangle();
                roi.X = Math.Min((int)roiCoords[0].X, (int)roiCoords[1].X);
                roi.Y = Math.Min((int)roiCoords[0].Y, (int)roiCoords[1].Y);
                roi.Width = (int)Math.Abs(roiCoords[0].X - roiCoords[1].X);
                roi.Height = (int)Math.Abs(roiCoords[0].Y - roiCoords[1].Y);

                using (Mat frameTemp = videoCapture.QueryFrame())
                {
                    if (frameTemp != null)
                    {
                        Mat croppedImage = new Mat(frameTemp, roi);
                        if (croppedImage.Width != parameters.Resolution)
                            CvInvoke.Resize(croppedImage, croppedImage, new System.Drawing.Size(parameters.Resolution, parameters.Resolution), 0, 0, Emgu.CV.CvEnum.Inter.Lanczos4);
                        fileName = System.IO.Path.Combine(sequenceFolder, String.Format("{0:D6}.png", frameZeroedId++));
                        croppedImage.Save(fileName);
                    }
                }

            }
        }

        private void generateMetadataXML(string sequenceFolder)
        {
            string[] inputFileElements = parameters.InputFilePath.Split(System.IO.Path.DirectorySeparatorChar);
            string inputFileName = inputFileElements[inputFileElements.Length-1];
            string outputFullPath = System.IO.Path.Combine(sequenceFolder, "metadata.xml");

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            settings.CloseOutput = true;
            settings.OmitXmlDeclaration = true;

            using (XmlWriter writer = XmlWriter.Create(outputFullPath))
            {
                writer.WriteStartElement("sequence");
                writer.WriteElementString("input_video_file_name", inputFileName);
                writer.WriteElementString("input_video_file_full_path", parameters.InputFilePath);
                writer.WriteElementString("start_frame", parameters.CurrentFramePos.ToString());
                writer.WriteElementString("frames_number", parameters.NumFrames.ToString());
                writer.WriteElementString("resolution", parameters.Resolution.ToString());
                writer.WriteElementString("class", parameters.ClassName);
                writer.WriteEndElement();
                writer.Flush();
            }
        }

        private void bGenerateOutput_Click(object sender, RoutedEventArgs e)
        {
            if (!checksGenerateOutput())
            {
                return;
            }
            string sequenceFolder = null;
            //generateSequenceDirectoryName(sequenceFolder);
            if (!createSequenceDirectory(ref sequenceFolder))
            {
                return;
            }
            generateSequenceImages(sequenceFolder);
            generateMetadataXML(sequenceFolder);
            System.Windows.MessageBox.Show(String.Format("Sequence generated into the folder\n{0}", sequenceFolder));
        }

        private void tbSequenceName_LostFocus(object sender, RoutedEventArgs e)
        {
            updateElementsStyles();
        }

        private void tbNumFrames_LostFocus(object sender, RoutedEventArgs e)
        {
            updateElementsStyles();
        }

        private void tbResolution_LostFocus(object sender, RoutedEventArgs e)
        {
            updateElementsStyles();
        }
    }

    class DynamicData : INotifyPropertyChanged
    {
        private string inputFilePath;
        private string outputDatasetFolder;
        private string outputSequenceName;
        private int numFrames;
        private int resolution;
        private bool isAutoIncrement;
        private int currentFramePos;
        private string className;

        public int CurrentFramePos
        {
            get
            {
                return currentFramePos;
            }
            set
            {
                currentFramePos = value;
                OnPropertyChanged("CurrentFramePos");
            }
        }

        public bool IsAutoIncrement
        {
            get
            {
                return isAutoIncrement;
            }
            set
            {
                isAutoIncrement = value;
                OnPropertyChanged("IsAutoIncrement");
            }
        }

        public int Resolution
        {
            get { return resolution; }
            set
            {
                resolution = value;
                OnPropertyChanged("Resolution");
            }
        }

        public int NumFrames
        {
            get { return numFrames; }
            set
            {
                numFrames = value;
                OnPropertyChanged("NumFrames");
            }
        }

        public string ClassName
        {
            get
            {
                return className;
            }
            set
            {
                className = value;
                OnPropertyChanged("ClassName");
            }
        }

        public string InputFilePath
        {
            get { return inputFilePath; }
            set
            {
                inputFilePath = value;
                OnPropertyChanged("InputFilePath");
            }
        }

        public string OutputDatasetFolder
        {
            get { return outputDatasetFolder; }
            set
            {
                outputDatasetFolder = value;
                OnPropertyChanged("OutputDatasetFolder");
            }
        }

        public string OutputSequenceName
        {
            get { return outputSequenceName; }
            set
            {
                outputSequenceName = value;
                OnPropertyChanged("OutputSequenceName");
            }
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
