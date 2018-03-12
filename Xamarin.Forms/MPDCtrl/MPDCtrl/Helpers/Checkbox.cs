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
            //base.Image = "Assets/newcbu.png";
            base.Clicked += new EventHandler(OnClicked);
            base.BackgroundColor = Color.Transparent;
            base.BorderWidth = 0;
            base.BorderColor = Color.Transparent;
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
                //((Checkbox)bindable).Image = "Assets/repeat-36.png";
                ((Checkbox)bindable).BorderWidth = 1;
                ((Checkbox)bindable).BorderColor = ((Checkbox)bindable).TextColor;
            }
            else
            {
                //((Checkbox)bindable).Image = "Assets/shuffle-variant-36.png";
                ((Checkbox)bindable).BorderWidth = 0;
                ((Checkbox)bindable).BorderColor = Color.Transparent;
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
