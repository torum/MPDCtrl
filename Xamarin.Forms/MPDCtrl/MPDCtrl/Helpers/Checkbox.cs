using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MPDCtrl.Helpers
{
    public class Checkbox : Button
    {
        public Checkbox()
        {
            //base.Image = ";
            base.Clicked += new EventHandler(OnClicked);
            base.BackgroundColor = Color.Transparent;
            base.BorderWidth = 2;
            base.BorderColor = Color.Transparent;
            base.TextColor = Color.Default;
        }

        public static BindableProperty IsCheckedProperty = BindableProperty.Create(
            propertyName: "IsChecked",
            returnType: typeof(Boolean?),
            declaringType: typeof(Checkbox),
            defaultValue: null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: CheckedValueChanged);

        public Boolean? IsChecked
        {
            get
            {
                if (GetValue(IsCheckedProperty) == null)
                {
                    return null;
                }
                return (Boolean)GetValue(IsCheckedProperty);
            }
            set
            {
                SetValue(IsCheckedProperty, value);
                OnPropertyChanged();
                RaiseCheckedChanged();
            }
        }

        private static void CheckedValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && (Boolean)newValue == true)
            {
                //((Checkbox)bindable).Image = "";
                //((Checkbox)bindable).BorderWidth = 1;
                ((Checkbox)bindable).BorderColor = ((Checkbox)bindable).TextColor;
                //((Checkbox)bindable).BackgroundColor = ((Checkbox)bindable).TextColor;
            }
            else
            {
                //((Checkbox)bindable).Image = "";
                //((Checkbox)bindable).BorderWidth = 0;
                ((Checkbox)bindable).BorderColor = Color.Transparent;
                //((Checkbox)bindable).BackgroundColor = Color.Transparent;
            }
        }

        public event EventHandler CheckedChanged;
        private void RaiseCheckedChanged()
        {
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }


        public void OnClicked(object sender, EventArgs e)
        {
            IsChecked = !IsChecked;
        }

    }
}
