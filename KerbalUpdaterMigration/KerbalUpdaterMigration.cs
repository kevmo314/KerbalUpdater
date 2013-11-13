using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace KerbalUpdater
{
    class KerbalUpdaterMigration
    {
        static bool RestartKSP = false;
        static string STAGING_TARGET;
        static string PLUGIN_TARGET;
        static string RESTART_SIGNAL;
        static string REMOVE_QUEUE;
        static string KSP_EXE;
        static void Main(string[] args)
        {
            while (System.Diagnostics.Process.GetProcessesByName("KSP").Length > 0);
            LoadConstants();
            DetectRestartSignal();
            RemoveAbandonedDirectories();
            DirectoryInfo staging = new DirectoryInfo(STAGING_TARGET);
            CopyFilesRecursively(staging, new DirectoryInfo(PLUGIN_TARGET));
            try
            {
                staging.Delete(true);
                staging.Create(); // make an empty directory
            }
            catch (Exception ex)
            {
                // oh well?
            }
            if (RestartKSP)
            {
                System.Diagnostics.Process.Start(KSP_EXE);
            }
        }
        static void LoadConstants()
        {
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/PluginData/KerbalUpdater/Staging/CONSTANTS";
            using (StreamReader s = new StreamReader(dir))
            {
                STAGING_TARGET = s.ReadLine();
                PLUGIN_TARGET = s.ReadLine();
                RESTART_SIGNAL = s.ReadLine();
                REMOVE_QUEUE = s.ReadLine();
                KSP_EXE = s.ReadLine();
            }
            File.Delete(dir);
        }
        /// <summary>
        /// The plugin author no longer uses a directory, so delete it
        /// </summary>
        static void RemoveAbandonedDirectories()
        {
            if (File.Exists(REMOVE_QUEUE))
            {
                using (StreamReader s = new StreamReader(REMOVE_QUEUE))
                {
                    while (s.Peek() >= 0)
                    {
                        if (Directory.Exists(PLUGIN_TARGET + s.ReadLine()))
                        {
                            Directory.Delete(PLUGIN_TARGET + s.ReadLine(), true);
                        }
                    }
                }
                File.Delete(REMOVE_QUEUE);
            }
        }
        /// <summary>
        /// Detect if the user requested that we restart KSP
        /// </summary>
        static void DetectRestartSignal()
        {
            if (File.Exists(RESTART_SIGNAL))
            {
                RestartKSP = true;
                File.Delete(RESTART_SIGNAL);
            }
        }
        /// <summary>
        /// From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c-sharp
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }
            foreach (FileInfo file in source.GetFiles())
            {
                //this.Worker.ReportProgress(Done / Total);
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }
    }
}
