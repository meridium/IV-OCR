using System;
using System.Collections.Generic;
using System.IO;
using ImageVault.Common.Data;
using ImageVault.Core.Common.Analysers;
using ImageVault.Core.Common.Analysers.Data;
using ImageVault.Core.Common.Data;
using ImageVault.Core.Configuration;
using log4net;

namespace OCRAnalyzer
{
    public class OcrAnalyzer: IMediaAnalyser
    {
        private ITextFinder _textFinder;
        private string _tesseractPath;
        private string _asciiMetadataName;
        private string _ocrMetadataName;
        private static readonly ILog Log = LogManager.GetLogger(typeof(OcrAnalyzer));

        public OcrAnalyzer() {
            InitConfig();
            _textFinder = new TesseractTextFinder(_tesseractPath);
        }

        private void InitConfig() {
            _tesseractPath = CoreConfigurationSection.Instance.AppSettings["TesseractPath"];
            _asciiMetadataName = CoreConfigurationSection.Instance.AppSettings["AsciiMetadataName"];
            _ocrMetadataName = CoreConfigurationSection.Instance.AppSettings["OCRMetadataName"];

            if (string.IsNullOrEmpty(_tesseractPath) || string.IsNullOrEmpty(_ocrMetadataName) || string.IsNullOrEmpty(_asciiMetadataName))
            {
                throw new Exception("Configuration is missing/invalid");
            }
        }

        public IList<RawMetadata> ReadMetadata(MediaContent content) {
            _textFinder = new TesseractTextFinder(_tesseractPath);

            var sourceFile = Path.Combine(Path.GetTempPath(), content.Name);
            var bc = content as BinaryMediaContentBase;
            using (var f = new FileStream(sourceFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                bc.WriteToStream(f);
            }
            Log.Debug("Searching for text");
            var text = _textFinder.GetText(sourceFile);
            Log.Debug("Found text " + text);
            var metaData = new RawMetadata
            {
                DefinitionType = MetadataDefinitionTypes.User,
                Type = MetadataTypes.LongString,
                Name = _ocrMetadataName,
                Value = text
            };
            File.Delete(sourceFile);
            Log.Debug("Returning metadata");
            return new List<RawMetadata> {metaData};
        }
     
    }
}
