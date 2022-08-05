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
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using System.Drawing;
using Microsoft.Win32;
using Application = System.Windows.Forms.Application;
using System.Security.Permissions;
using System.Windows.Interop;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.IO.Compression;
using System.Drawing.Imaging;

namespace MicControl
{

    public enum ACTION_MODE : int
    {
        TOGGLE = 0,
        PUSH_TO_TALK = 1,
        PUSH_TO_MUTE = 2
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private static readonly String appName = "MicMuteControl";

        private OverlayWindow overlayWindow;
        private CancellationTokenSource cancelTokenSource;

        private NotifyIcon tray;
        private SystemVolumeConfigurator mic;
        private List<Hotkey> lHotkey;

        private IniManager ini;
        private String baseUrl;
        private ACTION_MODE mode;
        private MOD_KEY modKey;
        private String triggerKey;
        private bool isGamingMode = false;
        public bool isOverlayMutedInvisible = false;
        private bool isStartUp;

        // リソース
        private int overlayImageNo = 1;
        private int iconImageNo = 1;
        public BitmapImage imageUnmuted;
        public BitmapImage imageMuted;
        public Icon iconUnmuted;
        public Icon iconMuted;


        /** 初期化 */
        public MainWindow()
        {
            InitializeComponent();
            this.IsEnabled = false;
            ini = new IniManager("./setting.ini");
            lHotkey = new List<Hotkey>();
            //Application.UseWaitCursor = true;
            //Application.DoEvents();
        }

