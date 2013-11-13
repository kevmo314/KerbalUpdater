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
                WWW request = WaitForLoad(new WWW(KerbalUpdater.Constants.DOWNLOAD_URL, postData));
                Debug.Log(request.text);
                return new Uri(request.text);
            }
        }
        private DateTime? LastUpdated = null;
        /// <summary>
        /// Create a SpacePort page reference from a SpacePort ID
        /// </summary>
        /// <param name="SpacePortID"></param>
        public SpacePortPage(int SpacePortID)
        {
            this.SpacePortID = SpacePortID;
            Page = new WWW(String.Format(KerbalUpdater.Constants.SPACEPORT_URL, SpacePortID));
        }
        /// <summary>
        /// Create a SpacePort page reference from a URL
        /// </summary>
        /// <param name="URL">The URL</param>
        public SpacePortPage(string URL)
        {
            Page = WaitForLoad(new WWW(URL));
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
            if (LastUpdated == null)
            {
                while (!Page.isDone)
                {
                    System.Threading.Thread.Sleep(250);
                }
                Match match = (new Regex("Created:(?<Date>.*?)</li>")).Match(Page.text);
                if (match.Success && match.Groups.Count > 0)
                {
                    LastUpdated = DateTime.Parse(match.Groups["Date"].Value.Trim());
                }
            }
            return (DateTime)LastUpdated;
        }
    }
}
