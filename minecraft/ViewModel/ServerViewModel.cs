using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using nihilus.Annotations;
using nihilus.Logic.BackgroundWorker.Performance;
using nihilus.Logic.CustomConsole;
using nihilus.Logic.Manager;
using nihilus.Logic.Model;
using nihilus.Logic.Persistence;
using nihilus.View.Xaml.MainWindowFrames;
using Timer = System.Timers.Timer;

namespace nihilus.ViewModel
{
    public class ServerViewModel : INotifyPropertyChanged
    {
        public static ServerViewModel HomeViewModel()
        {
            ServerViewModel homeViewModel = new ServerViewModel();

            return homeViewModel;
        }

        private string externalIP =  new WebClient().DownloadString("http://icanhazip.com").Trim();
        private bool isHome;

        private ChartValues<double> cpuTotal;
        private ChartValues<double> cpuServer;
        private CPUTracker cpuTracker;

        private ChartValues<double> memServer;
        private MEMTracker memTracker;

        private Timer restartTimer = null;

        public ConsoleReader ConsoleReader;
        public ObservableCollection<string> ConsoleOutList { get; }
        public ObservableCollection<Player> PlayerList { get; set; } = new ObservableCollection<Player>();
        public ObservableCollection<Player> BanList { get; set; } = new ObservableCollection<Player>();
        public ObservableCollection<Player> OPList { get; set; } = new ObservableCollection<Player>();
        public ObservableCollection<Player> WhiteList { get; set; } = new ObservableCollection<Player>();
        public ObservableCollection<ServerVersion> Versions { get; set; } = new ObservableCollection<ServerVersion>();
        public SeriesCollection CPUList { get; set; } = new SeriesCollection();
        public SeriesCollection MEMList { get; set; } = new SeriesCollection();
        public string ConsoleIn { get; set; } = "";
        public ServerStatus CurrentStatus { get; set; }
        public Server Server { get; set; }

        public bool RestartEnabled { get; set; }
        public string NextRestartHours { get; set; }
        public string NextRestartMinutes { get; set; }
        public string NextRestartSeconds { get; set; }
        public string ServerTitle
        {
            get
            {
                if (isHome)
                {
                    return "Home";
                }
                return Server + Environment.NewLine + CurrentStatus;
            }
        }

        public string AddressInfo { get; set; }
        public Page ServerPage { get; set; }

        private ICommand readConsoleIn;
        public ICommand ReadConsoleIn
        {
            get
            {
                return readConsoleIn
                       ?? (readConsoleIn = new ActionCommand(() =>
                       {
                           ConsoleReader?.Read(ConsoleIn);
                           ConsoleIn = "";
                       }));
            }
        }
        
        public double DownloadProgress { get; set; }
        public string DownloadProgressReadable { get; set; }
        public bool DownloadCompleted { get; set; }

        public ServerViewModel(Server server)
        {
            Server = server;
            CurrentStatus = ServerStatus.STOPPED;
            ConsoleOutList = new ObservableCollection<string>();
            if (Server.Version.Type == ServerVersion.VersionType.Vanilla)
            {
                Versions = new ObservableCollection<ServerVersion>(VersionManager.Instance.VanillaVersions);
            } else if (Server.Version.Type == ServerVersion.VersionType.Spigot)
            {
                Versions = new ObservableCollection<ServerVersion>(VersionManager.Instance.SpigotVersions);
            }
            
            Versions =  new ObservableCollection<ServerVersion>(VersionManager.Instance.VanillaVersions);
            ConsoleOutList.CollectionChanged += ConsoleOutChanged;
            UpdateAddressInfo();
            Application.Current.Dispatcher.Invoke(new Action(()=>ServerPage = new ServerPage(this)));
            UpdateWhiteListRead();
            WhiteList.CollectionChanged += WhiteListChanged;
            WhiteList.CollectionChanged += UpdateWhiteListWrite;
            BanList.CollectionChanged += BanListChanged;
            //TODO write
            OPList.CollectionChanged += OPListChanged;
            //TODO write
        }

        private ServerViewModel()
        {
            isHome = true;
            ServerPage = new StartPage();
        }

        private void UpdateAddressInfo()
        {
            AddressInfo = externalIP + ":" + Server.ServerSettings.ServerPort;
        }

        private void UpdateWhiteListWrite(object sender, NotifyCollectionChangedEventArgs e)
        {
            new Thread(() =>
            {
                new FileWriter().WriteWhitelist(Server.Name, new List<Player>(WhiteList));
            }).Start();
        }