        /** 初期化(ウィンドウ描画後) */
        private void Win_Rendered(object sender, EventArgs e)
        {
            String[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2) {
                if (args[1] == "/tray") this.Visibility = Visibility.Hidden;
            }
            Initialize();
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        private async void Initialize()
        {
            // 非同期処理の開始
            Task init = InitializeAsync();

            DeleteOldFiles();

            //-- スタートアップ判定
            isStartUp = isStartupEnabled();
            if (isStartUp)
            {
                settingStartup.IsChecked = true;
                // this.Visibility = Visibility.Hidden;
            }

            if (ini.ReadValue("system", "auto_update_check") == "true") {
                MenuItem_Help_about.IsChecked = true;
                UpdateCheck( false );
            }
            overlayImageNo = ini.ReadValueInt("image_setting", "overlay_image_no");
            iconImageNo = ini.ReadValueInt("image_setting", "icon_image_no");
            isOverlayMutedInvisible = ini.ReadValueBoolean("image_setting", "overlay_muted_invisible");

            //-- リソース読み込み
            imageUnmuted = new BitmapImage(new Uri(@"image\unmuted"+overlayImageNo+".png", UriKind.Relative));
            imageMuted = new BitmapImage(new Uri(@"image\muted"+ overlayImageNo + ".png", UriKind.Relative));
            Console.WriteLine(imageUnmuted.PixelWidth);
            Console.WriteLine(imageMuted.PixelWidth);
            iconUnmuted = ConvertIcon(new Bitmap(@"image\unmuted" + iconImageNo + ".png"));
            iconMuted = ConvertIcon(new Bitmap(@"image\muted" + iconImageNo + ".png"));

            TrayInitialize();       //タスクトレイに表示

            //---- メニューに項目追加
            int imgNo = 1;
            while(true) {
                if (!File.Exists(@"image\unmuted" + imgNo + ".png") ||
                    !File.Exists(@"image\muted" + imgNo + ".png"))
                    break;
                //-- オーバーレイ
                System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
                item.Header = "アイコン" + imgNo;
                item.Tag = imgNo.ToString();
                item.Click += Menu_Setting_OverlayIcon;
                this.overlayIcon.Items.Add(item);
                //-- トレイ
                item = new System.Windows.Controls.MenuItem();
                item.Header = "アイコン" + imgNo;
                item.Tag = imgNo.ToString();
                item.Click += Menu_Setting_TrayIcon;
                this.trayIcon.Items.Add(item);
                imgNo++;
            }

            this.overlayMutedVisibleToggle.IsChecked = isOverlayMutedInvisible;

            overlayWindow = new OverlayWindow(this);
            String[] sP = ini.ReadValue("Position", "overlay").Split(',');
            overlayWindow.SetPosition( new System.Windows.Point(int.Parse(sP[0]), int.Parse(sP[1])) );
            if (ini.ReadValue("action_mode", "overlay") == "true")
                ShowOverlay(true);
            else
                ShowOverlay(false);

            baseUrl = ini.ReadValue("system", "update_check_url");

            mode = (ACTION_MODE)ini.ReadValueInt("action_mode", "mode");
            isGamingMode = ini.ReadValueBoolean("action_mode", "gaming_mode");
            if(isGamingMode) this.gamingMode.IsChecked = true;
            ModeChangeFlush();

            // 非同期処理終了を待つ
            //Debug.WriteLine(Thread.CurrentThread.ManagedThreadId + "非同期処理終了before");
            await init;
            //Debug.WriteLine(Thread.CurrentThread.ManagedThreadId + "非同期処理終了after");

            //Application.UseWaitCursor = false;
            //Application.DoEvents();
            this.IsEnabled = true;
        }

        private async Task InitializeAsync()
        {
            //-- 非同期処理
            await Task.Run(() =>
            {
                mic = new SystemVolumeConfigurator();
                //Debug.WriteLine(Thread.CurrentThread.ManagedThreadId + "非同期処理終了");
            });

            //---- 非同期処理終了後 (録音デバイス読み込み後)

            String iniDefDev = ini.ReadValue("device", "default");
            captureDevices.ItemsSource = mic.getCaptureDevices();
            captureDevices.SelectedItem = iniDefDev;
            if (captureDevices.SelectedItem == null) captureDevices.SelectedItem = mic.getDefaultCaptureDevice();
            SystemSettingReflect();
            TrayFlush();

            //マイクの状態を監視
            SurveillanceMicVolume();

            //-- iniから設定を読み込み、ホットキーを登録
            modKey = (MOD_KEY)ini.ReadValueInt("hotkey", "modifire_key");
            triggerKey = ini.ReadValue("hotkey", "trigger_key");
            KeysConverter con = new KeysConverter();
            HotKeyRegistration(modKey, triggerKey);

            ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_Mode", true)[0]).Enabled = true;
            ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_OverlayToggle", true)[0]).Enabled = true;
            ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_GamingModeToggle", true)[0]).Enabled = true;
        }


        ////////////////////////////////// イベント ////////////////////////////////////////////////////

        /** WindowsMessage */
            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_SIZE = 0x5;
            const int SIZE_RESTORED = 0x0;
            const int SIZE_MINIMIZED = 0x1;
            const int SIZE_MAXIMIZED = 0x2;

            //リサイズされたら
            if (msg == WM_SIZE)
            {
                switch (wParam.ToInt32())
                {
                    case SIZE_RESTORED:
                        break;
                    case SIZE_MINIMIZED:
                        this.Visibility = Visibility.Hidden;
                        break;
                    case SIZE_MAXIMIZED:
                        break;
                }
            }
            return IntPtr.Zero;
        }

        /** 閉じたとき */
        public void Win_Closing(object sender, CancelEventArgs e)
        {
            overlayWindow.Close();
            HotkeyDispose();
            tray.Dispose();
        }


        //---------------------------- ホットキー -------------------------------------

        /** ホットキーを押したとき */
        private void HotKey_Push(object sender, EventArgs e)
        {
            Push();
        }

        /** ホットキーを離したとき */
        private void HotKey_Release(object sender, EventArgs e)
        {
            Release();
        }



        //---------------------------- メニューバー -------------------------------------

        /** メニュー ファイル/リフレッシュ */
        public async void Menu_RefleshCaptureDevice(object sender, RoutedEventArgs e)
        {
            //Application.UseWaitCursor = true;
            //Application.DoEvents();
            this.IsEnabled = false;

            await mic.RefleshCaptureDeviceAsync();

            //Application.UseWaitCursor = false;
            //Application.DoEvents();
            this.IsEnabled = true;
        }

