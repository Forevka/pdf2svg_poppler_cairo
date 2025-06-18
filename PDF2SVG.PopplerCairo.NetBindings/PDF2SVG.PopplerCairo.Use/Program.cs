using PDF2SVG.PopplerCairo.Bindings;
using System;

namespace PDF2SVG.PopplerCairo.Use
{
    internal class Program
    {
        static void Main()
        {
            // Read PDF into managed byte[]
            byte[] pdfBytes = File.ReadAllBytes("./input-2.pdf");


            var pageData = Pdf2SvgInterop.ConvertPdfPages(pdfBytes, true);
            // Unpin the PDF buffer

            // Wrap in MemoryStream or write out
            var index = 0;
            foreach (var pdfPageData in pageData)
            {
                if (pdfPageData.IsSvg)
                    File.WriteAllBytes($"./output-{index}.svg", pdfPageData.Data.ToArray());
                else
                {
                    File.WriteAllBytes($"./output-{index}.png", pdfPageData.Data.ToArray());
                }

                Console.WriteLine($"Pdf page {index} processed");
                index++;
            }
            

            Console.WriteLine("Pdf processed");
        }
    }
}
