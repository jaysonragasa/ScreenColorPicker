using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenColorPicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _currentHexColor = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            BitmapSource img = CopyScreen();

            this.Width = SystemParameters.MaximizedPrimaryScreenWidth;
            this.Height = SystemParameters.MaximizedPrimaryScreenHeight;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.None;

            this.screenImage.Source = CopyScreen();
            //this.imgZoomed.Source = this.screenImage.Source;
            this.screenImage.Stretch = Stretch.None;
            //this.imgZoomed.Visibility = Visibility.Collapsed;

            this.Cursor = Cursors.None;
            this.Cursor = new Cursor(new MemoryStream(Properties.Resources.color_picker));

            this.Loaded += MainWindow_Loaded;
            this.MouseMove += MainWindow_MouseMove;
            this.MouseDown += MainWindow_MouseDown;
            //this.screenImage.MouseWheel += MainWindow_MouseWheel;
        }

        //private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    System.Windows.Point point = e.GetPosition(this);
        //    var element = imgZoomed as UIElement;
        //    var transform = element.RenderTransform as MatrixTransform;
        //    var matrix = transform.Matrix;
        //    var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor
        //    matrix.ScaleAtPrepend(scale, scale, point.X, point.Y);
        //    element.RenderTransform = new MatrixTransform(matrix);

        //    Matrix m = element.RenderTransform.Value;

        //    txZoom.Text = m.M11.ToString();

        //    if (m.M11 < 1.0)
        //    {
        //        matrix.ScaleAtPrepend(1.1, 1.1, point.X, point.Y);
        //        element.RenderTransform = new MatrixTransform(matrix);
        //    }
        //}

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //Clipboard.Clear();

                Clipboard.SetText(this._currentHexColor);

                //try
                //{
                //    Clipboard.SetDataObject(this._currentHexColor);
                //}
                //catch(Exception ex)
                //{
                //    MessageBox.Show(ex.Message);
                //}
            }

            Application.Current.Shutdown();
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point point = e.GetPosition(this);

            //move info box
            System.Windows.Point oldPt = point;
            System.Windows.Point newPt = oldPt;

            double actualWidth = grdInfo.ActualWidth + 10;
            double actualHeight = grdInfo.ActualHeight + 10;

            newPt.X = (newPt.X + actualWidth) > this.ActualWidth ? this.ActualWidth - actualWidth : oldPt.X + 10;
            newPt.Y = (newPt.Y + actualHeight) > this.ActualHeight ? this.ActualHeight - actualHeight : oldPt.Y + 10;

            Canvas.SetLeft(grdInfo, newPt.X);
            Canvas.SetTop(grdInfo, newPt.Y);

            // Use RenderTargetBitmap to get the visual, in case the image has been transformed.
            var renderTargetBitmap = new RenderTargetBitmap((int)this.ActualWidth,
                                                            (int)this.ActualHeight,
                                                            96, 96, PixelFormats.Default);
            renderTargetBitmap.Render(screenImage);

            // Make sure that the point is within the dimensions of the image.
            if ((point.X <= renderTargetBitmap.PixelWidth) && (point.Y <= renderTargetBitmap.PixelHeight))
            {
                // Create a cropped image at the supplied point coordinates.
                var croppedBitmap = new CroppedBitmap(renderTargetBitmap,
                                                      new Int32Rect((int)point.X, (int)point.Y - 10, 1, 1));

                // Copy the sampled pixel to a byte array.
                var pixels = new byte[4];
                croppedBitmap.CopyPixels(pixels, 4, 0);

                // Assign the sampled color to a SolidColorBrush and return as conversion.
                System.Drawing.Color clr = System.Drawing.Color.FromArgb(255, pixels[2], pixels[1], pixels[0]);

                txRGB.Text = $"R:{clr.R} G:{clr.G} B:{clr.B}";
                txRGBHex.Text = $"#{clr.A:X2} {clr.R:X2} {clr.G:X2} {clr.B:X2}";
                borderColor.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(clr.A, clr.R, clr.G, clr.B));

                this._currentHexColor = $"#{clr.A:X2}" +
                                        $"{clr.R:X2}" +
                                        $"{clr.G:X2}" +
                                        $"{clr.B:X2}";
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private BitmapSource CopyScreen()
        {
            using (var screenBmp = new Bitmap(
                (int)SystemParameters.PrimaryScreenWidth,
                (int)SystemParameters.PrimaryScreenHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(0, 0, 0, 0, screenBmp.Size);
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        screenBmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
        }

        private BitmapSource RefreshZoomedImage(System.Windows.Point pt)
        {
            using (var screenBmp = new Bitmap(
                80,
                80,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {


                MemoryStream ms = new MemoryStream();
                var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(this.screenImage.Source as System.Windows.Media.Imaging.BitmapSource));
                encoder.Save(ms);
                ms.Flush();

                using (var bmpGraphics = Graphics.FromImage(System.Drawing.Image.FromStream(ms)))
                {
                    bmpGraphics.CopyFromScreen((int)pt.X, (int)pt.Y, 0, 0, screenBmp.Size);
                    return Imaging.CreateBitmapSourceFromHBitmap(
                        screenBmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }

            //CroppedBitmap cp = new CroppedBitmap(this.screenImage.Source as BitmapSource, new Int32Rect((int)pt.X, (int)pt.Y, 80, 80));
            //cp.
        }
    }
}
