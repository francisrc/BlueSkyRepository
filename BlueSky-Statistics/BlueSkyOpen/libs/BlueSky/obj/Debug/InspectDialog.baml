   M S B A M L   `   `   `  ����^  ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35tOclr-namespace:System.Windows.Forms.Integration;assembly=WindowsFormsIntegration System.Windows.Forms.Integration  5   	    BlueSky   BlueSky.InspectDialog   5   	   VmyOclr-namespace:System.Windows.Forms.Integration;assembly=WindowsFormsIntegration  5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � SizeToContent$  Height=�5       � Title$ InspectWindow��5   	    � 	Resources 5       
  purple �� EndPoint �� 
StartPoint � Color � Offset��%  &	        �� 5   
   $	 0,1&�6-   $	 0,0&�6<   .��5	      � $ 	#FF6CaaD6��6   $ 0Q�6-   � 5
      $ 	#FFEDEFF3��6   $ 1Q�6-   � 5      $ 	#FFDDDDDD��6   $ 1Q�6-   � 5      $ 	#FFEDEFF3��6   $ 1Q�6-   5      5      .��5      � ���	#FFEDefFf � RowDefinitions 5   
   �� 5      $��78 �6   6+   �� 5      6   �� 5      $��50 �6   6*   �� 5      $	��100 �6   6+   �� 5      $��50 �6   6+   .+�5    
   �� 
�������$��3,3,3,3q�6*   $��1q�6;   $��0��6O   ._�5!      �� -   5        $��helpMessage��5!   M    �� TextWrapping$
 Wrap=�6   "	 �� VerticalScrollBarVisibility$
	 Auto=�6*   .��6b   5"      5#      �� 5%   
   -   5        $��_CanvasHost��5%      $��1��6$   5'      �� 5(   
   -   5        $��Grouping��5(      $��1��6!   $��2��6/   $��1q�6A   
�������$��3,3,3,3q�6l   5*      �� 5:   
   
 �� Orientation$
 
Horizontal=�6   $��2��6.   .+�5;      �� -   5        $��Ok��5;   !   $��0q�6   $��5q�6+   $��50��68   $��24��6C   #��[  6`    J� Effect 5<      3  -System.Windows.Media.Effects.DropShadowEffect  5=      .��5>   !   OK5?      �� 5@      -   5        $��Syntax��5@   !   $��0q�6   $��5q�6/   $��50��6:   $��24��6E   #��[  6f    5A        5B      .��5C   !   Syntax5D      �� 5E      -   5        $
��Help��5E   !   $��0q�6   $��5q�6-   $��50��68   $��24��6C   #��[  6b    5F        5G      .��5H   !   Help5I      5K      �� 5M      $��3��6   $��1q�6$   
�������$��3,3,3,3q�6O   ._�5N   
   7   1System.Windows.Forms.Integration.WindowsFormsHost  -   5        $��_CanvasPropHost��5N      5O      �� 5Q   
   $
 
Horizontal=�6   $��4��6.   .+�5R      �� -   5        $	��Ok1��5R   !   $��0q�6   $��5q�6,   $��50��67   $��24��6B   #��[  6`    5S        5T      .��5U   !   OK5V      5Y      5]      5^      