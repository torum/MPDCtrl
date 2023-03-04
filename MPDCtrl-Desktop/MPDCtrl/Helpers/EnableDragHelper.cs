using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MPDCtrl.Helpers
{
    /// <summary>
    /// Enable drag for a control
    /// </summary>
    public static class EnableDragHelper
    {
        public static readonly DependencyProperty EnableDragProperty = DependencyProperty.RegisterAttached(
            "EnableDrag",
            typeof(bool),
            typeof(EnableDragHelper),
            new PropertyMetadata(default(bool), OnLoaded));

        private static void OnLoaded(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            UIElement uiElement = dependencyObject as UIElement;
            if (uiElement is null || (dependencyPropertyChangedEventArgs.NewValue is bool) == false)
            {
                return;
            }
            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {
                uiElement.MouseMove += UiElementOnMouseMove;
            }
            else
            {
                uiElement.MouseMove -= UiElementOnMouseMove;
            }

        }

        private static void UiElementOnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            UIElement uiElement = sender as UIElement;
            if (uiElement is not null && mouseEventArgs.LeftButton == MouseButtonState.Pressed)
            {
                DependencyObject parent = uiElement;
                int avoidInfiniteLoop = 0;
                // Search up the visual tree to find the first parent window.
                while ((parent is Window) == false)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                    avoidInfiniteLoop++;
                    if (avoidInfiniteLoop == 1000)
                    {
                        System.Diagnostics.Debug.WriteLine(" Something is wrong - we could not find the parent window.");

                        // Something is wrong - we could not find the parent window.
                        return;
                    }
                }
                var window = parent as Window;
                window.DragMove();
            }
        }

        public static void SetEnableDrag(DependencyObject element, bool value)
        {
            element.SetValue(EnableDragProperty, value);
        }

        public static bool GetEnableDrag(DependencyObject element)
        {
            return (bool)element.GetValue(EnableDragProperty);
        }
    }
}
