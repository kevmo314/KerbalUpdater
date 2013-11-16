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
            Ready, Downloading, Staging, Complete, Ignored, Error
        }
        private readonly static Dictionary<string, ModDownloader> Downloaders = new Dictionary<string, ModDownloader>();
        public static ModDownloader GetDownloader(KerbalMod mod)
        {
            if (!Downloaders.ContainsKey(mod.PluginName))
            {
                Downloaders[mod.PluginName] = new ModDownloader(mod);
            }
            return Downloaders[mod.PluginName];
        }
        private readonly KerbalMod Mod;
        private WebClient _client;
        private string _zipFileName;
        public string ErrorMessage { get; private set; }
        public int Progress { get; private set; }
        public State CurrentState;
        public ModDownloader(KerbalMod mod)
        {
            Mod = mod;
            CurrentState = (mod.UpToDate ? State.Ignored : State.Ready);
        }
        public void BeginDownload()
        {
            CurrentState = State.Downloading;
            using (_client = new WebClient())
            {
                _zipFileName = String.Format(KerbalUpdater.Constants.DownloadTarget, Mod.SpacePortID);
                _client.DownloadProgressChanged += (sender, e) =>
                {
                    Progress = e.ProgressPercentage;
                };
                _client.DownloadFileCompleted += (sender, e) =>
                {
                    Exception ex = e.Error;
                    if (ex != null)
                    {
                        CurrentState = State.Error;
                        ErrorMessage = ex.Message; 
                    }
                };
                _client.DownloadFileAsync(Mod.DownloadUrl, _zipFileName);
                // for some reason, the DownloadFileComplete handler doesn't work
                // staging invoked via KerbalUpdater
            }
        }

        public DirectoryInfo DetectPluginParentDirectory(DirectoryInfo parent)
        {
            DirectoryInfo directory = parent;
            foreach (DirectoryInfo childDirectory in directory.GetDirectories())
            {
                if (childDirectory.Name == KerbalUpdater.Constants.GameData)
                {   // This plugin is expecting us to extract to the KSP root folder
                    directory = childDirectory;
                    break;
                }
            }
            return directory;

        }
        /// <summary>
        /// Move the Kerbal Updater migration executable prematurely.
        /// </summary>
        /// <param name="directory"></param>
        private void MigrateMigrator(DirectoryInfo directory)
        {
            string path = "/KerbalUpdater/Plugins/KerbalUpdaterMigration.exe";
            FileInfo migration = new FileInfo(directory.FullName + path);
            if (migration.Exists)
            {   // move the migration before execution occurs to avoid file locking
                if (File.Exists(KerbalUpdater.Constants.PluginTarget + path))
                {
                    File.Delete(KerbalUpdater.Constants.PluginTarget + path);
                }
                migration.MoveTo(KerbalUpdater.Constants.PluginTarget + path);
            }
        }
        public void BeginStaging()
        {
            CurrentState = State.Staging;
            string tempDirectory = String.Format("{0}/{1}/", KerbalUpdater.Constants.StagingTarget, Mod.SpacePortID);
            try
            {
                // patch for bug, see: http://stackoverflow.com/questions/4600923/monotouch-icsharpcode-sharpziplib-giving-an-error
                ICSharpCode.SharpZipLib.Zip.ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
                // extract it first, then deal with individual files 
                (new ICSharpCode.SharpZipLib.Zip.FastZip()).ExtractZip(_zipFileName, tempDirectory, null);
                DirectoryInfo directory = DetectPluginParentDirectory(new DirectoryInfo(tempDirectory));
                if (Mod.PluginName == "KerbalUpdater")
                {   // handle a cute edge case
                    MigrateMigrator(directory);
                }
                foreach (DirectoryInfo childDirectory in directory.GetDirectories())
                {   // there should in theory only be one, but you never know...
                    childDirectory.MoveTo(String.Format("{0}/{1}", KerbalUpdater.Constants.StagingTarget, childDirectory.Name));
                }
                foreach (FileInfo file in directory.GetFiles("*.dll"))
                {   // Module manager? maybe? idk...
                    file.MoveTo(String.Format("{0}/{1}", KerbalUpdater.Constants.StagingTarget, file.Name));
                }
                File.Delete(_zipFileName);
                Directory.Delete(tempDirectory, true);
                KerbalUpdater.RestartRequired = true;
                CurrentState = State.Complete;
                ClearNotification();
            }
            catch (Exception ex)
            {
                CurrentState = State.Error;
                ErrorMessage = ex.Message;
                Debug.Log(ex.Message);
                Debug.Log(ex.StackTrace);
            }
        }
        public void CancelDownload()
        {
            if (_client != null)
            {
                _client.CancelAsync();
            }
            CurrentState = State.Ready;
        }
        public void IgnoreDownload()
        {
            ClearNotification();
            CurrentState = State.Ignored;
        }
        private void ClearNotification()
        {
            Mod.SetClientVersion(Mod.LastUpdated);
            UpdaterConfiguration.Save();
        }
    } 
}
