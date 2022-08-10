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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace MicControl
{
    /// <summary>
    /// Overray.xaml の相互作用ロジック
    /// </summary>
    public partial class OverlayWindow : Window
    {

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        MainWindow mainWindow;
        CancellationTokenSource cancelTokenSource;
        bool isAdjustMode = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImportAttribute("user32.dll")]
        private static extern bool ReleaseCapture();


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

        private void MouseLeftButtonDown_DragStart(object sender, MouseButtonEventArgs e)
        {
            if ((System.Windows.Forms.Control.ModifierKeys & Keys.Control) == Keys.Control &&
                isAdjustMode == false)
            {
                //マウスのキャプチャを解除
                ReleaseCapture();
                //タイトルバーでマウスの左ボタンが押されたことにする
                System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(this);
                _ = SendMessage(helper.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
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

        public void AdjustModeChange(bool adjustMode)
        {
            if (adjustMode)
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 128, 8, 255));
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                this.MouseLeftButtonDown += DragToMove;
                isAdjustMode = true;
            }
            else
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                this.ResizeMode = ResizeMode.NoResize;
                this.MouseLeftButtonDown -= DragToMove;
                isAdjustMode = false;
            }
        }

        public bool AdjustModeToggle()
        {
            if (this.ResizeMode == ResizeMode.NoResize)
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 128, 8, 255));
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                this.MouseLeftButtonDown += DragToMove;
                isAdjustMode = true;
                return true;
            }
            else
            {
                this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
                this.ResizeMode = ResizeMode.NoResize;
                this.MouseLeftButtonDown -= DragToMove;
                isAdjustMode = false;
                return false;
            }
        }

        private void DragToMove(object sender, EventArgs e)
        {
            this.DragMove();
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

        public Point GetPosition()
        {
            return new Point(this.Top, this.Left);
        }

        public void SetPosition(Point point)
        {
            this.Left = point.X;
            this.Top = point.Y;
        }

        public void SetSize(int pixel)
        {
            this.Width = pixel;
            this.Height = pixel;
        }

    }
}
