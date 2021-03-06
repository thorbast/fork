﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Fork.Logic.Manager;
using Fork.Logic.Model;
using Fork.Logic.Model.Settings;
using Fork.View.Xaml2.Pages.Server;
using Fork.View.Xaml2.Pages.Settings;
using Fork.ViewModel;
using Path = System.IO.Path;

namespace Fork.View.Xaml2.Pages.Settings
{
    public partial class VanillaSettingsPage : Page, ISettingsPage
    {
        public SettingsFile SettingsFile { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(SettingsFile.FileInfo.FullName);
        public string FileExtension => Path.GetExtension(SettingsFile.FileInfo.FullName);
        
        private SettingsViewModel viewModel;
        private ServerViewModel serverViewModel;
        
        public VanillaSettingsPage(SettingsViewModel viewModel, SettingsFile settingsFile)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.SettingsFile = settingsFile;
            serverViewModel = viewModel.EntityViewModel as ServerViewModel;
            DataContext = serverViewModel;
        }

        public void SaveSettings()
        {
            serverViewModel.SaveProperties();
        }
    }
}