        public void SetRestartTime(double time)
        {
            if (time < 0)
            {
                restartTimer?.Dispose();
                RestartEnabled = false;
                return;
            }
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(time);
            new Thread(() =>
            {
                restartTimer = new System.Timers.Timer();
                restartTimer.Interval = 1000;
                restartTimer.Elapsed += (sender, args) =>
                {
                    timeSpan = timeSpan.Subtract(TimeSpan.FromMilliseconds(1000));
                    RestartEnabled = true;
                    NextRestartHours = timeSpan.Hours.ToString();
                    NextRestartMinutes = timeSpan.Minutes.ToString();
                    NextRestartSeconds = timeSpan.Seconds.ToString();
                    if (timeSpan.Hours == 0 && timeSpan.Minutes == 30 && timeSpan.Seconds == 0)
                    {
                        ApplicationManager.Instance.ActiveServers[Server].StandardInput.WriteLineAsync("/say Next server restart in 30 minutes!");
                    } else if (timeSpan.Hours == 0 && timeSpan.Minutes ==5 && timeSpan.Seconds==0)
                    {
                        ApplicationManager.Instance.ActiveServers[Server].StandardInput.WriteLineAsync("/say Next server restart in 5 minutes!");
                    } else if (timeSpan.Hours == 0 && timeSpan.Minutes == 1 && timeSpan.Seconds==0)
                    {
                        ApplicationManager.Instance.ActiveServers[Server].StandardInput.WriteLineAsync("/say Next server restart in 1 minute!");
                    }
                };
                restartTimer.AutoReset = true;
                restartTimer.Enabled = true;
            }).Start();
        }

        public void UpdateSettings()
        {
            UpdateAddressInfo();
            new Thread(() =>
            {
                new FileWriter().WriteServerSettings(Server.Name,Server.ServerSettings.SettingsDictionary);
                Serializer.Instance.StoreServers(ServerManager.Instance.Servers);
            }).Start();
        }

        public bool PlayerExists(string name)
        {
            //TODO
            return true;
        }

        public void AddPlayerToWhiteList(string name)
        {
            new Thread(() =>
            {
                Player p = new Player(name);
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, 
                    new Action(()=>WhiteList.Add(p)));
            }).Start();
        }

        public void UpdateWhiteListRead()
        {
            new Thread(() =>
            {
                List<Player> whitelistedPlayers = new FileReader().ReadWhiteList(Server.Name);
                WhiteList = new ObservableCollection<Player>(whitelistedPlayers);
                WhiteList.CollectionChanged += WhiteListChanged;
                WhiteList.CollectionChanged += UpdateWhiteListWrite;
            }).Start();
        } 

        public void TrackPerformance(Process p)
        {
            // Track CPU usage
            cpuTracker?.StopThreads();
            
            cpuTracker = new CPUTracker();
            cpuTotal = new ChartValues<double>();
            cpuTracker.TrackTotal(p, cpuTotal,102);
            cpuServer = new ChartValues<double>();
            cpuTracker.TrackP(p,cpuServer, 102);

            CPUList = new SeriesCollection
            {
                new LineSeries
                {
                    Values = cpuTotal
                },
                new LineSeries
                {
                    Values = cpuServer
                }
            };
            cpuTotal.CollectionChanged += CPUListChanged;
            cpuServer.CollectionChanged += CPUListChanged;
            
            
            // Track memory usage
            memTracker?.StopThreads();
            
            memTracker = new MEMTracker();
            memServer = new ChartValues<double>();
            memTracker.TrackP(p,memServer,102);
            
            MEMList = new SeriesCollection
            {
                new LineSeries
                {
                    Values = memServer
                }
            };
            memServer.CollectionChanged += MEMListChanged;
        }
        
        public void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            DownloadProgress = bytesIn / totalBytes * 100;
            DownloadProgressReadable = Math.Round(DownloadProgress, 0)+"%";
        }

        public void DownloadCompletedHandler(object sender, AsyncCompletedEventArgs e)
        {
            DownloadCompleted = true;
        }

        private void ConsoleOutChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            raisePropertyChanged(nameof(ConsoleOutList));
        }

        private void CPUListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            raisePropertyChanged(nameof(CPUList));
        }

        private void MEMListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            raisePropertyChanged(nameof(MEMList));
        }
        private void WhiteListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            raisePropertyChanged(nameof(WhiteList));
        }
        private void BanListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            raisePropertyChanged(nameof(BanList));
        }
        private void OPListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            raisePropertyChanged(nameof(OPList));
        }

        
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void raisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        
        private class ActionCommand : ICommand
        {
            private readonly Action _action;

            public ActionCommand(Action action)
            {
                _action = action;
            }

            public void Execute(object parameter)
            {
                _action();
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }
    }
}