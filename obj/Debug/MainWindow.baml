   M S B A M L   `   `   `  ����I  EMicMuteControl, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'clr-namespace:MicControl
MicControl  5   	   [ WSystem.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089Y@clr-namespace:System.Windows.Forms;assembly=System.Windows.FormsSystem.Windows.Forms 5   	       MicControl.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   *localclr-namespace:MicControl     5   	   RForms@clr-namespace:System.Windows.Forms;assembly=System.Windows.Forms     5   	   -   5          � Title$  MicMuteControl��5
   	   $	��200��6    $	��400��6-   $	��400��69   $	��200��6H    � 
ResizeMode$ CanMinimize=�6X    � Icon$ /unmuted.ico��6�   .��5      �  � ColumnDefinitions 5   
   �� 5      $	��75* �6   �� 5      $	��23* �6   .+�5   
   �� -   5         ��Name$ captureDevices��5      $
��Left=�6+   $��	10,49,0,0q�6F   $	��Top=�6Y   $	��364��6q   $��2��6�   �� 5   
   $��録音デバイス��6   $
��Left=�6!   $��	10,23,0,0q�6<   $	��Top=�6O   o� 5   
   $��0q�6   $��18��6   $	��Top=�6)   $��2��6A   .@�5      m� $��ファイル��6   .@�5      m� -   5        $��リフレッシュ��5      6+   m� 5      -   5        $��	閉じる��5      6(   5      m� 5      $��	モード��6   .@�5      m� -   5         m�Name$ 
modeToggle��5      $��	トグル��6/    m� IsCheckable . 6O   m� 5      -   5        $ modePushToTalk��5      $$��プッシュトゥートーク��63    . 6Z   m� 5      -   5        $ modePushToMute��5      $$��プッシュトゥミュート��63    . 6Z   5      m� 5      $��設定��6   .@�5      m� -   5        $��ホットキー...��5      6-   m� 5      -	   5        $ 
gamingMode��5      $��ゲーミングモード��6/    .  6U   m� 5      -
   5        $ overlayIcon��5      $$��オーバーレイアイコン��60   5       m� 5!      -   5        $ trayIcon��5!      $��トレイアイコン��6-   5"      �� 5#      m� 5$      -   5        $ overlayMutedVisibleToggle��5$      $=��7オーバーレイ ミュート時アイコン非表示��6>    . 6n   �� 5%      m� 5&      -   5        $ settingStartup��5&      $��起動時に実行��63    .  6W   5'      m� 5(      $��	ヘルプ��6   .@�5)      m� -   5        $��更新の確認...��5)      6-   m� 5*      -   5        $ MenuItem_Help_about��5*      $*��$起動時に更新の確認をする��68   6N   �� 5+      m� 5,      -   5        $��about...��5,      6-   5-      5.      � 5/   
   $
��Left=�6   $��64��6*   $��
145,85,0,0q�66   $	��Top=�6J   $	��220��6b   
�������$��2��6�   .+�50      �� -   5         ��Name$ VolumeSlider��50      $
��Left=�6+   $��	10,31,0,0q�6F   $	��Top=�6Y   $	��200��6q    � SmallChange$ 1Q�6}   	 � Maximum$		 100Q�6�   
 �� TickFrequency$
 10Q�6�    �� TickPlacement$ BottomRight=�6�   �� 51      $��音量��6   $
��Left=�6!   $��84,5,0,0q�6<   $	��Top=�6N   �� 52      -   5         ��Name$ LabelVolume��52      $��
��6)   $
��Left=�6>   $��	118,5,0,0q�6Y   $	��Top=�6l   53      �� 54   
   $ ��オーバーレイ表示
��6   $
��Left=�6-   $��	10,76,0,0q�6H   $	��Top=�6[   $��26��6s   	� 55   
   -   5         	�Name$ Radio_OverlayOn��55      $��On��6/   $
��Left=�6<   $��
26,102,0,0q�6W   $	��Top=�6k    	� 	GroupName$ OverlayOnOff��6�   6�   	� 56   
   -   5        $ Radio_OverlayOff��56      $	��Off��60   $
��Left=�6>   $��
83,102,0,0q�6Y   $	��Top=�6m    J� RenderTransformOrigin$ 	2.229,0.6&�6�   $ OverlayOnOff��6�   6�   �� 57   
   -   5         ��Name$ Button_OverrayAdjust��57      $��調整��6/   $
��Left=�6<   $��
10,129,0,0q�6W   $	��Top=�6k   $��51��6�   6�   �� 58   
   -   5        $ Button_OverrayReset��58      $��位置リセット��6.   $
��Left=�6?   $��
66,129,0,0q�6Z   $	��Top=�6n   $��69��6�   6�   5:      5;      