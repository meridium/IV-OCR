using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using ImageVault.Client;
using ImageVault.Client.Query;
using ImageVault.Common.Data;
using ImageVault.Common.Data.Query;
using ImageVault.Common.Services;
using OCRAnalyzer;

namespace OCRConsole {
    internal class Program {
        private static void Main(string[] args) {

            var sourceFile = Path.Combine(Path.GetTempPath(), "source.jpg");
            var vaultId = Int32.Parse(ConfigurationManager.AppSettings["VaultId"]);
            var tesseractPath = ConfigurationManager.AppSettings["TesseractPath"];
            var ocrMetadataName = ConfigurationManager.AppSettings["OcrMetadataName"];
            var asciiMetadataName = ConfigurationManager.AppSettings["AsciiMetadataName"];

            if (string.IsNullOrEmpty(tesseractPath) || string.IsNullOrEmpty(ocrMetadataName) ||
                string.IsNullOrEmpty(asciiMetadataName))
            {
                throw new Exception("Configuration is missing/invalid");
            }
            var textFinder = new TesseractTextFinder(tesseractPath);


            var jpegWebFormat = new ImageFormat {MediaFormatOutputType = MediaFormatOutputTypes.Jpeg};
            var asciiFormat = new ImageFormat
            {
                Width = 300,
                KeepAspectRatio = true,
                MediaFormatOutputType = MediaFormatOutputTypes.Jpeg
            };

            // get images from vault
            var client = ClientFactory.GetSdkClient();

            var mediaList = client.Query<MediaItem>().Include(m => m.Metadata).Where(m => m.VaultId == vaultId).ToList();
            var ids = mediaList.Select(m => m.Id).ToList();

            var webMedias = client.Query<WebMedia>().Where(wm => ids.Contains(wm.Id)).UseFormat(jpegWebFormat).ToList();
            var asciiMedias = client.Query<WebMedia>().Where(wm => ids.Contains(wm.Id)).UseFormat(asciiFormat).ToList();

            // get metadatadefinition id for OCR metadata
            var metaChannel = client.CreateChannel<IMetadataDefinitionService>();

            var metadataDefinitionId = metaChannel.Find(new MetadataDefinitionQuery
            {
                Filter = new MetadataDefinitionFilter
                {
                    MetadataDefinitionType = MetadataDefinitionTypes.User

                }
            }).Where(x => x.Name == ocrMetadataName).Select(m => m.Id).SingleOrDefault();

            var asciiMetadataDefinitionId = metaChannel.Find(new MetadataDefinitionQuery
            {
                Filter = new MetadataDefinitionFilter
                {
                    MetadataDefinitionType = MetadataDefinitionTypes.User

                }
            }).Where(x => x.Name == asciiMetadataName).Select(m => m.Id).SingleOrDefault();


            foreach (var webMedia in webMedias) {
                Console.WriteLine("Finding text in image " + webMedia.Name + "...");
                var webclient = new WebClient();

                webclient.DownloadFile(webMedia.Url, sourceFile);

                var text = textFinder.GetText(sourceFile);

                var stream =
                    new MemoryStream(webclient.DownloadData(asciiMedias.SingleOrDefault(m => m.Id == webMedia.Id).Url));
                var ascii = AsciiArt.ConvertImage(stream, "2", null);

                AddMetadataToMedia(mediaList.SingleOrDefault(mi => mi.Id == webMedia.Id), text, ascii,
                    metadataDefinitionId, asciiMetadataDefinitionId);

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

        private static void AddMetadataToMedia(MediaItem media, string text, string ascii, int metadataDefinitionId,
            int asciiMetadataDefinitionId) {

            media.Metadata.Add(new MetadataLongString
            {
                LongStringValue = text,
                MetadataDefinitionId = metadataDefinitionId
            });
            media.Metadata.Add(new MetadataLongString
            {
                LongStringValue = ascii,
                MetadataDefinitionId = asciiMetadataDefinitionId
            });

        }
    }
}
