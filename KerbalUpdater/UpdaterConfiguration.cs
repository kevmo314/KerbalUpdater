using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// <typeparam name="K">Key type</typeparam>
        /// <typeparam name="V">Value type</typeparam>
        /// <param name="dict">The dictionary</param>
        /// <returns>The serialization</returns>
        private static string Serialize<K, V>(Dictionary<K, V> dict)
        {
            // apparently there's no String.Join in mono :(
            StringBuilder serialization = new StringBuilder();
            foreach (K key in dict.Keys)
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
        /// <typeparam name="K">Key type</typeparam>
        /// <typeparam name="V">Value type</typeparam>
        /// <param name="serialization">The serialization</param>
        /// <returns>The dictionary</returns>
        private static Dictionary<K, V> Deserialize<K, V>(string serialization)
        {
            Dictionary<K, V> deserialization = new Dictionary<K, V>();
            string[] elements = serialization.Split('|');
            foreach (string element in elements)
            {
                string[] bifurcation = element.Split(',');
                if (bifurcation.Length != 2)
                {   // something is wrong...
                    continue;
                }
                K key = (K)Convert.ChangeType(bifurcation[0], typeof(K));
                V value = (V)Convert.ChangeType(bifurcation[1], typeof(V));
                deserialization.Add(key, value);
            }
            return deserialization;
        }
        /// <summary>
        /// Load the configuration file
        /// </summary>
        public static void Load()
        {
            if (Configuration == null)
            {
                Configuration = KSP.IO.PluginConfiguration.CreateForType<KerbalUpdater>();
                Configuration.load();
                Overrides = Deserialize<string, int>(Configuration.GetValue<string>("Overrides", ""));
                ClientVersions = Deserialize<int, string>(Configuration.GetValue<string>("ClientVersions", ""));
                Toggles = Deserialize<string, bool>(Configuration.GetValue<string>("Toggles", ""));
                UpdaterSpacePortID = Configuration.GetValue<int>("SpacePortID", 40298); // lol hardcoded default...
                UpdaterName = Configuration.GetValue<string>("Name", "Kerbal Updater");
                FirstRun = Configuration.GetValue<bool>("FirstRun", true);
            }
        }
        /// <summary>
        /// Save the configuration file
        /// </summary>
        public static void Save()
        {
            if (Configuration != null)
            {
                Configuration.SetValue("Name", UpdaterName);
                Configuration.SetValue("SpacePortID", UpdaterSpacePortID);
                Configuration.SetValue("Overrides", Serialize<string, int>(Overrides));
                Configuration.SetValue("ClientVersions", Serialize<int, string>(ClientVersions));
                Configuration.SetValue("Toggles", Serialize<string, bool>(Toggles));
                Configuration.SetValue("FirstRun", false);
                Configuration.save();
            }
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
