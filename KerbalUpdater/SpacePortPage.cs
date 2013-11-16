using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace KerbalUpdater
{
    public class SpacePortPage
    {
        public WWW Page { get; private set; }
        public int SpacePortID { get; private set; }
        public Uri DownloadURL
        {
            get
            {
                WWWForm postData = new WWWForm();
                WaitForLoad(Page);
                postData.AddField("addonid", SpacePortID);
                postData.AddField("action", "downloadfileaddon");
                WWW request = WaitForLoad(new WWW(KerbalUpdater.Constants.DownloadUrl, postData));
                Debug.Log(request.text);
                return new Uri(request.text);
            }
        }
        private DateTime? _lastUpdated = null;
        /// <summary>
        /// Create a SpacePort page reference from a SpacePort ID
        /// </summary>
        /// <param name="spacePortID"></param>
        public SpacePortPage(int spacePortID)
        {
            this.SpacePortID = spacePortID;
            Page = new WWW(String.Format(KerbalUpdater.Constants.SpaceportUrl, spacePortID));
        }
        /// <summary>
        /// Create a SpacePort page reference from a URL
        /// </summary>
        /// <param name="url">The URL</param>
        public SpacePortPage(string url)
        {
            Page = WaitForLoad(new WWW(url));
            Match match = (new Regex("Product ID:(?<ID>.*?)</li>")).Match(Page.text);
            if (match.Success && match.Groups.Count > 0)
            {
                SpacePortID = int.Parse(match.Groups["ID"].Value.Trim());
            }
            else
            {
                throw new UriFormatException("No ID found");
            }

        }
        /// <summary>
        /// Wait until the page has finished loading.
        /// Not using `yield return Page;` because this makes the code a little less complicated, ie no delegates...
        /// </summary>
        private WWW WaitForLoad(WWW page)
        {
            while (page != null && !page.isDone)
            {
                System.Threading.Thread.Sleep(250);
            }
            return page;
        }
        /// <summary>
        /// Get the SpacePort ID
        /// </summary>
        /// <returns>The SpacePort ID</returns>
        public int GetSpacePortID()
        {
            WaitForLoad(Page);
            return SpacePortID;
        }
        /// <summary>
        /// Get the date that the plugin was last updated on SpacePort
        /// </summary>
        /// <returns>The last updated date</returns>
        public DateTime GetLastUpdatedDate()
        {
            if (_lastUpdated == null)
            {
                while (!Page.isDone)
                {
                    System.Threading.Thread.Sleep(250);
                }
                Match match = (new Regex("Created:(?<Date>.*?)</li>")).Match(Page.text);
                if (match.Success && match.Groups.Count > 0)
                {
                    _lastUpdated = DateTime.Parse(match.Groups["Date"].Value.Trim());
                }
            }
            return (DateTime)_lastUpdated;
        }
    }
}
