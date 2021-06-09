using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MPDCtrl.Helpers
{
    /// <summary>
    /// Enable maximizing the window on double click for a control
    /// </summary>
    public class MaximizeDoubleClickHelper
    {
        public static readonly DependencyProperty MaximizeDoubleClickProperty = DependencyProperty.RegisterAttached(
            "MaximizeDoubleClick",
            typeof(bool),
            typeof(MaximizeDoubleClickHelper),
            new PropertyMetadata(default(bool), OnLoaded));

        private static void OnLoaded(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            UIElement uiElement = dependencyObject as UIElement;
            if (uiElement == null || (dependencyPropertyChangedEventArgs.NewValue is bool) == false)
            {
                return;
            }
            if ((bool)dependencyPropertyChangedEventArgs.NewValue == true)
            {
                uiElement.MouseLeftButtonDown += UIElementMouseLeftButtonDown;
            }
            else
            {
                uiElement.MouseLeftButtonDown -= UIElementMouseLeftButtonDown;
            }

        }

        private static void UIElementMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseEventArgs)
        {
            UIElement uiElement = sender as UIElement;
            if (uiElement != null)
            {
                if (mouseEventArgs.ClickCount == 2)
                {
                    var parentWindow = Window.GetWindow(uiElement);
                    if (parentWindow != null && parentWindow.WindowState == WindowState.Normal)
                    {
                        if (parentWindow.ResizeMode != ResizeMode.NoResize)
                        {
                            parentWindow.WindowState = WindowState.Maximized;
                        }

                        
                    }
                    else if (parentWindow != null && parentWindow.WindowState == WindowState.Maximized)
                    {
                        parentWindow.WindowState = WindowState.Normal;
                    }
                }
            }
        }

        public static void SetMaximizeDoubleClick(DependencyObject element, bool value)
        {
            element.SetValue(MaximizeDoubleClickProperty, value);
        }

        public static bool GetMaximizeDoubleClick(DependencyObject element)
        {
            return (bool)element.GetValue(MaximizeDoubleClickProperty);
        }
    }
}
