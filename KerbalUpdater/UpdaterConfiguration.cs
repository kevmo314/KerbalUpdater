using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace KerbalUpdater
{
    /// <summary>
    /// An interface with KSP.IO.PluginConfiguration to make it a little easier to use
    /// </summary>
    static class UpdaterConfiguration
    {
        /// <summary>
        /// The configuration object
        /// </summary>
        private static KSP.IO.PluginConfiguration Configuration;
        /// <summary>
        /// Store the manual overrides
        /// </summary>
        private static Dictionary<string, int> Overrides;
        /// <summary>
        /// Store the versions of all the plugins
        /// </summary>
        private static Dictionary<int, string> ClientVersions;
        /// <summary>
        /// Whether or not the mod is being monitored
        /// </summary>
        private static Dictionary<string, bool> Toggles;
        /// <summary>
        /// Dog fooding!
        /// </summary>
        private static int UpdaterSpacePortID;
        private static string UpdaterName;
        /// <summary>
        /// Is this the first time the mod has been run?
        /// </summary>
        public static bool FirstRun { get; private set; }
        /// <summary>
        /// Serialize a dictionary.
        /// </summary>
        /// <typeparam name="TK">Key type</typeparam>
        /// <typeparam name="TV">Value type</typeparam>
        /// <param name="dict">The dictionary</param>
        /// <returns>The serialization</returns>
        private static string Serialize<TK, TV>(Dictionary<TK, TV> dict)
        {
            // apparently there's no String.Join in mono :(
            StringBuilder serialization = new StringBuilder();
            foreach (TK key in dict.Keys)
            {
                serialization.Append(String.Format("{0},{1}|", key.ToString(), dict[key].ToString()));
            }
            if (serialization.Length > 1)
            {
                return serialization.Remove(serialization.Length - 1, 1).ToString();
            }
            else
            {
                return serialization.ToString();
            }
            
        }
        /// <summary>
        /// Deserialize a dictionary.
        /// </summary>
        /// <typeparam name="TK">Key type</typeparam>
        /// <typeparam name="TV">Value type</typeparam>
        /// <param name="serialization">The serialization</param>
        /// <returns>The dictionary</returns>
        private static Dictionary<TK, TV> Deserialize<TK, TV>(string serialization)
        {
            Dictionary<TK, TV> deserialization = new Dictionary<TK, TV>();
            string[] elements = serialization.Split('|');
            foreach (string element in elements)
            {
                string[] bifurcation = element.Split(',');
                if (bifurcation.Length != 2)
                {   // something is wrong...
                    continue;
                }
                TK key = (TK)Convert.ChangeType(bifurcation[0], typeof(TK));
                TV value = (TV)Convert.ChangeType(bifurcation[1], typeof(TV));
                deserialization.Add(key, value);
            }
            return deserialization;
        }
        /// <summary>
        /// Get the plugin's configuration file
        /// </summary>
        /// <param name="directory">The directory the plugin is located in</param>
        /// <returns>A reader for the config file</returns>
        public static XmlElement GetPluginConfiguration(DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles())
            {
                if (file.Name == "config.xml")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(File.ReadAllText(file.FullName));
                    return doc.DocumentElement;
                }
            }
            // No config.xml found yet :(
            foreach (DirectoryInfo childDirectory in directory.GetDirectories())
            {
                XmlElement pluginConfiguration = GetPluginConfiguration(childDirectory);
                if (pluginConfiguration != null)
                {
                    return pluginConfiguration;
                }
            }
            return null;
        }
        /// <summary>
        /// Load the configuration file
        /// </summary>
        public static void Load()
        {
            UpdaterSpacePortID = 40298;
            UpdaterName = "Kerbal Updater";
            Overrides = new Dictionary<string, int>();
            ClientVersions = new Dictionary<int, string>();
            Toggles = new Dictionary<string, bool>();

            XmlElement config =
                GetPluginConfiguration(new DirectoryInfo(KerbalUpdater.Constants.PluginTarget + "/KerbalUpdater/"));
            if (config == null)
            {
                Save(); // create a new file
                return;
            }
            foreach (XmlElement childNode in config.ChildNodes)
            {
                if (childNode.GetAttribute("name") == "Overrides")
                {
                    Overrides = Deserialize<string, int>(childNode.InnerText);
                }
                else if (childNode.GetAttribute("name") == "ClientVersions")
                {
                    ClientVersions = Deserialize<int, string>(childNode.InnerText);
                }
                else if (childNode.GetAttribute("name") == "Toggles")
                {
                    Toggles = Deserialize<string, bool>(childNode.InnerText);
                }
                else if (childNode.GetAttribute("name") == "SpacePortID")
                {
                    UpdaterSpacePortID = int.Parse(childNode.InnerText);
                }
                else if (childNode.GetAttribute("name") == "Name")
                {
                    UpdaterName = childNode.InnerText;
                }
            }
        }

        private static XmlElement GenerateElement(XmlDocument doc, string name, string val)
        {
            XmlElement element = doc.CreateElement("string");
            element.SetAttribute("name", name);
            element.InnerText = val;
            return element;
        }
        /// <summary>
        /// Save the configuration file
        /// </summary>
        public static void Save()
        {
            XmlDocument document = new XmlDocument();
            XmlElement config = document.CreateElement("config");
            document.AppendChild(config);
            config.AppendChild(GenerateElement(document, "Name", UpdaterName));
            config.AppendChild(GenerateElement(document, "SpacePortID", UpdaterSpacePortID.ToString()));
            config.AppendChild(GenerateElement(document, "Overrides", Serialize<string, int>(Overrides)));
            config.AppendChild(GenerateElement(document, "ClientVersions", Serialize<int, string>(ClientVersions)));
            config.AppendChild(GenerateElement(document, "Toggles", Serialize<string, bool>(Toggles)));
            document.Save(KerbalUpdater.Constants.PluginTarget + "/KerbalUpdater/Plugins/PluginData/KerbalUpdater/config.xml");
        }
        public static bool GetToggle(KerbalMod mod)
        {
            return GetToggle(mod.PluginName);
        }
        public static bool GetToggle(string name)
        {
            if (Toggles.ContainsKey(name))
            {
                return Toggles[name];
            }
            else
            {
                return false;
            }
        }
        public static void SetToggle(KerbalMod mod, bool toggle)
        {
            SetToggle(mod.PluginName, toggle);
        }
        public static void SetToggle(string name, bool toggle)
        {
            Toggles[name] = toggle;
        }
        /// <summary>
        /// Get the client version date of a given spacePortID from the configuration
        /// </summary>
        /// <param name="spacePortID">The SpacePort ID</param>
        /// <returns>The client's version date</returns>
        public static DateTime? GetClientVersion(int spacePortID)
        {
            if (ClientVersions.ContainsKey(spacePortID))
            {
                return DateTime.Parse(ClientVersions[spacePortID]);
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Set the client version date of a given spacePortID in the configuration
        /// </summary>
        /// <param name="spacePortID">The SpacePort ID</param>
        /// <param name="version">The client's version date</param>
        public static void SetClientVersion(int spacePortID, DateTime? version)
        {
            Debug.Log(spacePortID + " " + version);
            if (version == null)
            {
                ClientVersions.Remove(spacePortID);
            }
            else
            {
                ClientVersions[spacePortID] = ((DateTime)version).ToShortDateString();
            }
        }
        /// <summary>
        /// Get the manual configuration of a plugin
        /// </summary>
        /// <param name="name">The plugin name</param>
        /// <returns>The specified SpacePort ID</returns>
        public static int GetOverride(string name)
        {
            if (Overrides.ContainsKey(name))
            {
                return Overrides[name];
            }
            else
            {
                return -1;
            }
        }
        /// <summary>
        /// Set the manual configuration of a plugin
        /// </summary>
        /// <param name="name">The plugin name</param>
        /// <param name="spacePortID">The SpacePort ID</param>
        public static void SetOverride(string name, int spacePortID)
        {
            if (spacePortID == -1)
            {
                Overrides.Remove(name);
            }
            else
            {
                Overrides[name] = spacePortID;
            }
        }
    }
}
