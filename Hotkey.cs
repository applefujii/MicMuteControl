using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;


namespace MicControl
{

    /// <summary>
    /// グローバルホットキーを登録するクラス。
    /// 使用後は必ずDisposeすること。
    /// </summary>
    public class Hotkey : IDisposable
    {
        HotKeyForm form;
        Key key;
        MOD_KEY modKey;
        /// <summary>
        /// ホットキーが押されると発生する。
        /// </summary>
        public event EventHandler HotKeyPush;
        public event EventHandler HotKeyRelease;

        /// <summary>
        /// ホットキーを指定して初期化する。
        /// 使用後は必ずDisposeすること。
        /// </summary>
        /// <param name="modKey">修飾キー</param>
        /// <param name="key">キー</param>
        public Hotkey(MOD_KEY modKey, Keys key)
        {
            form = new HotKeyForm(modKey, key, RaiseHotKeyPush, RaiseHotKeyRelease);
            KeyConverter con = new KeyConverter();
            this.key = (Key)con.ConvertFrom(key.ToString());
            this.modKey = modKey;
        }

        private void RaiseHotKeyPush()
        {
            if (HotKeyPush != null)
            {
                HotKeyPush(this, EventArgs.Empty);
            }
        }

        private void RaiseHotKeyRelease()
        {
            if (HotKeyRelease != null)
            {
                HotKeyRelease(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            form.Close();
        }

        public Key getKey()
        {
            return key;
        }

        public MOD_KEY getModKey()
        {
            return modKey;
        }

        private class HotKeyForm : Form
        {
            KeyState keyState;

            [DllImport("user32.dll")]
            extern static int RegisterHotKey(IntPtr HWnd, int ID, MOD_KEY MOD_KEY, Keys KEY);

            [DllImport("user32.dll")]
            extern static int UnregisterHotKey(IntPtr HWnd, int ID);

            // HotKeyのイベントを示すメッセージID
            const int WM_HOTKEY = 0x0312;
            int id = -1;
            ThreadStart procPush;

            public HotKeyForm(MOD_KEY modKey, Keys key, ThreadStart procPush, ThreadStart procRelease)
            {
                keyState = new KeyState(key, procRelease);
                this.procPush = procPush;

                Random rdm = new System.Random();
                id = rdm.Next(1, 1000);
                if (RegisterHotKey(this.Handle, id, modKey, key) == 0)
                {
                    System.Windows.MessageBox.Show("ホットキーが登録できませんでした。", "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    throw new Exception("ホットキーが登録できませんでした。");
                }

                // ホットキー開放
                Closed += delegate (object sender, EventArgs e)
                {
                    UnregisterHotKey(this.Handle, id);
                };

                keyState.Surveillance();
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == WM_HOTKEY)
                {
                    if ((int)m.WParam == id)
                    {
                        if (keyState.isFirstPress())
                        {
                            procPush();
                            keyState.setExecuting(true);
                        }
                    }
                }
            }




            private class KeyState
            {
                //登録したキー
                Key key;
                bool state = false;
                bool executing = false;
                ThreadStart procRelease;

                public KeyState(Keys key, ThreadStart procRelease)
                {
                    KeyConverter con = new KeyConverter();
                    this.key = (Key)con.ConvertFrom(key.ToString());
                    this.procRelease = procRelease;
                }

                public void Surveillance()
                {
                    System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
                    timer.Tick += (sender, e) =>
                    {
                        bool newState = Keyboard.IsKeyDown(key);
                        if (executing == true && newState == false && state == true)
                        {
                            procRelease();
                            executing = false;
                        }
                        state = newState;
                    };
                    timer.Start();
                }

                private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
                {
                    throw new NotImplementedException();
                }

                public bool isFirstPress()
                {
                    bool newState = Keyboard.IsKeyDown(key);
                    if (newState == true && state == false)
                    {
                        state = true;
                        return true;
                    }
                    else return false;
                }

                public void setExecuting(bool state)
                {
                    executing = state;
                }

            }
        }
    }

    /// <summary>
    /// HotKeyクラスの初期化時に指定する修飾キー
    /// </summary>
    public enum MOD_KEY : int
    {
        NONE = 0x0000,
        ALT = 0x0001,
        CONTROL = 0x0002,
        SHIFT = 0x0004
    }

}