        private void Menu_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /** メニュー モード/トグル */
        private void Menu_Mode_Toggle(object sender, RoutedEventArgs e)
        {
            ActionModeChange(ACTION_MODE.TOGGLE);
        }

        /** メニュー モード/プッシュトゥートーク */
        private void Menu_Mode_PushToTalk(object sender, RoutedEventArgs e)
        {
            ActionModeChange(ACTION_MODE.PUSH_TO_TALK);
        }

        /** メニュー モード/プッシュトゥーミュート */
        private void Menu_Mode_PushToMute(object sender, RoutedEventArgs e)
        {
            ActionModeChange(ACTION_MODE.PUSH_TO_MUTE);
        }

        /** メニュー セッティング/ホットキー */
        private void Menu_Setting_Hotkey(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new SettingWindow(this);
            settingWindow.ShowDialog();
        }

        /** メニュー セッティング/ゲーミングモード */
        private void Menu_Setting_GamingMode(object sender, RoutedEventArgs e)
        {
            isGamingMode = !isGamingMode;
            GamingMode(isGamingMode);
        }

        /** メニュー セッティング/オーバーレイアイコン */
        private void Menu_Setting_OverlayIcon(object sender, EventArgs e)
        {
            System.Windows.Controls.MenuItem menuitem = (System.Windows.Controls.MenuItem)sender;
            string tag = menuitem.Tag.ToString();
            imageUnmuted = new BitmapImage(new Uri(@"image\unmuted"+tag+".png", UriKind.Relative));
            imageMuted = new BitmapImage(new Uri(@"image\muted"+tag+".png", UriKind.Relative));
            Console.WriteLine(imageUnmuted.PixelWidth);
            Console.WriteLine(imageMuted.PixelWidth);
            ini.WriteValue("image_setting", "overlay_image_no", tag);
        }

        /** メニュー セッティング/トレイアイコン */
        private void Menu_Setting_TrayIcon(object sender, EventArgs e)
        {
            System.Windows.Controls.MenuItem menuitem = (System.Windows.Controls.MenuItem)sender;
            string tag = menuitem.Tag.ToString();
            iconUnmuted = ConvertIcon(new Bitmap(@"image\unmuted"+tag+".png"));
            iconMuted = ConvertIcon(new Bitmap(@"image\muted"+tag+".png"));
            ini.WriteValue("image_setting", "icon_image_no", tag);
        }

        /** メニュー オーバーレイ ミュート時アイコン非表示 */
        private void Menu_overlayMutedVisibleToggle(object sender, RoutedEventArgs e)
        {
            isOverlayMutedInvisible = !isOverlayMutedInvisible;
            this.overlayMutedVisibleToggle.IsChecked = isOverlayMutedInvisible;
            if(isOverlayMutedInvisible == true) ini.WriteValue("image_setting", "overlay_muted_invisible", "true");
            else ini.WriteValue("image_setting", "overlay_muted_invisible", "false");
        }

        /** メニュー セッティング/スタートアップに登録 */
        private void Menu_Setting_RegStartup(object sender, RoutedEventArgs e)
        {
            //Runキーを開く
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (RegistryKey regkey = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                if (isStartUp == false)
                {
                    regkey.SetValue(Application.ProductName, "\"" + Application.StartupPath + "\\start.vbs\"");
                    isStartUp = true;
                    settingStartup.IsChecked = true;
                }
                else
                {
                    regkey.DeleteValue(Application.ProductName, false);
                    isStartUp = false;
                    settingStartup.IsChecked = false;
                }
                regkey.Close();
            }
        }

        /** メニュー ヘルプ/更新の確認 */
        private void Menu_Help_UpdateCheck(object sender, RoutedEventArgs e)
        {
            UpdateCheck();
        }

