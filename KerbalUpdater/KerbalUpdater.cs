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
            public const string FIRST_RUN = "FirstRun";
            public const string MOD_REFERENCE = "Mod-{0}";
            public const string SPACEPORT_URL = "http://kerbalspaceport.com/?p={0}";
            public const string DOWNLOAD_URL = "http://kerbalspaceport.com/wp/wp-admin/admin-ajax.php";
            public static readonly string DOWNLOAD_TARGET = KSPUtil.ApplicationRootPath + "/GameData/KerbalUpdater/Plugins/PluginData/KerbalUpdater/Downloads/{0}.zip";
            public static readonly string STAGING_TARGET = KSPUtil.ApplicationRootPath + "/GameData/KerbalUpdater/Plugins/PluginData/KerbalUpdater/Staging/";
            public static readonly string MIGRATION_EXE = KSPUtil.ApplicationRootPath + "/GameData/KerbalUpdater/Plugins/KerbalUpdaterMigration.exe";
            public static readonly string RESTART_SIGNAL = STAGING_TARGET + "RESTART";
            public static readonly string REMOVE_QUEUE = STAGING_TARGET + "REMOVE_QUEUE";
            public static readonly string KSP_EXE = KSPUtil.ApplicationRootPath + "/KSP.exe";
            public const string GAME_DATA = "GameData";
            public static readonly string PLUGIN_TARGET = KSPUtil.ApplicationRootPath + GAME_DATA + "/";
            public const string OVERRIDE_STRING = "ModID-{0}";
        }
        private List<KerbalMod> Mods;
        private KerbalMod ManualConfiguration;
        public static bool RestartRequired = false;
        public static bool Disabled = false;
        public static bool ShowError = false;
        public void Start()
        {
            if (!IsDirectoryEmpty(Constants.STAGING_TARGET))
            {
                Disabled = true;
                ShowError = true;
            }
            else
            {
                UpdaterConfiguration.Load();
                Mods = KerbalMod.GetMods();
            }
        }
        public void OnApplicationQuit()
        {
            if (!Disabled)
            {
                if (RestartRequired)
                {
                    // Because we can't access the constants class
                    using (StreamWriter stream = new StreamWriter(Constants.STAGING_TARGET + "CONSTANTS"))
                    {
                        stream.WriteLine(Constants.STAGING_TARGET);
                        stream.WriteLine(Constants.PLUGIN_TARGET);
                        stream.WriteLine(Constants.RESTART_SIGNAL);
                        stream.WriteLine(Constants.REMOVE_QUEUE);
                        stream.WriteLine(Constants.KSP_EXE);
                    }
                    Application.OpenURL(Constants.MIGRATION_EXE);
                }
                UpdaterConfiguration.Save();
            }
        }
        private Rect UpdaterWindowPos = new Rect(Screen.width - 405, 5, 400, 500);
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
                UpdaterWindowPos = GUILayout.Window(1, UpdaterWindowPos, RenderUpdaterWindow, "Kerbal Updater", GUILayout.MinWidth(100));
                if (ManualConfiguration != null)
                {
                    GUILayout.Window(2, new Rect(Screen.width / 4, Screen.height / 2 - 80, Screen.width / 2, 120), RenderManualConfigurationWindow, "Kerbal Updater - Configure " + ManualConfiguration.DisplayName);
                }
            }
        }
        private void RenderStagingError(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Oh no! It looks like your staging directory is not empty! Kerbal Updater has been disabled. This usually means a mod was downloaded but not installed properly.");
            GUILayout.Label("Please check your staging directory to see which mods were affected, then manually redownload them and install if necessary. Your staging directory is:");
            GUILayout.Label((new DirectoryInfo(Constants.STAGING_TARGET)).FullName); // make it pretty
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
            foreach (DirectoryInfo directory in new DirectoryInfo(path).GetDirectories())
            {
                return false;
            }
            foreach (FileInfo file in new DirectoryInfo(path).GetFiles())
            {
                return false;
            }
            return true;
        }
        private Vector2 ScrollPosition;
        private void RenderUpdaterWindow(int windowID)
        {
            if (Mods != null)
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
                        File.Create(Constants.RESTART_SIGNAL);
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
        private string ManualConfigurationURL = "http://kerbalspaceport.com/";
        private string Message = "Ask the plugin creator to add automatic updater support!";
        /// <summary>
        /// Draw the manual override window
        /// </summary> 
        /// <param name="windowID">The associated window ID</param>
        private void RenderManualConfigurationWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Enter the Kerbal SpacePort URL for this plugin:");
            ManualConfigurationURL = GUILayout.TextField(ManualConfigurationURL);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Message);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply", Resources.ACTION_BUTTON_STYLE))
            {
                try
                {
                    SpacePortPage page = new SpacePortPage(ManualConfigurationURL);
                    ManualConfiguration.SetSpacePortPage(page);
                    UpdaterConfiguration.SetClientVersion(ManualConfiguration.SpacePortID, null);
                    UpdaterConfiguration.SetOverride(ManualConfiguration.DisplayName, ManualConfiguration.SpacePortID);
                    ManualConfiguration = null;
                }
                catch (UriFormatException ex)
                {
                    Message = "Couldn't use that page: " + ex.Message;
                }
            }
            if (GUILayout.Button("Cancel", Resources.ACTION_BUTTON_STYLE))
            {
                ManualConfiguration = null;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        /// <summary>
        /// Render a row in the plugin list
        /// </summary>
        /// <param name="left">The left string</param>
        /// <param name="right">The right string</param>
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
                case ModDownloader.State.IGNORED:
                    RenderRow(mod.DisplayName, mod.LastUpdated.ToLongDateString(), mod);
                    break;
                case ModDownloader.State.DOWNLOADING:
                    RenderRow(mod.DisplayName, "Downloading (" + downloader.Progress + "%)", mod);
                    break;
                case ModDownloader.State.STAGING:
                    RenderRow(mod.DisplayName, "Staging Update...", mod);
                    break;
                case ModDownloader.State.READY:
                    RenderRow(mod.DisplayName, "Update Available", mod);
                    break;
                case ModDownloader.State.COMPLETE:
                    RenderRow(mod.DisplayName, "Restart Required", mod);
                    break;
                case ModDownloader.State.ERROR:
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
                    Application.OpenURL(String.Format(Constants.SPACEPORT_URL, mod.SpacePortID));
                }
                if (!mod.Automatic)
                {
                    GUILayout.Label("(Manual)");
                }
                GUILayout.FlexibleSpace();
                switch (downloader.CurrentState)
                {
                    case ModDownloader.State.IGNORED:
                        if (GUILayout.Button("Force Reinstall", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.BeginDownload();
                        }
                        break;
                    case ModDownloader.State.DOWNLOADING:
                        if (GUILayout.Button("Cancel", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.CancelDownload();
                        }
                        if (downloader.Progress == 100)
                        {   // workaround for odd callback issue
                            downloader.BeginStaging();
                        }
                        break;
                    case ModDownloader.State.STAGING:
                        break;
                    case ModDownloader.State.READY:
                        if (GUILayout.Button("Update", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.BeginDownload();
                        }
                        if (GUILayout.Button("Ignore", Resources.ACTION_BUTTON_STYLE))
                        {
                            downloader.IgnoreDownload();
                        }
                        break;
                    case ModDownloader.State.COMPLETE:
                        break;
                    case ModDownloader.State.ERROR:
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
                    ManualConfiguration = mod;
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
            foreach (KerbalMod mod in Mods)
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
