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
        private SpacePortPage _spacePortPage;
        /// <summary>
        /// Is there an associated SpacePort page?
        /// </summary>
        public bool Configured { get { return _spacePortPage != null; } }
        /// <summary>
        /// The date specified on the SpacePort page
        /// </summary>
        public DateTime LastUpdated { get { return _spacePortPage.GetLastUpdatedDate(); } }
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
        public Uri DownloadUrl { get { return _spacePortPage.DownloadURL; } }
        /// <summary>
        /// Is this configuration specified by the plugin author (and not by the user)?
        /// </summary>
        public bool Automatic { get; private set; }

        /// <summary>
        /// A KSP mod reference
        /// </summary>
        /// <param name="displayName">The user friendly name</param>
        /// <param name="pluginName">The base folder name</param>
        /// <param name="spacePortId">The Product ID on SpacePort</param>
        /// <param name="clientVersion">The known client version</param>
        /// <param name="organic">Was this object created as a result of the plugin's config?</param>
        public KerbalMod(string displayName, string pluginName, int spacePortId = -1, DateTime? clientVersion = null, bool organic = false)
        {
            Debug.Log("Instantiating");
            this.DisplayName = displayName;
            this.PluginName = pluginName;
            this.SpacePortID = spacePortId;
            this.ClientVersion = clientVersion;
            this._spacePortPage = (spacePortId == -1 ? null : new SpacePortPage(spacePortId));
            this.Automatic = organic;
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
            _spacePortPage = page;
        }
        /// <summary>
        /// Get the installed KSP mods
        /// </summary>
        /// <returns>A list of mods</returns>
        public static List<KerbalMod> GetMods()
        {
            List<KerbalMod> mods = new List<KerbalMod>();
            foreach (DirectoryInfo plugin in (new DirectoryInfo(KerbalUpdater.Constants.PluginTarget)).GetDirectories())
            {
                Debug.Log("Reading " + plugin.Name);
                XmlElement root = UpdaterConfiguration.GetPluginConfiguration(plugin);
                
                int spacePortId = -1;
                string displayName = plugin.Name;
                bool organic = false;
                if (root != null)
                {
                    foreach (XmlElement element in root.ChildNodes)
                    {
                        Debug.Log(element.GetAttribute("name"));
                        if (element.GetAttribute("name") == "SpacePortID")
                        {
                            Debug.Log(element.InnerText);
                            if (!int.TryParse(element.InnerText, out spacePortId))
                            {
                                Debug.Log("Failed parse " + element.InnerText);
                            }
                            organic = true;
                        }
                        else if (element.GetAttribute("name") == "Name")
                        {
                            displayName = element.InnerText;
                        }
                    }
                }
                else
                {
                    spacePortId = UpdaterConfiguration.GetOverride(plugin.Name);
                }
                DateTime? clientDate = UpdaterConfiguration.GetClientVersion(spacePortId);
                mods.Add(new KerbalMod(displayName, plugin.Name, spacePortId, clientDate, organic));
            }
            
            return mods;
        }
    }
}
