   M S B A M L   `   `   `  ���� I  EMicMuteControl, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:MicControl
MicControl  [ WSystem.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089Y@clr-namespace:System.Windows.Forms;assembly=System.Windows.FormsSystem.Windows.Forms     MicControl.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     *localclr-namespace:MicControl     RForms@clr-namespace:System.Windows.Forms;assembly=System.Windows.Forms     -     � Title$  MicMuteControl��$	��200��$	��400��$	��400��$	��200�� � 
ResizeMode$ CanMinimize=� � Icon$ /unmuted.ico��.���  � ColumnDefinitions �� $	��75* ��� $	��23* �.+��� -    ��Name$ captureDevices��$
��Left=�$��	10,49,0,0q�$	��Top=�$	��364��$��2���� $��録音デバイス��$
��Left=�$��	10,23,0,0q�$	��Top=�o� $��0q�$��18��$	��Top=�$��2��.@�m� $��ファイル��.@�m� -   $��リフレッシュ��m� -   $��	閉じる��m� $��	モード��.@�m� -    m�Name$ 
modeToggle��$��	トグル�� m� IsCheckable . m� -   $ modePushToTalk��$$��プッシュトゥートーク�� . m� -   $ modePushToMute��$$��プッシュトゥミュート�� . m� $��設定��.@�m� -   $��ホットキー...��m� -	   $ 
gamingMode��$��ゲーミングモード�� .  m� -
   $ overlayIcon��$$��オーバーレイアイコン��m� -   $ trayIcon��$��トレイアイコン���� m� -   $ overlayMutedVisibleToggle��$=��7オーバーレイ ミュート時アイコン非表示�� . �� m� -   $ settingStartup��$��起動時に実行�� .  m� $��	ヘルプ��.@�m� -   $��更新の確認...��m� -   $ MenuItem_Help_about��$*��$起動時に更新の確認をする���� m� -   $��about...��� $
��Left=�$��64��$��
145,85,0,0q�$	��Top=�$	��220��
�������$��2��.+��� -    ��Name$ VolumeSlider��$
��Left=�$��	10,31,0,0q�$	��Top=�$	��200�� � SmallChange$ 1Q�	 � Maximum$		 100Q�
 �� TickFrequency$
 10Q� �� TickPlacement$ BottomRight=��� $��音量��$
��Left=�$��84,5,0,0q�$	��Top=��� -    ��Name$ LabelVolume��$��
��$
��Left=�$��	118,5,0,0q�$	��Top=��� $ ��オーバーレイ表示
��$
��Left=�$��	10,76,0,0q�$	��Top=�$��26��	� -    	�Name$ Radio_OverlayOn��$��On��$
��Left=�$��
26,102,0,0q�$	��Top=� 	� 	GroupName$ OverlayOnOff��	� -   $ Radio_OverlayOff��$	��Off��$
��Left=�$��
83,102,0,0q�$	��Top=� J� RenderTransformOrigin$ 	2.229,0.6&�$ OverlayOnOff���� -    ��Name$ Button_OverrayAdjust��$��調整��$
��Left=�$��
10,129,0,0q�$	��Top=�$��51���� -   $ Button_OverrayReset��$��位置リセット��$
��Left=�$��
66,129,0,0q�$	��Top=�$��69��