   M S B A M L   `   `   `  ����   EPriceProviderServices'    !EPriceProviderServices.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$,  &Provider Services управление��$	��620��$
��1000��$	��620��$
��1000�� � 	Resources    TrayMenu��%-   &	        �� .@�m� -   $��
Выйти��.��� $��0,0,0,0q� � RowDefinitions �� $
��Auto ��� .+���  �� Orientation$ 
Horizontal=�$��2,2,0,2q�.+��� $��Статус:��$
��Bold"�$
��Left=�$��0,0,0,0q�$	��Top=��� -    ��Name$ 	txtStatus��$
��Left=�$��2,0,0,0q�$
��Wrap=�$	��Top=��� $��1��.@��� $��Каталог��.��� ���	#FFE5E5E5$��0,0,0,0q�$��Stretch=�$��Stretch=� � ColumnDefinitions �� $��0.44* ��� $��0.02* ��� $��0.44* �.+��  �� $
��Auto ��� $
��Auto ��� .+��� $��Каталог UN1T��$
��Bold"�$
��Left=�$��0,0,0,0q�$	��Top=�$
��Auto���� $��1��$��5,3,0,3q�$ 
Horizontal=�.+��� -    ��Name$ btnNewUn1tCategory��$��5,0,5,0q�.��Создать�� -   $ btnEditUn1tCategory��$��5,0,5,0q�$��22��$��Bottom=�.��Изменить�� -   $ btnDeleteUn1tCategory��$��5,0,5,0q�.��Удалитьd� -    d�Name$ btnToggleCategoryView��$��5,0,5,0q�.��#!Показать привязкиR� -    R�Name$ Un1tCategoriesView��$��2��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=�� $��2�� �� $
��Auto ��� .+��� $-��'Каталоги поставщиков��$
��Bold"�$
��Left=�$��0,0,0,0q�$	��Top=�$
��Auto���� -	   	 ��Name$ 	 ProviderCategoryTabControl��$��1��$��Stretch=�$��0,0,0,0q�$��Stretch=�$
��Auto��$��Stretch=�$��Stretch=�.@��� $��Merlion��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=�.��R� -
   $ MerlionCategoriesView��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=��� $��Treolan��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=�.��R� -   $ TreolanCategoriesView��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=��� $	��OCS��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=�.��R� -   $ OcsCategoriesView��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=��� $
��OLDI��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=�.��R� -   $ OldiCategoriesView��$��Stretch=�$��Stretch=�$��Stretch=�$��Stretch=��� $��Управление��.���� $ 
Horizontal=�.+��� -   
 ��Name$
 LoadProductsManagePanel��$ Vertical=�$
��Auto��$��	10,5,10,5q�$	��Top=�.+��� -    ��Name$ AutoManagePanel��
���   �$��1q�$��	10,5,10,5q�._��� $ Vertical=�$	��300��$	��Top=�.+��� $��Medium"�.��,*Загрузка по расписанию�� -    ��Name$ IsLoadPropertiesAuto��$��	10,5,10,5q�$5��/Загружать характеристики�� d� IsThreeState .   d� 	IsChecked$
 TrueY��� $ 
Horizontal=�$��	10,0,10,5q�.+��� .��#!Период обновления�� -    ��Name$ AutoUpdatePeriod��$��20��$��90��$��5,0,0,0q�@��� $��1 час��
 � Tag$ 60���� $��
2 часа��$	 120���� $��
3 часа��$	 180�� �� 
IsSelected . �� $��6 часов��$	 360���� $��9 часов��$	 540���� $��12 часов��$	 720���� $��18 часов��$
 1080���� $��24 часа��$
 1440���� -   $ btnStartAutoLoader��$��Center=�$��2,0,0,6q�$��34��$��70��.���� $ 
Horizontal=�$
��Auto��.+��� $
��Bold"�$��Center=�.��
Старт�  � Points% �   ��  � ��� ���� 
��� � �
���   �$��5,0,5,0q�$��Center=��� -   $ btnStopAutoLoader��$��Center=�$��2,0,0,6q�$��34��$��70��$y�	Collapsed=�.���� $ 
Horizontal=�$
��Auto��.+��� $
��Bold"�$��Center=�.��
СтопA� $��16��$��16��
���  ��
���   �$��Center=�$��5,0,5,0q��� -   $ ManualManagePanel��
���   �$��1q�$��	10,5,10,5q�._��� $ Vertical=�$	��300��$	��Top=�.+��� $��Medium"�.��Вручную�� -   $ IsLoadProperties��$��	10,5,10,5q�$5��/Загружать характеристики�� .  $
 TrueY��� -   $) #btnGetAllProviderProductsAndPricies��$��	10,5,10,5q�$C��=Получить данные всех поставщиков���� -   $ btnGetAllProviderCatalogs��$��	10,5,10,5q�$Q��KПерезагрузить каталоги всех поставщиков���� 
���   �$��1q�$��	10,5,10,5q�._��� $ Vertical=�$	��300��$	��Top=�$��5q�.+��� $��Medium"�.��42Последняя загрузка товаров�� -   $ txtMerlionProductsCount��$��
Merlion: 0���� -   $ txtTreolanProductsCount��$��
Treolan: 0���� -   $ txtOcsProductsCount��$��OCS: 0���� -   $ txtOldiProductsCount��$��OLDI: 0���� -   $ txtTotalProductsCount��$��Всего: 0���� -   $ PropertyPanel��
���   �$��1q�$��	10,5,10,5q�$y�	Collapsed=�._��� $ Vertical=�$	��300��$	��Top=�.+��� $��Medium"�.��-+Загрузка характеристик�� -    ��Name$ PropertiesLoadCount��.��(&Осталось обработать:�� -   $ PropertiesLoadedCount��.��.,Получено характеристик:� -     �Name$ PropertyLoadProgress�� � Minimum$ 0Q�$��	10,5,10,5q�$��20���� -!   $ StoragePanel��
���   �$��1q�$��	10,5,10,5q�$y�	Collapsed=�._��� $ Vertical=�$	��300��$	��Top=�.+��� $��Medium"�.��#!Сохранение данных�� -"   $ DataSaveCount��.��Осталось:� -#   $ DataSaveProgress��$ 0Q�$��	10,5,10,5q�$��20���� -$    ��Name$ lstProductsLog��$��
10,10,10,5q�$	��600��$
��Auto��$��Stretch=�$��Stretch=