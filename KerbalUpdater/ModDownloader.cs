using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
namespace KerbalUpdater
{
    class ModDownloader
    {
        public enum State
        {
            READY, DOWNLOADING, STAGING, COMPLETE, IGNORED, ERROR
        }
        private static Dictionary<string, ModDownloader> Downloaders = new Dictionary<string, ModDownloader>();
        public static ModDownloader GetDownloader(KerbalMod mod)
        {
            if (!Downloaders.ContainsKey(mod.PluginName))
            {
                Downloaders[mod.PluginName] = new ModDownloader(mod);
            }
            return Downloaders[mod.PluginName];
        }
        private KerbalMod Mod;
        private WebClient Client;
        private string ZipFileName;
        public string ErrorMessage { get; private set; }
        public int Progress { get; private set; }
        public State CurrentState;
        public ModDownloader(KerbalMod mod)
        {
            this.Mod = mod;
            CurrentState = (mod.UpToDate ? State.IGNORED : State.READY);
        }
        public void BeginDownload()
        {
            CurrentState = State.DOWNLOADING;
            using (Client = new WebClient())
            {
                ZipFileName = String.Format(KerbalUpdater.Constants.DOWNLOAD_TARGET, Mod.SpacePortID);
                Client.DownloadProgressChanged += (sender, e) =>
                {
                    Progress = e.ProgressPercentage;
                };
                Client.DownloadFileCompleted += (sender, e) =>
                {
                    Exception ex = e.Error;
                    if (ex != null)
                    {
                        this.CurrentState = State.ERROR;
                        this.ErrorMessage = ex.Message; 
                    }
                };
                Client.DownloadFileAsync(Mod.DownloadURL, ZipFileName);
                // for some reason, the DownloadFileComplete handler doesn't work
                // staging invoked via KerbalUpdater
            }
        }
        public void BeginStaging()
        {
            CurrentState = State.STAGING;
            string tempDirectory = String.Format("{0}/{1}/", KerbalUpdater.Constants.STAGING_TARGET, Mod.SpacePortID);
            try
            {
                new System.Threading.Thread(() =>
                {
                    // patch for bug, see: http://stackoverflow.com/questions/4600923/monotouch-icsharpcode-sharpziplib-giving-an-error
                    ICSharpCode.SharpZipLib.Zip.ZipConstants.DefaultCodePage = System.Text.Encoding.UTF8.CodePage;
                    // extract it first, then deal with individual files
                    (new ICSharpCode.SharpZipLib.Zip.FastZip()).ExtractZip(ZipFileName, tempDirectory, null);
                    DirectoryInfo directory = new DirectoryInfo(tempDirectory);
                    foreach (DirectoryInfo childDirectory in directory.GetDirectories())
                    {
                        if (childDirectory.Name == KerbalUpdater.Constants.GAME_DATA)
                        {   // This plugin is expecting us to extract to the KSP root folder
                            directory = childDirectory;
                            break;
                        }
                    }
                    if (Mod.PluginName == "KerbalUpdater")
                    {   // handle a cute edge case
                        Debug.Log("Updating KerbalUpdater");
                        FileInfo migration = new FileInfo(directory.FullName + "/KerbalUpdater/Plugins/KerbalUpdaterMigration.exe");
                        Debug.Log(directory.FullName + "/KerbalUpdater/Plugins/KerbalUpdaterMigration.exe");
                        if (migration.Exists)
                        {   // move the migration before execution occurs to avoid file locking
                            Debug.Log("Moving Migration");
                            migration.MoveTo(KerbalUpdater.Constants.PLUGIN_TARGET + "/KerbalUpdater/Plugins/KerbalUpdaterMigration.exe");
                            Debug.Log("Finished");
                        }
                    }
                    bool removeExisting = true;
                    foreach (DirectoryInfo childDirectory in directory.GetDirectories())
                    {   // there should in theory only be one, but you never know...
                        string target = String.Format("{0}/{1}", KerbalUpdater.Constants.STAGING_TARGET, childDirectory.Name);
                        childDirectory.MoveTo(target);
                        if (childDirectory.Name == Mod.PluginName)
                        {
                            removeExisting = false;
                        }
                    }
                    foreach (FileInfo file in directory.GetFiles("*.dll"))
                    {   // Module manager? maybe? idk...
                        string target = String.Format("{0}/{1}", KerbalUpdater.Constants.STAGING_TARGET, file.Name);
                        file.MoveTo(target);
                    }
                    if (removeExisting)
                    {   // mark directory for removal
                        using (StreamWriter writer = File.AppendText(KerbalUpdater.Constants.REMOVE_QUEUE))
                        {
                            writer.WriteLine(Mod.PluginName);
                        }
                    }
                    ClearNotification();
                    File.Delete(ZipFileName);
                    Directory.Delete(tempDirectory, true);
                    KerbalUpdater.RestartRequired = true;
                    CurrentState = State.COMPLETE;
                }).Start();
            }
            catch (Exception ex)
            {
                CurrentState = State.ERROR;
                ErrorMessage = ex.Message;
                Debug.Log(ex.Message);
                Debug.Log(ex.StackTrace);
            }
        }
        public void CancelDownload()
        {
            if (Client != null)
            {
                Client.CancelAsync();
            }
            CurrentState = State.READY;
        }
        public void IgnoreDownload()
        {
            ClearNotification();
            CurrentState = State.IGNORED;
        }
        private void ClearNotification()
        {
            Mod.SetClientVersion(Mod.LastUpdated);
        }
    }
}
