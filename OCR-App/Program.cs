﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ImageVault.Client;
using ImageVault.Client.Query;
using ImageVault.Common.Data;
using ImageVault.Common.Data.Query;
using ImageVault.Common.Services;

namespace OCR_App
{
    class Program {
        private static string _tesseractPath;
        private static int _vaultId;
        private static string _ocrMetadataName;
        private static string _asciiMetadataName;

        static void Main(string[] args) {

            var sourceFile = "";
            try {
                sourceFile = Path.Combine(Path.GetTempPath(), "source.jpg");
                _vaultId = Int32.Parse(ConfigurationManager.AppSettings["VaultId"]);
                _tesseractPath = ConfigurationManager.AppSettings["TesseractPath"];
                _ocrMetadataName = ConfigurationManager.AppSettings["OcrMetadataName"];
                _asciiMetadataName = ConfigurationManager.AppSettings["AsciiMetadataName"];

                if (string.IsNullOrEmpty(_tesseractPath) || string.IsNullOrEmpty(_ocrMetadataName) || string.IsNullOrEmpty(_asciiMetadataName)) {
                    throw new Exception("Configuration is missing/invalid");
                }

            } catch (Exception e) {
                Console.WriteLine("Error reading configuration: " + e.Message);
                return;
            }

            // get images from vault and loop through
            var client = ClientFactory.GetSdkClient();

            var jpegWebFormat = new ImageFormat {MediaFormatOutputType = MediaFormatOutputTypes.Jpeg};
            var asciiFormat = new ImageFormat {Width = 300, KeepAspectRatio = true, MediaFormatOutputType = MediaFormatOutputTypes.Jpeg};

            var mediaList = client.Query<MediaItem>().Include(m => m.Metadata).Where(m => m.VaultId == _vaultId).ToList();
            var ids = mediaList.Select(m => m.Id).ToList();

            var webMedias = client.Query<WebMedia>().Where(wm => ids.Contains(wm.Id)).UseFormat(jpegWebFormat).ToList();
            var asciiMedias = client.Query<WebMedia>().Where(wm => ids.Contains(wm.Id)).UseFormat(asciiFormat).ToList();

            // get metadatadefinition id for OCR metadata
            var metaChannel = client.CreateChannel<IMetadataDefinitionService>();

            var metadataDefinitionId = metaChannel.Find(new MetadataDefinitionQuery {
                Filter = new MetadataDefinitionFilter {
                    MetadataDefinitionType = MetadataDefinitionTypes.User

                }
            }).Where(x => x.Name == _ocrMetadataName).Select(m => m.Id).SingleOrDefault();

            var asciiMetadataDefinitionId = metaChannel.Find(new MetadataDefinitionQuery
            {
                Filter = new MetadataDefinitionFilter
                {
                    MetadataDefinitionType = MetadataDefinitionTypes.User

                }
            }).Where(x => x.Name == _asciiMetadataName).Select(m => m.Id).SingleOrDefault();


            foreach (var webMedia in webMedias) {
                Console.WriteLine("Finding text in image " + webMedia.Name + "...");
                var webclient = new WebClient();

                webclient.DownloadFile(webMedia.Url, sourceFile);

                var text = GetText(sourceFile);

                var stream = new MemoryStream(webclient.DownloadData(asciiMedias.SingleOrDefault(m => m.Id == webMedia.Id).Url));
                var ascii = AsciiArt.ConvertImage(stream, "2", null);

                AddMetadataToMedia(mediaList.SingleOrDefault(mi => mi.Id == webMedia.Id), text, ascii, metadataDefinitionId, asciiMetadataDefinitionId);

                Console.WriteLine(ascii);

                Console.WriteLine("Done.");

            }

            if (mediaList.Count > 0) {
                SaveMetadata(mediaList);
            }

        }
        private static void SaveMetadata(List<MediaItem> mediaList) {
            var client = ClientFactory.GetSdkClient();
            var channel = client.CreateChannel<IMediaService>();

            channel.Save(mediaList, MediaServiceSaveOptions.Metadata);
        }

        private static void AddMetadataToMedia(MediaItem media, string text, string ascii, int metadataDefinitionId, int asciiMetadataDefinitionId) {

            media.Metadata.Add(new MetadataLongString { LongStringValue = text, MetadataDefinitionId = metadataDefinitionId });
            media.Metadata.Add(new MetadataLongString { LongStringValue = ascii, MetadataDefinitionId = asciiMetadataDefinitionId });
           
        }
        private static string GetText(string source) {
            var result = "";

            var targetFile = Path.Combine(Path.GetTempPath(), "temp_out.txt");

            for (var i = 1; i >= 0; i-- )
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _tesseractPath,
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
                    var text = f.ReadToEnd();
                    text = new Regex("[^a-z0-9 - åäö]", RegexOptions.IgnoreCase).Replace(text, " ");
                    text = new Regex(@"\b\w{1,2}\b", RegexOptions.IgnoreCase).Replace(text, " ");
                    text = new Regex(@"\s+").Replace(text, " ");
                    result += text;
                }
            }

            File.Delete(targetFile + ".txt");

            return result;
        }
    }
}
