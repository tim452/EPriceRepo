   M S B A M L   `   `   `  ����  EPriceViewer    EPriceViewer.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  UN1T Viewer��5   	   $	��630��6   $
��1080��6*    � 	Resources 5         transparentListViewStyle �� 	Resources �� Color��%U  &	        �� 5   
    5      ���� 5      $ Transparent��6X   (��       6"   *$"SystemColors.HighlightTextBrushKey+)6X   �� 5	      $ Transparent��6T   (��       6"   * SystemColors.HighlightBrushKey+)6T   �� 5
      $ Transparent��6R   (��       6"   *SystemColors.ControlBrushKey+)6R   5      5      5      .��5      �  � ColumnDefinitions 5   
   �� 5      $
��Auto �6   6.   �� 5      $
��Auto �6   6.   .+�5   
   ��  �� Orientation$ Vertical=�6   .+�5      �� $��Каталог UN1T��6   $��Medium"�6+   5      R� 5      -   5         R�Name$ lstCatalogs��5      $��3,5,3,5q�6,   $	��360��6=   $	��560��6I   5      5      �� 5   
   $��1��6   $ Vertical=�6%   .+�5      �� $ 
Horizontal=�6   .+�5      �� $��Товары��6   $��Medium"�6)   5      �� 5      -   5         ��Name$ IsLoadNotNullStock��5   u   $ ��Есть на складе��6   $��Center=�64   $��5,0,0,0q�6O    d� IsThreeState .  6�   �� 5      $��Сортировка��6   $��Medium"�6-   5      �� 5       -   5        	 ��Name$	 ctrlSort��5       $	��130��6-   $��20��69   @�5!      �� 5"      $��Парт номер��6'   

 � Tag$
 1��6<    �� 
IsSelected . 6X   �� 5#      $��Наименование��6'   $
 2��6>   6H   5%      5&      �� 5'      -   5         ��Name$ lstProducts��5'      $	��560��6,    �� SelectionMode$ Extended=�69   $��3,5,3,5q�6R   $	��660��6c   #��[  6o   6�   5(      5)      5*      