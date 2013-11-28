using System;
using System.Diagnostics;
using System.IO;

namespace OCR_App
{
    class Program
    {
        static void Main(string[] args) {

            var sourceFile = @"D:\GitHub\IV-OCR\images\Golden_Globe_text_logo.png";
            var targetFile = @"D:\GitHub\IV-OCR\images\temp_out.txt";
            var resultString = "";

            var p = new ProcessStartInfo();

            p.CreateNoWindow = true;
            p.WindowStyle = ProcessWindowStyle.Hidden;

            p.FileName = @"D:\Tesseract-OCR\tesseract.exe";

            p.Arguments = string.Format("\"{0}\" \"{1}\" -l eng -psm 6", sourceFile, targetFile);

            using (var ps = Process.Start(p)) {
                // wait for command to finish
                var result = ps.ExitCode;
                if (result != 0) {
                    return;
                }
                ps.WaitForExit();
            }

            // read the result from file
            using (var f = File.OpenText(targetFile)) {
                resultString = f.ReadToEnd();
            }

            //File.Delete(sourceFile);
            File.Delete(targetFile);

            Console.WriteLine("Result: " + resultString);

            Console.ReadLine();
        }
    }
}
