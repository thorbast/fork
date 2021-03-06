using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Threading;
using Fork.Annotations;
using Fork.Logic.Manager;
using Fork.Logic.Model;
using Fork.Logic.Model.Settings;
using Fork.View.Xaml2.Pages.Settings;
using Fork.View.Xaml2.Pages.Settings;
using ICSharpCode.AvalonEdit.Rendering;

namespace Fork.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        #region private Varibles
        
        

        #endregion

        #region Properties
        
        //Others
        public Entity Entity { get; set; }
        //public ObservableCollection<SettingsFile> Settings { get; set; }
        public EntityViewModel EntityViewModel { get; set; }

        //Pages
        public SettingsPage SettingsPage { get; }
        public ISettingsPage ForkEntitySettingsPage { get; }
        
        public ObservableCollection<ISettingsPage> SettingsPages { get; set; } = new ObservableCollection<ISettingsPage>();

        #endregion

        public SettingsViewModel(ServerViewModel serverViewModel)
        {
            EntityViewModel = serverViewModel;
            Entity = serverViewModel.Entity;
            SettingsPage = new SettingsPage(this);
            ForkEntitySettingsPage = new ForkServerSettingsPage(this);
        }

        public SettingsViewModel(NetworkViewModel networkViewModel)
        {
            EntityViewModel = networkViewModel;
            Entity = networkViewModel.Network;
            SettingsPage = new SettingsPage(this);
            ForkEntitySettingsPage = new ForkNetworkSettingsPage(this);
        }

        public void InitializeSettings(List<SettingsFile> settingsFiles)
        {
            SettingsPages = new ObservableCollection<ISettingsPage>();
            SettingsPages.Add(ForkEntitySettingsPage);
            List<SettingsFile> orderedList = settingsFiles.OrderBy(x => x.NameID).ThenByDescending(x => x.FileInfo.Name).ToList();
            foreach (SettingsFile settingsFile in orderedList)
            {
                switch (settingsFile.Type)
                {
                    case SettingsFile.SettingsType.Vanilla:
                        Application.Current.Dispatcher?.Invoke(() =>
                            SettingsPages.Add(new VanillaSettingsPage(this, settingsFile)));
                        break;
                    case SettingsFile.SettingsType.Bungee:
                        if (EntityViewModel is NetworkViewModel)
                        {
                            Application.Current.Dispatcher?.Invoke(() =>
                                SettingsPages.Add(new ProxySettingsPage(this, settingsFile)));
                            break;
                        }
                        goto default;
                    default:
                        Application.Current.Dispatcher?.Invoke(() =>
                            SettingsPages.Add(new DefaultSettingsPage(settingsFile)));
                        break;
                }
            }
        }

        public void UpdateSettings(List<SettingsFile> settingsFiles)
        {
            foreach (SettingsFile settingsFile in settingsFiles)
            {
                bool add = true;
                ISettingsPage existingPage = null;
                foreach (ISettingsPage setting in SettingsPages)
                {
                    //Edge case for Settings without file
                    if (settingsFile == null || setting.SettingsFile == null) continue;
                    if (settingsFile.FileInfo.FullName.Equals(setting.SettingsFile.FileInfo.FullName))
                    {
                        add = false;
                        existingPage = setting;
                        break;
                    }

                }

                if (add)
                {
                    switch (settingsFile.Type)
                    {
                        case SettingsFile.SettingsType.Vanilla:
                            Application.Current.Dispatcher?.Invoke(() =>
                                SettingsPages.Add(new VanillaSettingsPage(this, settingsFile)));
                            break;
                        case SettingsFile.SettingsType.Bungee:
                            if (EntityViewModel is NetworkViewModel)
                            {
                                Application.Current.Dispatcher?.Invoke(() =>
                                    SettingsPages.Add(new ProxySettingsPage(this, settingsFile)));
                                break;
                            }
                            goto default;
                        default:
                            Application.Current.Dispatcher?.Invoke(() =>
                                SettingsPages.Add(new DefaultSettingsPage(settingsFile)));
                            break;
                    }
                }

                bool remove = true;
                int removeIndex = 0;
                foreach (ISettingsPage setting in SettingsPages)
                {
                    //Edge case for Settings without file
                    if (setting.SettingsFile == null) continue;
                    if (settingsFile.FileInfo.FullName.Equals(setting.SettingsFile.FileInfo.FullName))
                    {
                        remove = false;
                        removeIndex = SettingsPages.IndexOf(setting);
                    }
                }

                if (remove)
                {
                    Application.Current.Dispatcher?.Invoke(() => SettingsPages.RemoveAt(removeIndex));
                }

                if (!add && !remove)
                {
                    existingPage.SettingsFile.ReadText();
                }
            }
            SortSettingsPages();
        }

        private void SortSettingsPages()
        {
            List<ISettingsPage> sorted = SettingsPages.OrderBy(x => x.SettingsFile.NameID).ThenByDescending(x => x.SettingsFile.FileInfo.Name).ToList();
            
            int ptr = 0;
            while (ptr < sorted.Count)
            {
                if (!SettingsPages[ptr].Equals(sorted[ptr]))
                {
                    ISettingsPage t = SettingsPages[ptr];
                    int ptr1 = ptr;
                    Application.Current.Dispatcher?.Invoke(()=>SettingsPages.Move(ptr1,sorted.IndexOf(t)));
                }
                else
                {
                    ptr++;
                }
            }
        }

        public void SaveChanges()
        {
            List<ISettingsPage> pages = new List<ISettingsPage>(SettingsPages);
            foreach (ISettingsPage settingsPage in pages)
            {
                settingsPage.SaveSettings();
            }
        }
        
        
        [NotifyPropertyChangedInvocator]
        protected void raisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}