   M S B A M L   `   `   `  ����  EPriceProviderServices'    !EPriceProviderServices.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$,  &Provider Services управление��5   	   $	��620��6.   $
��1000��6;   $	��620��6H   $
��1000��6X    � 	Resources 5         TrayMenu��%_   &	        �� 5   
   .@�5      m� -   5        $��
Выйти��5      6&   5      5	      .��5
      � $��0,0,0,0q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   6,   �� 5      6   .+�5   
   ��  �� Orientation$ 
Horizontal=�6   $��2,2,0,2q�6.   .+�5      �� $��Статус:��6   $
��Bold"�6'   $
��Left=�69   $��0,0,0,0q�6T   $	��Top=�6e   �� 5      -   5         ��Name$ 	txtStatus��5      $
��Left=�6+   $��2,0,0,0q�6F   $
��Wrap=�6W   $	��Top=�6k   5      �� 5   
   $��1��6   .@�5      �� $��Каталог��6   .��5      � ���	#FFE5E5E5$��0,0,0,0q�6.   $��Stretch=�6?   $��Stretch=�6]    � ColumnDefinitions 5      �� 5      $��0.44* �6+   6;   �� 5      $��0.02* �6+   6;   �� 5      $��0.44* �6+   6;   .+�5      �  5      �� 5      $
��Auto �6,   6<   �� 5      $
��Auto �6,   6<   �� 5      6.   .+�5!      �� $��Каталог UN1T��6    $
��Bold"�67   $
��Left=�6I   $��0,0,0,0q�6d   $	��Top=�6u   $
��Auto��6�   �� 5"      $��1��6%   $��5,3,0,3q�62   $ 
Horizontal=�6C   .+�5#      �� -   5         ��Name$ btnNewUn1tCategory��5#   %   $��5,0,5,0q�6A   .��6s   Создать6|   �� 5$      -   5        $ btnEditUn1tCategory��5$   %   $��5,0,5,0q�6B   $��22��6S   $��Bottom=�6_   .��6�   Изменить6�   �� 5%      -   5        $ btnDeleteUn1tCategory��5%   %   $��5,0,5,0q�6D   .��6y   Удалить6�   d� 5&      -   5         d�Name$ btnToggleCategoryView��5&   +   $��5,0,5,0q�6J   .��6   #!Показать привязки6�   5'      R� 5(      -   5         R�Name$ Un1tCategoriesView��5(   0   $��2��6#   $��Stretch=�6�   $��Stretch=�6�   $��Stretch=�6  $��Stretch=�6   6>  5)      � 5*      $��2��6    5+      �� 5,      $
��Auto �6,   6<   �� 5-      6.   .+�5/      �� $-��'Каталоги поставщиков��6    $
��Bold"�6?   $
��Left=�6Q   $��0,0,0,0q�6l   $	��Top=�6}   $
��Auto��6�   �� 50      -	   5        	 ��Name$ 	 ProviderCategoryTabControl��50   %   $��1��6I   $��Stretch=�6V   $��0,0,0,0q�6t   $��Stretch=�6�   $
��Auto��6�   $��Stretch=�6�   $��Stretch=�6�   .@�51      �� $��Merlion��6&   $��Stretch=�67   $��Stretch=�6U   $��Stretch=�6q   $��Stretch=�6�   .��52   "   R� -
   5        $ MerlionCategoriesView��52   +   $��Stretch=�6J   $��Stretch=�6o   $��Stretch=�6�   $��Stretch=�6�   53      �� 54      $��Treolan��6&   $��Stretch=�67   $��Stretch=�6\   $��Stretch=�6   $��Stretch=�6�   .��55   "   R� -   5        $ TreolanCategoriesView��55   +   $��Stretch=�6J   $��Stretch=�6o   $��Stretch=�6�   $��Stretch=�6�   56      �� 57      $	��OCS��6&   $��Stretch=�63   $��Stretch=�6X   $��Stretch=�6{   $��Stretch=�6�   .��58   "   R� -   5        $ OcsCategoriesView��58   +   $��Stretch=�6F   $��Stretch=�6k   $��Stretch=�6�   $��Stretch=�6�   59      �� 5:      $
��OLDI��6&   $��Stretch=�64   $��Stretch=�6Y   $��Stretch=�6|   $��Stretch=�6�   .��5;   "   R� -   5        $ OldiCategoriesView��5;   +   $��Stretch=�6G   $��Stretch=�6l   $��Stretch=�6�   $��Stretch=�6�   5<      5=      5>      5?      5@      �� 5A      $��Управление��6   .��5B      �� $ 
Horizontal=�6   .+�5C      �� -   5        
 ��Name$
 LoadProductsManagePanel��5C   !   $ Vertical=�6B   $
��Auto��6Y   $��	10,5,10,5q�6f   $	��Top=�6y   .+�5D      �� -   5         ��Name$ AutoManagePanel��5D   !   
���   �$��1q�6N   $��	10,5,10,5q�6b   ._�5E      �� $ Vertical=�6)   $	��300��6@   $	��Top=�6L   .+�5F   "   �� $��Medium"�6(   .��6<   ,*Загрузка по расписанию6T   �� 5G   "   -   5         ��Name$ IsLoadPropertiesAuto��5G   +   $��	10,5,10,5q�6I   $5��/Загружать характеристики��6\    d� IsThreeState .   d� 	IsChecked$
 TrueY�6�   �� 5H   "   $ 
Horizontal=�6-   $��	10,0,10,5q�6F   .+�5I   &   �� .��6,   #!Период обновления6?   �� 5J   &   -   5         ��Name$ AutoUpdatePeriod��5J   /   $��20��6I   $��90��6U   $��5,0,0,0q�6`   @�5K   *   �� 5L   .   $��1 час��6;   
 � Tag$ 60��6K   6V   �� 5M   .   $��
