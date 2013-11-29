using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace OCRAnalyzer
{
    public interface ITextFinder {
        string GetText(string sourcePath);
    }

    public class TesseractTextFinder : ITextFinder {
        private readonly string _tesseractPath;

        public TesseractTextFinder(string tesseractPath) {
            _tesseractPath = tesseractPath;
        }

        public string GetText(string sourcePath) {
            var result = "";

            var targetFile = Path.Combine(Path.GetTempPath(), "temp_out.txt");

            for (var i = 1; i >= 0; i--)
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _tesseractPath,
                        Arguments = string.Format("\"{0}\" \"{1}\" -l eng -psm {2}", sourcePath, targetFile, 5 * i + 1),
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
