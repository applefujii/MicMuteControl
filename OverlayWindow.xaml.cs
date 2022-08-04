using System;
using System.Collections.Generic;
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

namespace MicControl
{
    /// <summary>
    /// Overray.xaml の相互作用ロジック
    /// </summary>
    public partial class OverlayWindow : Window
    {

        MainWindow mainWindow;
        CancellationTokenSource cancelTokenSource;


        public OverlayWindow(MainWindow w)
        {
            InitializeComponent();
            this.mainWindow = w;
        }


        private void DoubleClick_HiddenShortTime(object sender, MouseButtonEventArgs e)
        {
            if (cancelTokenSource != null) cancelTokenSource.Dispose();
            cancelTokenSource = new CancellationTokenSource();
            mainWindow.setCancellationTokenSource(cancelTokenSource);
            CancellationToken cancelToken = cancelTokenSource.Token;
            _ = HiddenShortTime(cancelToken);
        }


        public void SetVisibility( bool visible )
        {
            if( visible ) this.Visibility = Visibility.Visible;
            else this.Visibility = Visibility.Hidden;
        }

        public void SetMicStateImage(bool isMute)
        {
            this.MicStateImage.Visibility = Visibility.Visible;
            if (isMute)
            {
                if (mainWindow.isOverlayMutedInvisible) this.MicStateImage.Visibility = Visibility.Hidden;
                this.MicStateImage.Source = mainWindow.imageMuted;
            } 
            else this.MicStateImage.Source = mainWindow.imageUnmuted;
        }

        public void SetPosition(Point point)
        {
            this.Top = point.X;
            this.Left = point.Y;
        }

        public void SetSize(int pixel)
        {
            this.Width = pixel;
            this.Height = pixel;
        }

        public void AdjustModeChange(bool adjustMode)
        {
            if (adjustMode)
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 128, 8, 255));
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                this.MouseLeftButtonDown += DragToMove;
            }
            else
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                this.ResizeMode = ResizeMode.NoResize;
                this.MouseLeftButtonDown -= DragToMove;
            }
        }

        public bool AdjustModeToggle()
        {
            if (this.ResizeMode == ResizeMode.NoResize)
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 128, 8, 255));
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                this.MouseLeftButtonDown += DragToMove;
                return true;
            }
            else
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                this.ResizeMode = ResizeMode.NoResize;
                this.MouseLeftButtonDown -= DragToMove;
                return false;
            }
        }

        private void DragToMove(object sender, EventArgs e)
        {
            this.DragMove();
        }

        public Point getPosition()
        {
            return new Point(this.Top, this.Left);
        }

        private async Task HiddenShortTime(CancellationToken cancelToken)
        {
            this.Visibility = Visibility.Hidden;
            await Task.Delay(10000);
            if (cancelToken.IsCancellationRequested)
            {
                return;
            }
            this.Visibility = Visibility.Visible;
        }

    }
}
