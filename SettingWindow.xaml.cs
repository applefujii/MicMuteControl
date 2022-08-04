using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MicControl
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {

        private MainWindow mainWindow;
        private IniManager ini;

        private bool inputMode = false;

        public SettingWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            mainWindow.HotkeyDispose();
            ini = new IniManager("./setting.ini");
            int modKey = ini.ReadValueInt("hotkey", "modifire_key");
            String triggerKey = ini.ReadValue("hotkey", "trigger_key");
            String modStr = "";
            if( (modKey & (int)MOD_KEY.SHIFT) != 0 ) modStr += "Shift, ";
            if ((modKey & (int)MOD_KEY.CONTROL) != 0) modStr += "Control, ";
            if ((modKey & (int)MOD_KEY.ALT) != 0) modStr += "Alt, ";
            if (modKey == 0) modStr = "None";
            TextBox_Hotkey.Text = modStr + " + " + triggerKey;

            Closed += delegate (object sender, EventArgs e) {
                mainWindow.HotkeyRegistration();
            };
        }

        private void Win_KeyScan(object sender, KeyEventArgs e)
        {
            if( inputMode )
            {
                Key k = e.Key;
                if (k == Key.System) k = Key.None;
                if (k >= (Key)116  &&  k <= (Key)121) k = Key.None;     //修飾キー(LShift～RAlt)なら発火キーをNoneにする
                if (e.SystemKey != Key.None  && e.SystemKey != Key.LeftAlt) k = e.SystemKey;        //Altキーと他のキーを押すとシステムキー扱いになるのでそれを修飾キーに代入
                TextBox_Hotkey.Text = Keyboard.Modifiers.ToString() + " + " + k.ToString();
                if( e.IsDown  &&  !e.IsRepeat )
                {
                    if (k == Key.None) return;
                    inputMode = false;
                    Button_Scan.Content = "スキャン開始";
                }
            }
        }

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            inputMode = !inputMode;
            if(inputMode) Button_Scan.Content = "スキャン中";
            else Button_Scan.Content = "スキャン開始";
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            int code = 0x00;
            String[] keys = TextBox_Hotkey.Text.Split('+');
            String tKey = keys[1].Trim();
            String[] modKeys = keys[0].Split(',');
            if (tKey == "None")
            {
                System.Windows.Forms.MessageBox.Show("発火キーを指定してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            foreach ( String mk in modKeys)
            {
                switch ( mk.Trim() )
                {
                    case "Shift":
                        code |= (int)MOD_KEY.SHIFT;
                        break;
                    case "Control":
                        code |= (int)MOD_KEY.CONTROL;
                        break;
                    case "Alt":
                        code |= (int)MOD_KEY.ALT;
                        break;
                }
            }
            ini.WriteValue("hotkey", "modifire_key", "0x" + code.ToString("X2"));
            ini.WriteValue("hotkey", "trigger_key", tKey);
            mainWindow.setModKey((MOD_KEY)code);
            mainWindow.setTriggerKey(tKey);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
