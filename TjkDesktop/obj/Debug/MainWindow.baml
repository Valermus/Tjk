   M S B A M L   `   `   `  ����  
TjkDesktop    TjkDesktop.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$	  TJK��5   	   $	��360��6   $	��542��6"   .��5      �  � ColumnDefinitions 5   
   �� 5      $��0* �6   �� 5	      .+�5   
   (  "System.Windows.Controls.DatePicker  -   5          Name$ dtPicker��5      $
��Left=�6'   $��
193,27,0,0q�6B   $	��Top=�6V   $��1��6n   $��24��6~   $	��125��6�      FirstDayOfWeek$ Monday=�6�      Text$ Gün Seçin��6�      DatePickerWithoutWaterMark#���   6�   6�   �� 5   
   -   5         ��Name$ btnHipodrom��5      $��2��6&   $��Hipodromları Getir��6:   $
��Left=�6W   $��
193,76,0,0q�6r   $	��Top=�6�   $	��125��6�   6�   �� 5   
   -   5        $ btnYarisProg��5      $��2��6'   $ ��Yarış Programını Getir��6;   $
��Left=�6\   $��193,103,0,0q�6w   $	��Top=�6�   $	��125��6�   6�   �� 5   
   -   5        $ btnYarisSonuc��5      $��2��6(   $"��Yarış Sonuçlarını Getir��6<   $
��Left=�6^   $��193,157,0,0q�6y   $	��Top=�6�   $	��125��6�   6�   �� 5   
   -   5         ��Name$ 	lblResult��5      $��2��6#   $�� ��67   $��Center=�6B   $��52,261,34,41q�6_   $��Center=�6u    J� RenderTransformOrigin$ 0.447,-1.962&�6�   $��28��6�   $	��448��6�   $��Center=�6�   �� 5   
   -   5        $ lblHipodrom��5      $��2��6%   $�� ��69   $
��Left=�6D   $��
342,72,0,0q�6_   $	��Top=�6s   $	��126��6�   �� 5   
   -   5        $ lblKosu��5      $��2��6!   $�� ��65   $
��Left=�6@   $��342,103,0,0q�6[   $	��Top=�6p   $	��126��6�   �� 5   
   -	   5        $ lblAt��5      $��2��6   $�� ��63   $
��Left=�6>   $��342,142,0,0q�6Y   $	��Top=�6n   $	��126��6�   �� 5   
   -
   5        $ btnHorseDetails��5      $��2��6*   $��At Detaylarını Getir��6>   $
��Left=�6]   $��193,130,0,0q�6x   $	��Top=�6�   $	��125��6�   6�   � 5   
   -   5         �Name$ progBar��5      $��2��6'   $��41��6;   $��	0,289,0,0q�6G   $	��Top=�6Z   $y�Visible=�6r   �� 5   
   -   5        	 ��Name$	 pbTextBlock��5      $��Center=�6n   $��Center=�6�   $��40��6�   $��2��6�   $��230,294,247,10q�6�   ��6)   �� *Value+
 �� ElementName$
 progBar�� �� StringFormat$ {0:0.0}%��6�   �� 5   
   -   5        $ 	lblStatus��5      $��2��6#   $�� ��67   $
��Left=�6B   $��151,230,0,0q�6]   $	��Top=�6r   $	��251��6�   $��Segoe UI Semibold)�6�   $
��Bold"�6�   &   System.Windows.Media.TextOptions   TextHintingMode$ Animated=�6�   $��Center=�6�   $��Center=�6  5      5      