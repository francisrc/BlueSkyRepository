   M S B A M L   `   `   `  ����C  ?BlueSky, Version=4.1.5.81, Culture=neutral, PublicKeyToken=null;"clr-namespace:BlueSky.UserControlsBlueSky.UserControls  5   
   *    $BlueSky.UserControls.OptionSelection   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   4local"clr-namespace:BlueSky.UserControls     5   
     � Title$  OptionSelection��5   	   $	��419��6!   $	��535��6.   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   6,   �� 5	      $
��Auto �6   6,   �� 5
      $	��75* �6   .+�5   
   �� -   5        $��dXTabControl1��5   =   $	��305��6   $
��Left=�6"   $	��Top=�6R   $	��513��6k   .@�5      �� $��	Variables��6   ��.  .��5      �  � ColumnDefinitions 5      �� 5      $
��Auto �6+   6:   �� 5      $��* �6+   67   .+�5      �� .+�5      �� -   5        $��
textBlock1��5   ]   $��20��6$   $
��Left=�60   $��12,7,0,0q�6K   $��Variable List��6o   $	��Top=�6�   '   !BlueSky.UserControls.VariableList  5      -   5          Name$ lbOne��5   -   $
��Left=�6U   $��12,26,0,112q�6p   $	��215��6�    �� DisplayMemberPath$
 Name��6�   $	��242��6�   ��6=   �� 6�   5      �� 5      -   5        $��OptionPanel��5   m   $	��242��6!   $��1��6.   $
��Left=�6>   $��
241,26,0,0q�6Y   $	��Top=�6�   $	��220��6�   5      5      5      5      �� 5   
   $��1��6   $��5q�6   .+�5       �� -   5        $
��Help��5       $��70��6!   $��11,6,452,47q�6,   $��2��6A   .��6N   Help6T   �� 5!      -   5        $��OK��5!      $��70��6   $��342,6,101,47q�6*   $��2��6@   .��6^   OK6b   �� 5"      -   5        $��Cancel��5"      $��70��6#   $��429,6,34,47q�6.   $��2��6C   .��6f   Cancel6n   �� 5#      -   5        $��Reset��5#      $��70��6"   $��84,6,379,47q�6-   $��2��6B   .��6O   Reset6V   5$      5%      5&      