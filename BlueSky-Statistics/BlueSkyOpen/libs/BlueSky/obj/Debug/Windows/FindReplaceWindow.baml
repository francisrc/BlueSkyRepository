   M S B A M L   `   `   `  ����  BlueSky'    !BlueSky.Windows.FindReplaceWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  Find Replace Window��5   	   $	��150��6%   $	��400��62    � 
ResizeMode$ NoResize=�6>   
�������.��5      �  � RowDefinitions 5   
   �� 5      6   �� 5      6   �� 5	      6    � ColumnDefinitions 5   
   �� 5      $
��Auto �6   6.   �� 5      6!   �� 5      $
��Auto �6   6.   .+�5   
   �� $��0��6   $��0��6   $��1q�6-   .��69   Find:6@   �� 5   
   -   5        $��findtxt��5   0   $��0��6   $��1��6    $��24��6?   $��1q�6K   $��Center=�6V   6s   �� 5   
   $��1��6   $��0��6   $��1q�6-   .��69   Replace With:6H   �� 5   
   -   5        $��
replacetxt��5   /   $��1��6   $��1��6   $��24��6A   $��1q�6M   $��Center=�6Y   6v   �� 5   
   $��2��6   $��1��6"    �� Orientation$ 
Horizontal=�62   .+�5      �� -   5        $��findnextbutton��5      $��70��6+   $��20��66   $
��10,1q�6B   $��Center=�6n   .��6�   	Find Next6�   �� 5      -   5        $��replacebutton��5      $��70��6*   $��20��65   $
��10,1q�6A   $��Center=�6l   .��6�   	Replace6�   �� 5      -   5        $��replaceallbutton��5      $��70��6-   $��20��68   $
��10,1q�6D   $��Center=�6r   .��6�   Replace All6�   5      5      5      