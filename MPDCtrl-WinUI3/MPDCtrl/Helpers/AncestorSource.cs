using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Helpers;

public class AncestorSource
{
    public static readonly DependencyProperty AncestorTypeProperty =
        DependencyProperty.RegisterAttached(
            "AncestorType",
            typeof(Type),
            typeof(AncestorSource),
            new PropertyMetadata(null, OnAncestorTypeChanged));

    public static Type GetAncestorType(FrameworkElement element) =>
        (Type)element.GetValue(AncestorTypeProperty);

    public static void SetAncestorType(FrameworkElement element, Type value) =>
        element.SetValue(AncestorTypeProperty, value);

    private static void OnAncestorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement targetElement && e.NewValue is Type ancestorType)
        {
            targetElement.Loaded += (s, args) =>
            {
                var parent = targetElement.FindParent(ancestorType);
                if (parent is FrameworkElement ancestor)
                {
                    targetElement.DataContext = ancestor.DataContext;
                }
            };
        }
    }
}

// A helper extension method
public static class VisualTreeHelperExtensions
{
    public static DependencyObject? FindParent(this DependencyObject child, Type ancestorType)
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null && parent.GetType() != ancestorType)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent;
    }
}
