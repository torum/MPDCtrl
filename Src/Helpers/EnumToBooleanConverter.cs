using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using MPDCtrl.Models;
using System;

namespace MPDCtrl.Helpers;

public sealed partial class ElementThemeEnumToBooleanConverter : IValueConverter
{
    public ElementThemeEnumToBooleanConverter()
    {
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            if (!Enum.IsDefined(typeof(ElementTheme), value))
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse<ElementTheme>(enumString);

            return enumValue.Equals(value);
        }

        throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            return Enum.Parse<ElementTheme>(enumString);
        }

        throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
    }
}

public partial class SystemBackdropEnumToBooleanConverter : IValueConverter
{
    public SystemBackdropEnumToBooleanConverter()
    {
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            if (!Enum.IsDefined(typeof(SystemBackdropOptions), value))
            {
                throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
            }

            var enumValue = Enum.Parse<SystemBackdropOptions>(enumString);

            return enumValue.Equals(value);
        }

        throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            return Enum.Parse<SystemBackdropOptions>(enumString);
        }

        throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
    }
}
