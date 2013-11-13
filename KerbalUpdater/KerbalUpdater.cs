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
        public void Start()
        {
            UpdaterConfiguration.Load();
            Mods = KerbalMod.GetMods();
        }
        public void OnApplicationQuit()
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
        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            UIStyle = new GUIStyle(GUI.skin.button);
            GUILayout.Window(1, new Rect(5, 5, 700, 500), RenderUpdaterWindow, "Kerbal Updater", GUILayout.MinWidth(100));
            if (ManualConfiguration != null)
            {
                GUILayout.Window(2, new Rect(Screen.width / 4, Screen.height / 2 - 100, Screen.width / 2, 200), RenderManualConfigurationWindow, "Kerbal Updater - Configure " + ManualConfiguration.DisplayName);
            }
        }
        private GUIStyle UIStyle;
        private Vector2 scrollPosition;
        private void RenderUpdaterWindow(int windowID)
        {
            if (Mods != null)
            {
                GUILayout.BeginVertical();
                RenderModList(windowID); 
                if (RestartRequired)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("A restart is needed to update your plugins!");
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Restart"))
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
        }
        /// <summary>
        /// The current URL entered into the manual override window
        /// </summary>
        private string ManualConfigurationURL = "http://kerbalspaceport.com/";
        /// <summary>
        /// Draw the manual override window
        /// </summary> 
        /// <param name="windowID">The associated window ID</param>
        private void RenderManualConfigurationWindow(int windowID)
        {
            GUILayout.BeginVertical(UIStyle);
            GUILayout.Label("Enter the Kerbal SpacePort URL for this plugin:");
            GUILayout.BeginHorizontal();
            ManualConfigurationURL = GUILayout.TextField(ManualConfigurationURL);
            string message = "Ask the plugin creator to add automatic updater support!";
            if (GUILayout.Button("Apply"))
            {
                Debug.Log("Requesting " + ManualConfigurationURL);
                SpacePortPage page = new SpacePortPage(ManualConfigurationURL);
                if (page != null)
                {
                    ManualConfiguration.SetSpacePortPage(page);
                    UpdaterConfiguration.SetOverride(ManualConfiguration.DisplayName, ManualConfiguration.SpacePortID);
                    ManualConfiguration = null;
                }
                else
                {
                    message = "Not a valid SpacePort page :(";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(message);
            GUILayout.EndVertical();
        }
        /// <summary>
        /// Draw the scrollable mod list
        /// </summary>
        /// <param name="windowID">The associated window id</param>
        private void RenderModList(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, UIStyle);
            foreach (KerbalMod mod in Mods)
            {
                GUILayout.BeginHorizontal();
                if (mod.Configured)
                {
                    ModDownloader downloader = ModDownloader.GetDownloader(mod);
                    switch (downloader.CurrentState)
                    {
                        case ModDownloader.State.IGNORED:
                            GUILayout.Label(mod.DisplayName + " last updated " + mod.LastUpdated.ToLongDateString());
                            GUILayout.FlexibleSpace();
                            break;
                        case ModDownloader.State.DOWNLOADING:
                            GUILayout.Label(mod.DisplayName + " downloading... (" + downloader.Progress + "%)");
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Cancel"))
                            {
                                downloader.CancelDownload();
                            }
                            if (downloader.Progress == 100)
                            {   // workaround for odd callback issue
                                downloader.BeginStaging();
                            }
                            break;
                        case ModDownloader.State.STAGING:
                            GUILayout.Label(mod.DisplayName + " staging...");
                            GUILayout.FlexibleSpace();
                            break;
                        case ModDownloader.State.READY:
                            GUILayout.Label(mod.DisplayName + " has a new version available!");
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Update"))
                            {
                                downloader.BeginDownload();
                            }
                            if (GUILayout.Button("Ignore"))
                            {
                                downloader.IgnoreDownload();
                            }
                            break;
                        case ModDownloader.State.COMPLETE:
                            GUILayout.Label(mod.DisplayName + " will be updated on restart!");
                            break;
                        case ModDownloader.State.ERROR:
                            GUILayout.Label(mod.DisplayName + " failed to download: " + downloader.ErrorMessage);
                            break;
                    }
                }
                else
                {
                    GUILayout.Label(mod.DisplayName + " isn't configured :(");
                    GUILayout.FlexibleSpace();
                }
                if (!mod.Automatic)
                {
                    if (GUILayout.Button("Configure"))
                    {
                        ManualConfiguration = mod;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
}
