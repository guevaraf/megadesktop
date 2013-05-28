﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MegaDesktop.ViewModel;

namespace MegaDesktop
{
    /// <summary>
    /// Interaction logic for ConfigurationModalDialog.xaml
    /// </summary>
    public partial class ConfigurationModalDialog : UserControl
    {
        public ConfigurationModalDialog()
        {
            InitializeComponent();
            Visibility = Visibility.Hidden;
        }

        private bool _parentWasEnabled = true;

        public bool IsShown
        {
            get { return (bool)GetValue(IsShownProperty); }
            set { SetValue(IsShownProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsShown.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsShownProperty =
            DependencyProperty.Register("IsShown", typeof(bool), typeof(ConfigurationModalDialog), new UIPropertyMetadata(false, IsShownChangedCallback));

        public static void IsShownChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                ConfigurationModalDialog dlg = (ConfigurationModalDialog)d;
                dlg.Show();
            }
            else
            {
                ConfigurationModalDialog dlg = (ConfigurationModalDialog)d;
                dlg.Hide();
            }
        }

        #region OverlayOn

        public UIElement OverlayOn
        {
            get { return (UIElement)GetValue(OverlayOnProperty); }
            set { SetValue(OverlayOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Parent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverlayOnProperty =
            DependencyProperty.Register("OverlayOn", typeof(UIElement), typeof(ConfigurationModalDialog), new UIPropertyMetadata(null));

        #endregion

        public void Show()
        {

            // Force recalculate binding since Show can be called before binding are calculated            
            BindingExpression expressionOverlayParent = this.GetBindingExpression(OverlayOnProperty);
            if (expressionOverlayParent != null)
            {
                expressionOverlayParent.UpdateTarget();
            }

            if (OverlayOn == null)
            {
                throw new InvalidOperationException("Required properties are not bound to the model.");
            }

            Visibility = System.Windows.Visibility.Visible;

            _parentWasEnabled = OverlayOn.IsEnabled;
            OverlayOn.IsEnabled = false;

        }

        private void Hide()
        {
            Visibility = Visibility.Hidden;
            OverlayOn.IsEnabled = _parentWasEnabled;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IsShown = false;
        }
    }
}
