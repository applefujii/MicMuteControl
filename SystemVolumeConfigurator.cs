using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi.CoreAudio;

namespace MicControl
{
    class SystemVolumeConfigurator
    {
        private CoreAudioDevice controlCaptureDevice;
        private CoreAudioDevice defaultCaptureDevice;
        private IEnumerable<CoreAudioDevice> aCaptureDevice;

        public SystemVolumeConfigurator()
        {
            //サウンドの録音デバイスを取得
            RefleshCaptureDevice();

            controlCaptureDevice = defaultCaptureDevice;
        }

        public bool ToggleMicMute()
        {
            return controlCaptureDevice.Mute( !controlCaptureDevice.IsMuted );
        }

        public bool SetMicMute()
        {
            return controlCaptureDevice.Mute(true);
        }

        public bool SetMicUnmute()
        {
            return controlCaptureDevice.Mute(false);
        }

        public void RefleshCaptureDevice()
        {
            //System.Threading.Thread.Sleep(5000);
            //サウンドの既定の録音デバイスを取得
            defaultCaptureDevice = new CoreAudioController().DefaultCaptureDevice;
            //サウンドの録音一覧を取得
            aCaptureDevice = new CoreAudioController().GetCaptureDevices();
        }

        public async Task RefleshCaptureDeviceAsync()
        {
            await Task.Run(() =>
            {
                RefleshCaptureDevice();
            });
        }



        //////////////////////////////// ゲッター、セッター /////////////////////////////////////////////

        public String getDefaultCaptureDevice()
        {
            return defaultCaptureDevice.FullName;
        }

        public List<String> getCaptureDevices()
        {
            List<String> list = new List<string>();
            foreach (CoreAudioDevice capDevice in aCaptureDevice)
            {
                list.Add(capDevice.FullName);
            }
            list.Reverse();
            return list;
        }

        public void setControlCaptureDevice( String capDeviceName )
        {
            foreach (CoreAudioDevice capDevice in aCaptureDevice)
            {
                if( capDevice.FullName == capDeviceName)
                {
                    controlCaptureDevice = capDevice;
                }
            }
        }

        public void setControlCapturDeviceVolume( double vol )
        {
            controlCaptureDevice.Volume = vol;
        }

        public double getControlCapturDeviceVolume()
        {
            return controlCaptureDevice.Volume;
        }

        public bool isMuteControlCaptureDevice()
        {
            return controlCaptureDevice.IsMuted;
        }

    }
}
