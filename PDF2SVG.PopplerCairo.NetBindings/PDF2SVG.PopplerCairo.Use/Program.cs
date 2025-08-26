using PDF2SVG.PopplerCairo.Bindings;
using System;

namespace PDF2SVG.PopplerCairo.Use
{
    internal class Program
    {
        static void Main()
        {
            // Read PDF into managed byte[]
            byte[] pdfBytes = File.ReadAllBytes("./input.pdf");

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

                Console.WriteLine($"[No enumerable] Pdf page {index} processed");
                index++;
            }
            

            Console.WriteLine("Pdf processed without enumerable");



            using (var pages = PdfPageEnumerable.ConvertPdfPages(pdfBytes, true))
            {
                Console.WriteLine($"PageCount = {pages.PageCount}");

                foreach (var page in pages)
                {
                    if (page.IsSvg)
                        File.WriteAllBytes($"./output-enumerable-{index}.svg", page.Data.ToArray());
                    else
                    {
                        File.WriteAllBytes($"./output-enumerable-{index}.png", page.Data.ToArray());
                    }

                    Console.WriteLine($"[Enumerable] Pdf page {index} processed");
                }
            }

            Console.WriteLine("Pdf processed with enumerable");
        }
    }
}
