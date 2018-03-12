using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MPDCtrl.Helpers
{
    public class Toggle : Button
    {
        public Toggle()
        {
            //base.Image = "";
            base.Clicked += new EventHandler(OnClicked);
            base.BackgroundColor = Color.Transparent;
            base.BorderWidth = 0;
            base.BorderColor = Color.Transparent;
        }

        public static BindableProperty IsOnProperty = BindableProperty.Create(
            propertyName: "IsOn",
            returnType: typeof(Boolean?),
            declaringType: typeof(Toggle),
            defaultValue: null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: CheckedValueChanged);

        public Boolean? IsOn
        {
            get
            {
                if (GetValue(IsOnProperty) == null)
                {
                    return null;
                }
                return (Boolean)GetValue(IsOnProperty);
            }
            set
            {
                SetValue(IsOnProperty, value);
                OnPropertyChanged();
                RaiseCheckedChanged();
            }
        }

        private static void CheckedValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && (Boolean)newValue == true)
            {
                ((Toggle)bindable).Image = ((Toggle)bindable).OnImageSource;
            }
            else
            {
                ((Toggle)bindable).Image = ((Toggle)bindable).OffImageSource;
            }
        }

        public event EventHandler CheckedChanged;
        private void RaiseCheckedChanged()
        {
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }


        public void OnClicked(object sender, EventArgs e)
        {
            IsOn = !IsOn;
        }


        public static readonly BindableProperty OnImageSourceProperty = BindableProperty.Create(
            propertyName: "OnImageSource",
            returnType: typeof(FileImageSource),
            declaringType: typeof(Toggle));

        public FileImageSource OnImageSource
        {
            get { return (FileImageSource)GetValue(OnImageSourceProperty); }
            set { SetValue(OnImageSourceProperty, value); }
        }

        public static readonly BindableProperty OffImageSourceProperty = BindableProperty.Create(
            propertyName: "OffImageSource",
            returnType: typeof(FileImageSource),
            declaringType: typeof(Toggle));

        public FileImageSource OffImageSource
        {
            get { return (FileImageSource)GetValue(OffImageSourceProperty); }
            set { SetValue(OffImageSourceProperty, value); }
        }
    }
}