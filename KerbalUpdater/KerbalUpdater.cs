using System;
using System.Xml;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalUpdater
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KerbalUpdater : MonoBehaviour
    {
        public class Constants
        {
            public const string FirstRun = "FirstRun";
            public const string ModReference = "Mod-{0}";
            public const string SpaceportUrl = "http://kerbalspaceport.com/?p={0}";
            public const string DownloadUrl = "http://kerbalspaceport.com/wp/wp-admin/admin-ajax.php";
            public static readonly string DownloadTarget = KSPUtil.ApplicationRootPath + "/GameData/KerbalUpdater/Plugins/PluginData/KerbalUpdater/Downloads/{0}.zip";
            public static readonly string StagingTarget = KSPUtil.ApplicationRootPath + "/GameData/KerbalUpdater/Plugins/PluginData/KerbalUpdater/Staging/";
            public static readonly string MigrationExe = KSPUtil.ApplicationRootPath + "/GameData/KerbalUpdater/Plugins/KerbalUpdaterMigration.exe";
            public static readonly string RestartSignal = StagingTarget + "RESTART";
            public static readonly string RemoveQueue = StagingTarget + "REMOVE_QUEUE";
            public static readonly string KspExe = KSPUtil.ApplicationRootPath + "/KSP.exe";
            public const string GameData = "GameData";
            public static readonly string PluginTarget = KSPUtil.ApplicationRootPath + GameData + "/";
            public const string OverrideString = "ModID-{0}";
        }
        private List<KerbalMod> _mods;
        private KerbalMod _manualConfiguration;
        public static bool RestartRequired = false;
        public static bool Disabled = false;
        public static bool ShowError = false;
        public void Start()
        {
            if (!IsDirectoryEmpty(Constants.StagingTarget))
            {
                Disabled = true;
                ShowError = true;
            }
            else
            {
                UpdaterConfiguration.Load();
                _mods = KerbalMod.GetMods();
            }
        }
        public void OnApplicationQuit()
        {
            Debug.Log("Trigger quit");
            if (!Disabled)
            {
                if (RestartRequired)
                {
                    // Because we can't access the constants class
                    using (StreamWriter stream = new StreamWriter(Constants.StagingTarget + "CONSTANTS"))
                    {
                        stream.WriteLine(Constants.StagingTarget);
                        stream.WriteLine(Constants.PluginTarget);
                        stream.WriteLine(Constants.RestartSignal);
                        stream.WriteLine(Constants.RemoveQueue);
                        stream.WriteLine(Constants.KspExe);
                    }
                    Application.OpenURL(Constants.MigrationExe);
                }
                UpdaterConfiguration.Save();
                Debug.Log("Saving config...");
            }
        }
        private Rect _updaterWindowPos = new Rect(Screen.width - 405, 5, 400, 500);
        public void OnGUI()
        {
            Resources.Initialize();
            if (Disabled)
            {
                if (ShowError)
                {
                    GUILayout.Window(1, new Rect(Screen.width / 2 - 180, Screen.height / 2 - 60, 360, 120), RenderStagingError, "Kerbal Updater - Error");
                }
            }
            else
            {
                _updaterWindowPos = GUILayout.Window(1, _updaterWindowPos, RenderUpdaterWindow, "Kerbal Updater", GUILayout.MinWidth(100));
                if (_manualConfiguration != null)
                {
                    GUILayout.Window(2, new Rect(left: Screen.width / 4, top: Screen.height / 2 - 80, width: Screen.width / 2, height: 120), RenderManualConfigurationWindow, "Kerbal Updater - Configure " + _manualConfiguration.DisplayName);
                }
            }
        }
        private void RenderStagingError(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Oh no! It looks like your staging directory is not empty! Kerbal Updater has been disabled. This usually means a mod was downloaded but not installed properly.");
            GUILayout.Label("Please check your staging directory to see which mods were affected, then manually redownload them and install if necessary. Your staging directory is:");
            GUILayout.Label((new DirectoryInfo(Constants.StagingTarget)).FullName); // make it pretty
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Okay"))
            {
                ShowError = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        /// <summary>
        /// http://stackoverflow.com/questions/755574/how-to-quickly-check-if-folder-is-empty-net
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsDirectoryEmpty(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return true;
            }
            if (new DirectoryInfo(path).GetDirectories().Any())
            {
                return false;
            }
            return !new DirectoryInfo(path).GetFiles().Any();
        }
        private Vector2 ScrollPosition;
        private void RenderUpdaterWindow(int windowID)
        {
            if (_mods != null)
            {
                GUILayout.BeginVertical();
                RenderModList(windowID); 
                if (RestartRequired)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Restart KSP to update your plugins!");
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Restart Now", Resources.ACTION_BUTTON_STYLE))
                    {
                        // this is the most ridiculous thing ever >.<
                        File.Create(Constants.RestartSignal);
                        Application.Quit();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Something has gone horribly wrong. I couldn't load your mods. :(");
                GUILayout.EndVertical();
            }
            GUI.DragWindow(new Rect(0, 0, 400, 40));
        }
        /// <summary>
        /// The current URL entered into the manual override window
        /// </summary>
        private string _manualConfigurationUrl = "http://kerbalspaceport.com/";
        private string _message = "Ask the plugin creator to add automatic updater support!";
        /// <summary>
        /// Draw the manual override window
        /// </summary> 
        /// <param name="windowID">The associated window ID</param>
        private void RenderManualConfigurationWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Enter the Kerbal SpacePort URL for this plugin:");
            _manualConfigurationUrl = GUILayout.TextField(_manualConfigurationUrl);
            GUILayout.BeginHorizontal();
            GUILayout.Label(_message);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", Resources.ACTION_BUTTON_STYLE))
            {
                try
                {
                    SpacePortPage page = new SpacePortPage(_manualConfigurationUrl);
                    _manualConfiguration.SetSpacePortPage(page);
                    UpdaterConfiguration.SetClientVersion(_manualConfiguration.SpacePortID, null);
                    UpdaterConfiguration.SetOverride(_manualConfiguration.DisplayName, _manualConfiguration.SpacePortID);
                    _manualConfiguration = null;
                }
                catch (UriFormatException ex)
                {
                    _message = "Couldn't use that page: " + ex.Message;
                }
            }
            if (GUILayout.Button("Cancel", Resources.ACTION_BUTTON_STYLE))
            {
                _manualConfiguration = null;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Render a row in the plugin list
        /// </summary>
        /// <param name="left">The left string</param>
        /// <param name="right">The right string</param>
        /// <param name="mod"></param>
        private void RenderRow(string left, string right, KerbalMod mod)
        {
            GUILayout.BeginHorizontal();
            UpdaterConfiguration.SetToggle(mod, 
                GUILayout.Toggle(UpdaterConfiguration.GetToggle(mod), "", Resources.TOGGLE_STYLE)
                );
            GUILayout.Label(left);
            GUILayout.FlexibleSpace();
            GUILayout.Label(right);
            GUILayout.EndHorizontal();
        }
        private ModDownloader RenderMod(KerbalMod mod) 
        {
            if (!mod.Configured || !UpdaterConfiguration.GetToggle(mod))
            {
                RenderRow(mod.DisplayName, (UpdaterConfiguration.GetToggle(mod) ? "Not Configured" : "Not Monitored"), mod);
                return null;
            }
            ModDownloader downloader = ModDownloader.GetDownloader(mod);
            switch (downloader.CurrentState)
            {
                case ModDownloader.State.Ignored:
                    RenderRow(mod.DisplayName, mod.LastUpdated.ToLongDateString(), mod);
                    break;
                case ModDownloader.State.Downloading:
                    RenderRow(mod.DisplayName, "Downloading (" + downloader.Progress + "%)", mod);
                    break;
                case ModDownloader.State.Staging:
                    RenderRow(mod.DisplayName, "Staging Update...", mod);
                    break;
                case ModDownloader.State.Ready:
                    RenderRow(mod.DisplayName, "Update Available", mod);
                    break;
                case ModDownloader.State.Complete:
                    RenderRow(mod.DisplayName, "Restart Required", mod);
                    break;
                case ModDownloader.State.Error:
                    RenderRow(mod.DisplayName, "Error", mod);
                    GUILayout.Label(downloader.ErrorMessage);
                    break;
            }
            return downloader;
        }
        private void RenderOptions(KerbalMod mod, ModDownloader downloader)
        {
            GUILayout.BeginHorizontal();
            if (mod.Configured)
            {
                GUILayout.Space(10);
                if (GUILayout.Button("ID: " + mod.SpacePortID, Resources.URL_STYLE))
                {
                    Application.OpenURL(String.Format(Constants.SpaceportUrl, mod.SpacePortID));
                }
                if (!mod.Automatic)
                {
                    GUILayout.Label("(Manual)");
                }
                GUILayout.FlexibleSpace();
                switch (downloader.CurrentState)
                {
                    case ModDownloader.State.Ignored:
                        if (GUILayout.Button("Force Reinstall", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.BeginDownload();
                        }
                        break;
                    case ModDownloader.State.Downloading:
                        if (GUILayout.Button("Cancel", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.CancelDownload();
                        }
                        if (downloader.Progress == 100)
                        {   // workaround for odd callback issue
                            downloader.BeginStaging();
                        }
                        break;
                    case ModDownloader.State.Staging:
                        break;
                    case ModDownloader.State.Ready:
                        if (GUILayout.Button("Update", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.BeginDownload();
                        }
                        if (GUILayout.Button("Ignore", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.IgnoreDownload();
                        }
                        break;
                    case ModDownloader.State.Complete:
                        break;
                    case ModDownloader.State.Error:
                        break;
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
            }
            if (!mod.Automatic)
            {
                if (GUILayout.Button("Configure", Resources.ACTION_BUTTON_STYLE))
                {
                    _manualConfiguration = mod;
                }
            }
            GUILayout.EndHorizontal();
        }
        /// <summary>
        /// Draw the scrollable mod list
        /// </summary>
        /// <param name="windowID">The associated window id</param>
        private void RenderModList(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(45);
            GUILayout.Label("Mod Name", Resources.TABLE_HEAD_STYLE);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Status/Last Updated", Resources.TABLE_HEAD_STYLE);
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, Resources.TABLE_BODY_STYLE);
            foreach (KerbalMod mod in _mods)
            {
                GUILayout.BeginVertical(Resources.TABLE_ROW_STYLE);
                ModDownloader downloader = RenderMod(mod);
                if (UpdaterConfiguration.GetToggle(mod))
                {
                    RenderOptions(mod, downloader);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }
    }
}
