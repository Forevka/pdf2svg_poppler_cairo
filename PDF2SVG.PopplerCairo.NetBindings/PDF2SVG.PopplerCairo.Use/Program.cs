using PDF2SVG.PopplerCairo.Bindings;

namespace PDF2SVG.PopplerCairo.Use
{
    internal class Program
    {
        static void Main()
        {
            // Read PDF into managed byte[]
            byte[] pdfBytes = File.ReadAllBytes("./input.pdf");


            var svg = Pdf2SvgInterop.ConvertPdfPageToSvg(pdfBytes, 0);
            // Unpin the PDF buffer

            // Wrap in MemoryStream or write out
            File.WriteAllBytes("./output.svg", svg.ToArray());

            Console.WriteLine("SVG written to output.svg");
        }
    }
}