        /** メニュー ヘルプ/起動時に更新を確認する */
        private void Menu_Help_AutoUpdateCheck(object sender, RoutedEventArgs e)
        {
            MenuItem_Help_about.IsChecked = !MenuItem_Help_about.IsChecked;

            if (MenuItem_Help_about.IsChecked)
            {
                ini.WriteValue("system", "auto_update_check", "true");
            } else
            {
                ini.WriteValue("system", "auto_update_check", "false");
            }
        }

        /** メニュー ヘルプ/About */
        private void Menu_Help_About(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }


        //---------------------------- 画面中 -------------------------------------

        /** 操作する入力機器切り替え */
        private void CaptureDevices_Updated(object sender, SelectionChangedEventArgs e)
        {
            mic.setControlCaptureDevice((string)e.AddedItems[0]);
            SystemSettingReflect();
            TrayFlush();

            ini.WriteValue("device", "default", ((string)e.AddedItems[0]));
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mic.setControlCapturDeviceVolume( e.NewValue );
            LabelVolume.Content = Math.Round(e.NewValue).ToString();
        }

        private void Click_RadioOn(object sender, RoutedEventArgs e)
        {
            ShowOverlay(true);
        }

        private void Click_RadioOff(object sender, RoutedEventArgs e)
        {
            overlayWindow.AdjustModeChange(false);
            if(cancelTokenSource != null) cancelTokenSource.Cancel();
            ShowOverlay(false);
        }

        private void Click_Button_OverrayAdjust(object sender, RoutedEventArgs e)
        {
            if (overlayWindow.AdjustModeToggle())
            {
                System.Windows.Point p = overlayWindow.getPosition();
                ini.WriteValue("Position", "overlay", p.ToString());
            }
        }

        private void Click_Button_OverrayReset(object sender, RoutedEventArgs e)
        {
            System.Windows.Point p = new System.Windows.Point(0, 0);
            //System.Windows.Point p = this.PointToScreen(new System.Windows.Point(0,0));
            overlayWindow.SetPosition(p);
            overlayWindow.SetSize(32);
            ini.WriteValue("Position", "overlay", p.ToString());
        }


        //---------------------------- タスクトレイ -------------------------------------

        private async void Tray_DoubleClick(object sender, EventArgs e)
        {
            //フォームを表示する
            this.Visibility = Visibility.Visible;
            await Task.Delay(10);       //時間をずらさないと最小化解除できない
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
                this.Activate();
            }
        }

        /** タスクトレイ メニュー/トグル */
        private void TrayMenu_Toggle(object sender, EventArgs e)
        {
            ActionModeChange(ACTION_MODE.TOGGLE);
        }

        /** タスクトレイ メニュー/プッシュトゥトーク */
        private void TrayMenu_PushToTalk(object sender, EventArgs e)
        {
            ActionModeChange(ACTION_MODE.PUSH_TO_TALK);
        }

        /** タスクトレイ メニュー/プッシュトゥミュート */
        private void TrayMenu_PushToMute(object sender, EventArgs e)
        {
            ActionModeChange(ACTION_MODE.PUSH_TO_MUTE);
        }

        /** タスクトレイ メニュー/オーバーレイを表示 */
        private void TrayMenu_OverlayToggle(object sender, EventArgs e)
        {
            ToolStripMenuItem xSender = (ToolStripMenuItem)sender;
            xSender.Checked = !xSender.Checked;
            if (xSender.Checked) ShowOverlay(true);
            else
            {
                overlayWindow.AdjustModeChange(false);
                if (cancelTokenSource != null) cancelTokenSource.Cancel();
                ShowOverlay(false);
            }
        }

        /** タスクトレイ メニュー/オーバーレイを表示 */
        private void TrayMenu_GamingModeToggle(object sender, EventArgs e)
        {
            ToolStripMenuItem xSender = (ToolStripMenuItem)sender;
            isGamingMode = !isGamingMode;
            xSender.Checked = isGamingMode;
            GamingMode(isGamingMode);
        }

