   M S B A M L   `   `   `  ����  BlueSky+    %BlueSky.Windows.AddFactorLevelsDialog   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Add Factor Levels Dialog��5   	   $	��260��6*   $	��318��67    � 
ResizeMode$ NoResize=�6C   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   6,   �� 5      $
��Auto �6   6,   �� 5	      $
��Auto �6   6,   �� 5
      $��* �6   6)   �� 5      $
��Auto �6   6,    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   6.   �� 5      $��1 �6   6+   �� 5      $
��Auto �6   6.   .+�5   
   �� -   5        $��
textBlock1��5   L   $��18��6   $
��Left=�6    $��5,5,0,0q�6;   $��New factor level:��6^   $	��Top=�6w   $	��190��6�   �� 5   
   -   5        $��	faclvltxt��5   W   $��1��6   $��25��6   $
��Left=�6+   $��5,5,0,0q�6F   $	��Top=�6h   $	��182��6�   �� 5   
   -   5        $��
textBlock2��5   Z   $��2��6   $��18��6"   $
��Left=�6.   $��5,5,0,0q�6I   $��Current Factor Levels:��6l   $	��Top=�6�   $	��149��6�   �� 5   
   -   5        $��levelsListBox��5   Y   $��3��6   $	��100��6    $
��Left=�6-   $��5,5,0,0q�6H   $	��Top=�6n   $	��182��6�   �� 5   
   -   5        $��addlvlButton��5   x   $��1��6   $��2��6   $��Add New Level��6.   $��21��6F   $
��Left=�6R   $��5q�6m   $	��Top=�6�   $��90��6�   6�   �� 5   
   $��4��6   $��0��6"    �� Orientation$ 
Horizontal=�62   .+�5   
   �� -   5        $��okbutton��5   R   $��OK��6   $��23��6    $
��Left=�6,   $��5q�6G   $	��Top=�6b   $��50��6z   6�   �� 5   
   -   5        $��cancelbutton��5   U   $��Cancel��6   $��23��6#   $
��Left=�6/   $��5q�6J   $	��Top=�6i   $��60��6�   6�   5      �� 5   
   -   5        $��removelvlbutton��5   }   $��3��6   $��2��6   $��Remove Level��6.   $��23��6E   $
��Left=�6Q   $��4,5,0,0q�6l   $	��Top=�6�   $��90��6�   6�   5      5      