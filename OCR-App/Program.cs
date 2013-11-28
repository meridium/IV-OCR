using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using ImageVault.Client;
using ImageVault.Client.Query;
using ImageVault.Common.Data;

namespace OCR_App
{
    class Program
    {
        static void Main(string[] args) {

            // Todo: extract to config
            var sourceFile = @"D:\GitHub\IV-OCR\images\source.jpg";

            var resultString = "";
            
            var vaultId = 520;  // fedex vault on my local ImageVault instance

            // get images from vault and loop through
            var client = ClientFactory.GetSdkClient();

            var jpegWebFormat = new ImageFormat {MediaFormatOutputType = MediaFormatOutputTypes.Jpeg};

            var mediaList = client.Query<MediaItem>().Where(m => m.VaultId == vaultId).ToList();
            var ids = mediaList.Select(m => m.Id).ToList();

            var webMedias = client.Query<WebMedia>().Where(wm => ids.Contains(wm.Id)).UseFormat(jpegWebFormat).ToList();

            foreach (var webMedia in webMedias) {
                var webclient = new WebClient();

                webclient.DownloadFile(webMedia.Url, sourceFile);

                var text = GetText(sourceFile);

                Console.WriteLine("Text for image " + webMedia.Url + ": " + text);

                Console.ReadLine();

            }

        }

        private static string GetText(string source) {
            var result = "";

            const string targetFile = @"D:\GitHub\IV-OCR\images\temp_out.txt";
            const string teserrectPath = @"D:\Tesseract-OCR\tesseract.exe";

            for (int i = 0; i < 2; i++ )
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = teserrectPath,
                        Arguments = string.Format("\"{0}\" \"{1}\" -l eng -psm {2}", source, targetFile, 5 * i + 1),
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();
                process.WaitForExit();

                // read the result from file
                using (var f = File.OpenText(targetFile + ".txt"))
                {
                    result+=f.ReadToEnd();
                }
            }

            File.Delete(targetFile + ".txt");

            return result;
        }
    }
}
