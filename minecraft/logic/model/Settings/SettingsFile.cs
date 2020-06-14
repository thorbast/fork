using System;
using System.IO;
using System.Text;
using System.Threading;
using fork.Logic.Manager;
using fork.Logic.Persistence;
using fork.ViewModel;
using ICSharpCode.AvalonEdit.Utils;
using FileReader = fork.Logic.Persistence.FileReader;

namespace fork.Logic.Model.Settings
{
    public class SettingsFile
    {
        public enum SettingsType
        {
            Vanilla, Bungee, Undefined
        }
        
        private static object fileLock = new object();
        
        public FileInfo FileInfo { get; set; }
        public int NameID { get; }
        public string Text { get; set; }
        public SettingsType Type { get; }

        //Constructor for Settings without file
        public SettingsFile(string name)
        {
            Type = SettingsType.Undefined;
            NameID = GetNameID(name);
            FileInfo = new FileInfo(Path.Combine(App.ApplicationPath,"persistence","entities.json"));
        }
        public SettingsFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            ReadText();
            switch (fileInfo.Name)
            {
                case "server.properties":
                    Type = SettingsType.Vanilla;
                    break;
                case "config.yml":
                    Type = SettingsType.Bungee;
                    break;
                default:
                    Type = SettingsType.Undefined;
                    break;
            }

            NameID = GetNameID(FileInfo.Name);
        }


        public void ReadText()
        {
            new Thread(() =>
            {
                while (!FileReader.IsFileReadable(FileInfo))
                {
                    Thread.Sleep(500);
                    if (ApplicationManager.Instance.HasExited)
                    {
                        return;
                    }
                }
                lock (fileLock)
                {
                    Text = File.ReadAllText(FileInfo.FullName);
                }
            }).Start();
        }

        public void SaveText()
        {
            new Thread(() =>
            {
                while (!FileWriter.IsFileWritable(FileInfo))
                {
                    Thread.Sleep(500);
                    if (ApplicationManager.Instance.HasExited)
                    {
                        return;
                    }
                }

                lock (fileLock)
                {
                    File.WriteAllText(FileInfo.FullName,Text);
                }
            }).Start();
        }

        private int GetNameID(string filename)
        {
            switch (filename)
            {
                case "Settings":
                    return -1;
                case "server.properties":
                    return 0;
                case "config.yml":
                    return 0;
                case "paper.yml":
                    return 1;
                case "waterfall.yml":
                    return 1;
                case "spigot.yml":
                    return 2;
                case "bukkit.yml":
                    return 3;
                case "permissions.yml":
                    return 11;
                case "help.yml":
                    return 30;
                default:
                    return 10;
            }
        }
        
        
        public override string ToString()
        {
            return FileInfo.Name;
        }

        protected bool Equals(SettingsFile other)
        {
            return Equals(FileInfo, other.FileInfo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SettingsFile) obj);
        }

        public override int GetHashCode()
        {
            return (FileInfo != null ? FileInfo.GetHashCode() : 0);
        }
    }
}