2 часа��6;   $	 120��6L   6X   �� 5N   .   $��
3 часа��6;   $	 180��6L    �� 
IsSelected . 6j   �� 5O   .   $��6 часов��6;   $	 360��6M   6Y   �� 5P   .   $��9 часов��6;   $	 540��6M   6Y   �� 5Q   .   $��12 часов��6;   $	 720��6N   6Z   �� 5R   .   $��18 часов��6;   $
 1080��6N   6[   �� 5S   .   $��24 часа��6;   $
 1440��6M   6Z   5U   '   5V   #   �� 5W   "   -   5        $ btnStartAutoLoader��5W   )   $��Center=�6E   $��2,0,0,6q�6b   $��34��6s   $��70��6   .��5X   &   �� $ 
Horizontal=�61   $
��Auto��6J   .+�5Y   *   �� $
��Bold"�60   $��Center=�6B   .��6]   
Старт6d   � 5Z   *    � Points% �   ��  � ��� ���� 
��� � �
���   �$��5,0,5,0q�6k   $��Center=�6|   6�   5[   '   5\   #   �� 5]   "   -   5        $ btnStopAutoLoader��5]   )   $��Center=�6D   $��2,0,0,6q�6a   $��34��6r   $��70��6~   $y�	Collapsed=�6�   .��5^   &   �� $ 
Horizontal=�61   $
��Auto��6J   .+�5_   *   �� $
��Bold"�60   $��Center=�6B   .��6]   
Стоп6c   A� 5`   *   $��16��62   $��16��6=   
���  ��
���   �$��Center=�6d   $��5,0,5,0q�6   6�   5a   '   5b   #   5c      5d      �� 5e      -   5        $ ManualManagePanel��5e   !   
���   �$��1q�6P   $��	10,5,10,5q�6d   ._�5f      �� $ Vertical=�6)   $	��300��6@   $	��Top=�6L   .+�5g   "   �� $��Medium"�6(   .��6<   Вручную6E   �� 5h   "   -   5        $ IsLoadProperties��5h   +   $��	10,5,10,5q�6E   $5��/Загружать характеристики��6X    .  $
 TrueY�6�   �� 5i   "   -   5        $) #btnGetAllProviderProductsAndPricies��5i   )   $��	10,5,10,5q�6V   $C��=Получить данные всех поставщиков��6�   �� 5j   "   -   5        $ btnGetAllProviderCatalogs��5j   )   $��	10,5,10,5q�6L   $Q��KПерезагрузить каталоги всех поставщиков��6�   5k      5l      �� 5m      
���   �$��1q�65   $��	10,5,10,5q�6I   ._�5n      �� $ Vertical=�6)   $	��300��6@   $	��Top=�6L   $��5q�6d   .+�5o   "   �� $��Medium"�6(   .��6<   42Последняя загрузка товаров6X   �� 5p   "   -   5        $ txtMerlionProductsCount��5p   ,   $��
Merlion: 0��6M   6a   �� 5q   "   -   5        $ txtTreolanProductsCount��5q   ,   $��
Treolan: 0��6M   6a   �� 5r   "   -   5        $ txtOcsProductsCount��5r   ,   $��OCS: 0��6I   6Y   �� 5s   "   -   5        $ txtOldiProductsCount��5s   ,   $��OLDI: 0��6J   6[   �� 5t   "   -   5        $ txtTotalProductsCount��5t   ,   $��Всего: 0��6K   6]   5u      5v      �� 5w      -   5        $ PropertyPanel��5w   !   
���   �$��1q�6L   $��	10,5,10,5q�6`   $y�	Collapsed=�6s   ._�5x      �� $ Vertical=�6)   $	��300��6@   $	��Top=�6L   .+�5y   "   �� $��Medium"�6(   .��6<   -+Загрузка характеристик6T   �� 5z   "   -   5         ��Name$ PropertiesLoadCount��5z   (   .��6E   Осталось:6Q   � 5{   "   -   5         �Name$ PropertyLoadProgress��5{   .    � Minimum$ 0Q�6L   $��	10,5,10,5q�6X   $��20��6k   5|      5}      5~      �� 5      -    5         ��Name$ lstProductsLog��5      $��
10,10,10,5q�67   $	��600��6K   $
��Auto��6W   $��Stretch=�6e   $��Stretch=�6�   6�   5�      5�      5�      5�      5�      