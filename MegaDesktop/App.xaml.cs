using MegaDesktop;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using MegaDesktop.ViewModel;
using System.Globalization;
using MegaApi.Comms;

namespace MegaWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            // This code is used to test the app when using other cultures.
            //
            //System.Threading.Thread.CurrentThread.CurrentCulture =
            //    System.Threading.Thread.CurrentThread.CurrentUICulture =
            //        new System.Globalization.CultureInfo("it-IT");


            // Ensure the current culture passed into bindings is the OS culture.
            // By default, WPF uses en-US as the culture, regardless of the system settings.
            //
            FrameworkElement.LanguageProperty.OverrideMetadata(
              typeof(FrameworkElement),
              new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            MainWindow window = new MainWindow();

            // Create the ViewModel to which 
            // the main window binds.
            var viewModel = new MainWindowViewModel();

            // Allow all controls in the window to 
            // bind to the ViewModel by setting the 
            // DataContext, which propagates down 
            // the element tree.
            window.DataContext = viewModel;

            window.Show();
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            GoogleAnalytics.SendTrackingRequest("/Desktop_Current_DispatcherUnhandledException_" + e.Exception.GetType().Name);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            GoogleAnalytics.SendTrackingRequest("/Desktop_CurrentDomain_UnhandledException_" + e.ExceptionObject.GetType().Name);
        }
    }
}