        /** タスクトレイ 閉じるキー */
        private void TrayMenu_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }






        //////////////////////////////////////////////// 動作 ////////////////////////////////////////////////////////

        /** ホットキーを押したとき */
        private void Push()
        {
            Console.WriteLine("push");
            switch (mode)
            {
                case ACTION_MODE.TOGGLE:
                    mic.ToggleMicMute();
                    break;
                case ACTION_MODE.PUSH_TO_TALK:
                    mic.SetMicUnmute();
                    break;
                case ACTION_MODE.PUSH_TO_MUTE:
                    mic.SetMicMute();
                    break;
            }

            TrayFlush();
            OverlayFlush();
        }

        /** ホットキーを離したとき */
        private void Release()
        {
            Console.WriteLine("release");
            switch (mode)
            {
                case ACTION_MODE.TOGGLE:
                    break;
                case ACTION_MODE.PUSH_TO_TALK:
                    mic.SetMicMute();
                    break;
                case ACTION_MODE.PUSH_TO_MUTE:
                    mic.SetMicUnmute();
                    break;
            }

            TrayFlush();
            OverlayFlush();
        }

        private void ActionModeChange( ACTION_MODE mode )
        {
            switch (mode)
            {
                case ACTION_MODE.TOGGLE:
                    this.mode = ACTION_MODE.TOGGLE;
                    break;
                case ACTION_MODE.PUSH_TO_TALK:
                    this.mode = ACTION_MODE.PUSH_TO_TALK;
                    mic.SetMicMute();
                    break;
                case ACTION_MODE.PUSH_TO_MUTE:
                    this.mode = ACTION_MODE.PUSH_TO_MUTE;
                    mic.SetMicUnmute();
                    break;
            }
            ModeChangeFlush();
            TrayFlush();
        }

        private void ShowOverlay(bool isEnable)
        {
            Console.WriteLine("in "+ ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_OverlayToggle", true)[0]).Checked);
            if (isEnable)
            {
                Radio_OverlayOn.IsChecked = true;
                ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_OverlayToggle", true)[0]).Checked = true; 
                overlayWindow.SetVisibility(true);
                Button_OverrayAdjust.IsEnabled = true;
                Button_OverrayReset.IsEnabled = true;
                ini.WriteValue("action_mode", "overlay","true");
            }
            else
            {
                Radio_OverlayOff.IsChecked = true;
                ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_OverlayToggle", true)[0]).Checked = false;
                overlayWindow.SetVisibility(false);
                Button_OverrayAdjust.IsEnabled = false;
                Button_OverrayReset.IsEnabled = false;
                ini.WriteValue("action_mode", "overlay", "false");
            }
            Console.WriteLine("out " + ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_OverlayToggle", true)[0]).Checked);
        }

        private void GamingMode(bool isEnable)
        {
            if (isEnable)
            {
                this.gamingMode.IsChecked = true;
                ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_GamingModeToggle", true)[0]).Checked = true;
                HotKeyRegistration(modKey, triggerKey);
                ini.WriteValue("action_mode", "gaming_mode", "true");
            }
            else
            {
                this.gamingMode.IsChecked = false;
                ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_GamingModeToggle", true)[0]).Checked = false;
                HotKeyRegistration(modKey, triggerKey);
                ini.WriteValue("action_mode", "gaming_mode", "false");
            }
        }




        public void HotkeyRegistration()
        {
            HotKeyRegistration(modKey, triggerKey);
        }

        public void HotKeyRegistration(MOD_KEY mod, String keyName)
        {
            Keys tKey = Keys.None;
            keyName = keyName.Replace("Ime", "IME");
            keyName = keyName.Replace("NonConvert", "Nonconvert");
            KeysConverter con = new KeysConverter();
            try
            {
                tKey = (Keys)con.ConvertFromString(keyName);
            }
            catch ( Exception e )
            {
                return;
            }
            HotkeyDispose();
            Hotkey hk = new Hotkey(mod, tKey);
            hk.HotKeyPush += new EventHandler(HotKey_Push);
            hk.HotKeyRelease += new EventHandler(HotKey_Release);
            lHotkey.Add(hk);
            //-- ゲーミングモード
            if(isGamingMode)
            {
                MOD_KEY[] modkey;
                if (mod == 0)
                {
                    modkey = new MOD_KEY[]{ MOD_KEY.ALT, MOD_KEY.CONTROL, MOD_KEY.SHIFT };
                    foreach (MOD_KEY mk in modkey)
                    {
                        Console.WriteLine(mk);
                        hk = new Hotkey(mk, tKey);
                        hk.HotKeyPush += new EventHandler(HotKey_Push);
                        hk.HotKeyRelease += new EventHandler(HotKey_Release);
                        lHotkey.Add(hk);
                    }
                    modkey = new MOD_KEY[]{ MOD_KEY.ALT | MOD_KEY.CONTROL, MOD_KEY.ALT | MOD_KEY.SHIFT, MOD_KEY.CONTROL | MOD_KEY.SHIFT, MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT };
                    foreach (MOD_KEY mk in modkey)
                    {
                        Console.WriteLine(mk);
                        hk = new Hotkey(mk, tKey);
                        hk.HotKeyPush += new EventHandler(HotKey_Push);
                        hk.HotKeyRelease += new EventHandler(HotKey_Release);
                        lHotkey.Add(hk);
                    }
                } else {
                    modkey = new MOD_KEY[]{ MOD_KEY.ALT | MOD_KEY.CONTROL, MOD_KEY.ALT | MOD_KEY.SHIFT, MOD_KEY.CONTROL | MOD_KEY.SHIFT, MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT };
                    foreach (MOD_KEY mk in modkey)
                    {
                        if (mk == mod) continue;
                        if ((int)(mk & mod) != (int)mod) continue;
                        Console.WriteLine(mk);
                        hk = new Hotkey(mk, tKey);
                        hk.HotKeyPush += new EventHandler(HotKey_Push);
                        hk.HotKeyRelease += new EventHandler(HotKey_Release);
                        lHotkey.Add(hk);
                    }
                }
            }
        }

        public void HotkeyDispose()
        {
            foreach(Hotkey hk in lHotkey)
            {
                hk.Dispose();
            }
        }

        private void TrayInitialize()
        {
            tray = new NotifyIcon();
            tray.Icon = Properties.Resources.initializing;
            tray.Visible = true;
            tray.Text = "MicControl";
            tray.DoubleClick += new EventHandler(Tray_DoubleClick);
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem menuItem;
            ToolStripMenuItem menuItemChild;

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "モード";
            menuItem.Name = "trayMenu_Mode";
            menuItem.Enabled = false;
            menu.Items.Add(menuItem);

            menuItemChild = new ToolStripMenuItem();
            menuItemChild.Text = "トグル";
            menuItemChild.Name = "trayMenu_Toggle";
            menuItemChild.Click += new EventHandler(TrayMenu_Toggle);
            menuItem.DropDownItems.Add(menuItemChild);
            menuItemChild = new ToolStripMenuItem();
            menuItemChild.Text = "プッシュトゥトーク";
            menuItemChild.Name = "trayMenu_PushToTalk";
            menuItemChild.Click += new EventHandler(TrayMenu_PushToTalk);
            menuItem.DropDownItems.Add(menuItemChild);
            menuItemChild = new ToolStripMenuItem();
            menuItemChild.Text = "プッシュトゥミュート";
            menuItemChild.Name = "trayMenu_PushToMute";
            menuItemChild.Click += new EventHandler(TrayMenu_PushToMute);
            menuItem.DropDownItems.Add(menuItemChild);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "オーバーレイを表示";
            menuItem.Name = "trayMenu_OverlayToggle";
            menuItem.Click += new EventHandler(TrayMenu_OverlayToggle);
            menuItem.Enabled = false;
            menu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "ゲーミングモード";
            menuItem.Name = "trayMenu_GamingModeToggle";
            menuItem.Click += new EventHandler(TrayMenu_GamingModeToggle);
            menuItem.Enabled = false;
            menu.Items.Add(menuItem);

            menu.Items.Add(new ToolStripSeparator());

            menuItem = new ToolStripMenuItem();
            menuItem.Text = "終了";
            menuItem.Click += new EventHandler(TrayMenu_Close_Click);
            menu.Items.Add(menuItem);

            tray.ContextMenuStrip = menu;
        }

        private void SystemSettingReflect()
        {
            VolumeSlider.Value = mic.getControlCapturDeviceVolume();
            TrayFlush();
            OverlayFlush();
        }

        private void ModeChangeFlush()
        {
            ini.WriteValue("action_mode", "mode", ((int)mode).ToString());
            modeToggle.IsChecked = false;
            modePushToMute.IsChecked = false;
            modePushToTalk.IsChecked = false;
            ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_Toggle", true)[0]).Checked = false;
            ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_PushToTalk", true)[0]).Checked = false;
            ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_PushToMute", true)[0]).Checked = false;
            switch (mode)
            {
                case ACTION_MODE.TOGGLE:
                    modeToggle.IsChecked = true;
                    ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_Toggle", true)[0]).Checked = true;
                    break;
                case ACTION_MODE.PUSH_TO_TALK:
                    modePushToTalk.IsChecked = true;
                    ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_PushToTalk", true)[0]).Checked = true;
                    break;
                case ACTION_MODE.PUSH_TO_MUTE:
                    modePushToMute.IsChecked = true;
                    ((ToolStripMenuItem)tray.ContextMenuStrip.Items.Find("trayMenu_PushToMute", true)[0]).Checked = true;
                    break;
            }
        }

        private void TrayFlush()
        {
            if (mic.isMuteControlCaptureDevice()) tray.Icon = iconMuted;
            else tray.Icon = iconUnmuted;
        }

        private void OverlayFlush()
        {
            overlayWindow.SetMicStateImage(mic.isMuteControlCaptureDevice());
        }

        private async void UpdateCheck( bool notice = true )
        {
            try
            {
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                String version = ini.ReadValue("system", "version");

                var res = await client.GetAsync(baseUrl + "lastest_version");
                var updateInfo = await res.Content.ReadAsStringAsync();
                String[] info = updateInfo.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });
                String lastestVersion = info[0];
                bool isIniUpdate = System.Convert.ToBoolean(info[1]);
                Console.WriteLine(lastestVersion);

                if (isNew(version, lastestVersion))
                {
                    var answer = System.Windows.MessageBox.Show("最新版があります(ver"+ lastestVersion + ")。更新しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (answer == MessageBoxResult.Yes)
                    {
                        Update(lastestVersion, isIniUpdate);
                    }
                } else if ( notice )
                {
                    System.Windows.MessageBox.Show("更新はありません。", "通知", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


            bool isNew(string current, string target)
            {
                var ca = current.Split('.');
                var ta = target.Split('.');
                var len = Math.Min(ca.Length, ta.Length);

                for (var i = 0; i < len; i++)
                {
                    int ci, ti;
                    if (!int.TryParse(ca[i], out ci) | !int.TryParse(ta[i], out ti))
                    {
                        return false;
                    }

                    if (ci < ti)
                    {
                        return true;
                    }
                    if (ci > ti)
                    {
                        return false;
                    }
                }

                return ca.Length < ta.Length;
            }
        }

        private void Update( String lastestVersion, bool isIniUpdate )
        {
            void RenameOld( String path )
            {
                foreach (String fileName in Directory.GetFiles(path))
                {
                    Console.WriteLine(fileName);
                    //if (!fileName.Contains("setting.ini")) continue;
                    if (fileName.Contains("MicMuteControl.exe.config")) continue;
                    if (fileName.Contains("MicMuteControl.pdb")) continue;
                    File.Delete(fileName + ".old");
                    File.Move(fileName, fileName + ".old");
                }
                /*
                foreach (String subdirPath in Directory.GetDirectories(path))
                {
                    RenameOld(subdirPath);
                }
                */
            }

            RenameOld("./");
            
            //ダウンロード
            var zipPath = "./new.zip";
            File.Delete(zipPath);
            using (var wclient = new WebClient())
            {
                wclient.DownloadFile(baseUrl + lastestVersion + ".zip", zipPath);
            }
            using (ZipArchive za = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in za.Entries)
                {
                    //フォルダは飛ばす
                    if(entry.ExternalAttributes == 16) continue;
                    entry.ExtractToFile("./" + entry.Name, true);
                }
            }
            //ZipFile.ExtractToDirectory(zipPath, "./");
            File.Delete(zipPath);

            // iniファイルの内容継承
            if (isIniUpdate == false)
            {
                IniManager newIni = new IniManager("./setting.ini");
                newIni.Inherit("./setting.ini.old");
            }

            IniManager ini = new IniManager("./setting.ini");
            ini.WriteValue("system", "version", lastestVersion);

            if ( File.Exists(appName + ".exe") == false )
            {
                File.Move(appName + ".old", appName + ".exe");
            }

            var answer = System.Windows.MessageBox.Show("アップデートを完了するため終了します。もう一度起動してください", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            //Process.Start(appName + ".exe", "/up " + Process.GetCurrentProcess().Id);
            this.Close();
        }

        private void DeleteOldFiles()
        {
            void DeleteOld(String path)
            {
                foreach (String fileName in Directory.GetFiles(path))
                {
                    if (fileName.Contains(".old"))
                    {
                        File.Delete(fileName);
                    }
                }
                foreach (String subdirPath in Directory.GetDirectories(path))
                {
                    DeleteOld(subdirPath);
                }
            }

            /*
            if (Environment.CommandLine.IndexOf("/up", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                Console.WriteLine("up!!!");
                try
                {
                    string[] args = Environment.GetCommandLineArgs();
                    Console.WriteLine(args[2]);
                    int pid = Convert.ToInt32(args[2]);
                    Process.GetProcessById(pid).WaitForExit();    // 終了待ち
                }
                catch (Exception)
                {
                }

                DeleteOld("./");
            }
            */
            
            DeleteOld("./");
        }

        private bool isStartupEnabled()
        {
            string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                return (rKey.GetValue(Application.ProductName) != null) ? true : false;
            }
        }

        private Icon ConvertIcon(System.Drawing.Image img)
        {
            Icon icon;
            using (var msImg = new MemoryStream())
            using (var msIco = new MemoryStream())
            {
                img.Save(msImg, ImageFormat.Png);
                using (var bw = new BinaryWriter(msIco))
                {
                    bw.Write((short)0);           //0-1 reserved
                    bw.Write((short)1);           //2-3 image type, 1 = icon, 2 = cursor
                    bw.Write((short)1);           //4-5 number of images
                    bw.Write((byte)32);         //6 image width
                    bw.Write((byte)32);         //7 image height
                    bw.Write((byte)0);            //8 number of colors
                    bw.Write((byte)0);            //9 reserved
                    bw.Write((short)0);           //10-11 color planes
                    bw.Write((short)32);          //12-13 bits per pixel
                    bw.Write((int)msImg.Length);  //14-17 size of image data
                    bw.Write(22);                 //18-21 offset of image data
                    bw.Write(msImg.ToArray());    // write image data
                    bw.Flush();
                    bw.Seek(0, SeekOrigin.Begin);
                    icon = new Icon(msIco);
                }
            }
            return icon;
        }



        //////////////////////////////// 定期実行 /////////////////////////////////////////////

        public void SurveillanceMicVolume()
        {
            System.Windows.Threading.DispatcherTimer timerMicVol = new System.Windows.Threading.DispatcherTimer();
            timerMicVol.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timerMicVol.Tick += (sender, e) =>
            {
                SystemSettingReflect();
            };
            timerMicVol.Start();
        }


        //////////////////////////////// セッター、ゲッター /////////////////////////////////////////////
        
        public void setCancellationTokenSource(CancellationTokenSource s)
        {
            cancelTokenSource = s;
        }

        public void setModKey(MOD_KEY modKey)
        {
            this.modKey = modKey;
        }

        public void setTriggerKey(String triggerKey)
        {
            this.triggerKey = triggerKey;
        }

    }

}
