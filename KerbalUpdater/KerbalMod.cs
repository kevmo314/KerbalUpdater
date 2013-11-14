using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalUpdater
{
    /// <summary>
    /// A representative object for a mod
    /// </summary>
    public class KerbalMod
    {
        /// <summary>
        /// The name of the containing folder
        /// </summary>
        public string PluginName { get; private set; }
        /// <summary>
        /// The name requested by the mod
        /// </summary>
        public string DisplayName { get; private set; }
        /// <summary>
        /// The "Product ID" on SpacePort
        /// </summary>
        public int SpacePortID { get; private set; }
        /// <summary>
        /// The reference to the SpacePort page
        /// </summary>
        private SpacePortPage SpacePortPage;
        /// <summary>
        /// Is there an associated SpacePort page?
        /// </summary>
        public bool Configured { get { return SpacePortPage != null; } }
        /// <summary>
        /// The date specified on the SpacePort page
        /// </summary>
        public DateTime LastUpdated { get { return SpacePortPage.GetLastUpdatedDate(); } }
        /// <summary>
        /// The last known update date
        /// </summary>
        public DateTime? ClientVersion { get; private set; }
        /// <summary>
        /// Is the current version (probably) up to date?
        /// Limitations: only one update permitted per day
        /// </summary>
        public bool UpToDate { get { return ClientVersion != null && ClientVersion >= LastUpdated; } }
        /// <summary>
        /// The URL to the zip file
        /// </summary>
        public Uri DownloadURL { get { return SpacePortPage.DownloadURL; } }
        /// <summary>
        /// Is this configuration specified by the plugin author (and not by the user)?
        /// </summary>
        public bool Automatic { get; private set; }
        /// <summary>
        /// A KSP mod reference
        /// </summary>
        /// <param name="displayName">The user friendly name</param>
        /// <param name="pluginName">The base folder name</param>
        /// <param name="spacePortID">The Product ID on SpacePort</param>
        /// <param name="clientVersion">The known client version</param>
        public KerbalMod(string displayName, string pluginName, int spacePortID = -1, DateTime? clientVersion = null, bool organic = false)
        {
            this.DisplayName = displayName;
            this.PluginName = pluginName;
            this.SpacePortID = spacePortID;
            this.ClientVersion = clientVersion;
            this.SpacePortPage = (spacePortID == -1 ? null : new SpacePortPage(spacePortID));
            this.Automatic = organic;
        }
        /// <summary>
        /// Get the plugin's configuration file
        /// </summary>
        /// <param name="directory">The directory the plugin is located in</param>
        /// <returns>A reader for the config file</returns>
        public static XmlReader GetPluginConfiguration(DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles())
            {
                if (file.Name == "config.xml")
                {
                    return XmlReader.Create(file.OpenRead());
                }
            }
            // No config.xml found yet :(
            foreach (DirectoryInfo childDirectory in directory.GetDirectories())
            {
                XmlReader Xml = GetPluginConfiguration(childDirectory);
                if (Xml != null)
                {
                    return Xml;
                }
            }
            return null;
        }
        public void SetClientVersion(DateTime? version)
        {
            UpdaterConfiguration.SetClientVersion(SpacePortID, version);
            ClientVersion = version;
        }
        /// <summary>
        /// Manually override the SpacePort page
        /// </summary>
        /// <param name="page">The page to override with</param>
        public void SetSpacePortPage(SpacePortPage page)
        {
            SpacePortID = page.GetSpacePortID();
            SpacePortPage = page;
        }
        /// <summary>
        /// Get the installed KSP mods
        /// </summary>
        /// <returns>A list of mods</returns>
        public static List<KerbalMod> GetMods()
        {
            List<KerbalMod> mods = new List<KerbalMod>();
            foreach (DirectoryInfo plugin in (new DirectoryInfo(KerbalUpdater.Constants.PLUGIN_TARGET)).GetDirectories())
            {
                Debug.Log("Reading " + plugin.Name);
                using (XmlReader reader = GetPluginConfiguration(plugin))
                {
                    int spacePortID = -1;
                    string displayName = plugin.Name;
                    bool organic = false;
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            if (reader.GetAttribute("name") == "SpacePortID")
                            {
                                spacePortID = reader.ReadElementContentAsInt();
                                organic = true;
                            }
                            else if (reader.GetAttribute("name") == "Name")
                            {
                                displayName = reader.ReadContentAsString();
                            }
                        }
                    }
                    else
                    {
                        spacePortID = UpdaterConfiguration.GetOverride(plugin.Name);
                    }
                    DateTime? clientDate = UpdaterConfiguration.GetClientVersion(spacePortID);
                    mods.Add(new KerbalMod(displayName, plugin.Name, spacePortID, clientDate, organic));
                }
            }
            return mods;
        }
    }
}
