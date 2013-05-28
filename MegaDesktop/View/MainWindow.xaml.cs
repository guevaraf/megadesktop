using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using MegaApi;
using MegaApi.Utility;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using MegaApi.DataTypes;
using MegaDesktop;
using System.Diagnostics;
using System.Reflection;
using MegaApi.Comms;
using MegaDesktop.ViewModel;

namespace MegaWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonLogin_Click(object sender, RoutedEventArgs e)
        {
            //var w = new WindowLogin();
            //w.OnLoggedIn += (s, args) =>
            //{
            //    api = args.Api;
            //    SaveAccount(GetUserKeyFilePath(), "user.anon.dat");
            //    Invoke(() =>
            //        {
            //            CancelTransfers();
            //            w.Close();
            //            buttonLogin.Visibility = System.Windows.Visibility.Collapsed;
            //            buttonLogout.Visibility = System.Windows.Visibility.Visible;
            //            Title = title + " - " + api.User.Email;
            //        });
            //    InitialLoadNodes();
            //};
            //w.ShowDialog();
        }

        private void buttonLogout_Click(object sender, RoutedEventArgs e)
        {
            //CancelTransfers();
            //Invoke(() =>
            //    {
            //        transfers.Clear();
            //        listBoxNodes.ItemsSource = null;
            //    });
            //var userAccount = GetUserKeyFilePath();
            //// to restore previous anon account
            ////File.Move(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(userAccount), "user.anon.dat"), userAccount);
            //// or simply drop logged in account
            //File.Delete(userAccount);
            //Login(false, userAccount);
        }

        private void buttonFeedback_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
        }

        private void Window_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if ((e.Effects & DragDropEffects.Copy) == DragDropEffects.Copy)
                {
                    String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);

                    if (files.Length > 0)
                    {
                        ((MainWindowViewModel)this.DataContext).UploadBatchCommand.Execute(files);
                    }
                }
            }
        }
    }
}
