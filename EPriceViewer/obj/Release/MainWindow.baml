   M S B A M L   `   `   `  ����   EPriceViewer    EPriceViewer.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  UN1T Viewer��$	��630��$
��1080�� � 	Resources    transparentListViewStyle �� 	Resources �� Color��%�   &	        ��  ���� $ Transparent��(��       *$"SystemColors.HighlightTextBrushKey+)�� $ Transparent��(��       * SystemColors.HighlightBrushKey+)�� $ Transparent��(��       *SystemColors.ControlBrushKey+).���  � ColumnDefinitions �� $
��Auto ��� $
��Auto �.+���  �� Orientation$ Vertical=�.+��� $��Каталог UN1T��$��Medium"�R� -    R�Name$ lstCatalogs��$��3,5,3,5q�$	��360��$	��560���� $��1��$ Vertical=�.+��� $ 
Horizontal=�.+��� $��Товары��$��Medium"��� -    ��Name$ IsLoadNotNullStock��$ ��Есть на складе��$��Center=�$��5,0,0,0q� d� IsThreeState .  �� $��Сортировка��$��Medium"��� -   	 ��Name$	 ctrlSort��$	��130��$��20��@��� $��Парт номер��

 � Tag$
 1�� �� 
IsSelected . �� $��Наименование��$
 2���� -    ��Name$ lstProducts��$	��560�� �� SelectionMode$ Extended=�$��3,5,3,5q�$	��660��#��[  