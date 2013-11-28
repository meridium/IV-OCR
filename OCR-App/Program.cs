using System;
using System.Drawing;
using tesseract;

namespace OCR_App
{
    class Program
    {
        static void Main(string[] args) {
            var processor = new TesseractProcessor();

            var succeed = processor.Init(@"D:\Tesseract-OCR\tessdata\", "eng", 3); // TesseractEngineMode: DEFAULT

            if (succeed) {

                processor.SetVariable("tessedit_pageseg_mode", "2"); // TesseractPageSegMode: PSM_SINGLE_LINE

                using (var image = Image.FromFile(@"D:\GitHub\IV-OCR\images\pdtext_03.gif"))
                {
                    processor.Clear();
                    processor.ClearAdaptiveClassifier();

                    var result = processor.Apply(image);

                    Console.WriteLine("Result: " + result);

                    Console.WriteLine("Exit...");

                }


            }

        }
    }
}
