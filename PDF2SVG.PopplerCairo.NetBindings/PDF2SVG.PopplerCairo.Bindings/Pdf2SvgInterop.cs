namespace PDF2SVG.PopplerCairo.Bindings;

using System;
using System.IO;
using System.Runtime.InteropServices;

public class PdfPageData
{
    public required bool IsSvg { get; set; }
    public required MemoryStream Data { get; set; }
}

public static class Pdf2SvgInterop
{
    internal static class NativeMethods
    {
        [DllImport("native-svg2pdf/pdf2svgwrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pdf_open_doc(
            IntPtr pdfData,
            int pdfLen,
            out int pageCount
        );

        [DllImport("native-svg2pdf/pdf2svgwrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pdf_get_page_data(
            IntPtr docHandle,
            int pageNum,
            bool isForcePng,
            int dpi,
            out int dataLen,
            out bool isSvg
        );

        [DllImport("native-svg2pdf/pdf2svgwrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern void pdf_close_doc(IntPtr docHandle);

        [DllImport("native-svg2pdf/pdf2svgwrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern void pdf_release_buffer(IntPtr ptr);
    }

    //// Native conversion function
    //[DllImport("PDF2SVG.PopplerCairo", CallingConvention = CallingConvention.Cdecl)]
    //public static extern IntPtr pdf_page_to_svg_mem(
    //    IntPtr pdfData,        // pointer to PDF bytes
    //    int pdfLen,          // length of PDF in bytes
    //    int pageNum,         // zero-based page index
    //    out int svgLen        // output length of SVG buffer
    //);                                         

    //// CRT free() to release malloc'd buffer
    //[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    //public static extern void free(IntPtr ptr);

    public static IEnumerable<PdfPageData> ConvertPdfPages(byte[] pdfBytes, bool isForceToPng, int dpi = 300)
    {
        // pin the managed array
        var handle = GCHandle.Alloc(pdfBytes, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = NativeMethods.pdf_open_doc(
                handle.AddrOfPinnedObject(),
                pdfBytes.Length,
                out var pageCount
            );

            if (ptr == IntPtr.Zero)
                throw new PopplerCairoConvertationException("Failed to open PDF.");

            try
            {
                for (int i = 0; i < pageCount; i++)
                {
                    IntPtr dataBuf = NativeMethods.pdf_get_page_data(ptr, i, isForceToPng, dpi, out int dataLen, out var isSvg);
                    if (dataBuf == IntPtr.Zero)
                        throw new PopplerCairoConvertationException($"Page {i} conversion failed.");

                    try
                    {
                        var dataBytes = new byte[dataLen];
                        Marshal.Copy(dataBuf, dataBytes, 0, dataLen);

                        yield return new PdfPageData
                        {
                            Data = new MemoryStream(dataBytes, writable: false),
                            IsSvg = isSvg,
                        };
                    }
                    finally
                    {
                        NativeMethods.pdf_release_buffer(dataBuf);
                    }
                }
            }
            finally
            {
                NativeMethods.pdf_close_doc(ptr);
            }
        }
        finally
        {
            handle.Free();
        }
    }


    public static PdfPageData ConvertPdfPage(byte[] pdfBytes, int page, bool isForceToPng, int dpi = 300)
    {
        // pin the managed array
        var handle = GCHandle.Alloc(pdfBytes, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = NativeMethods.pdf_open_doc(
                handle.AddrOfPinnedObject(),
                pdfBytes.Length,
                out _
            );

            if (ptr == IntPtr.Zero)
                throw new PopplerCairoConvertationException("Failed to open PDF.");

            try
            {
                IntPtr dataBuf = NativeMethods.pdf_get_page_data(ptr, page, isForceToPng, dpi, out var dataLen, out var isSvg);
                if (dataBuf == IntPtr.Zero)
                    throw new PopplerCairoConvertationException($"Page {page} conversion failed.");

                try
                {
                    var svgBytes = new byte[dataLen];
                    Marshal.Copy(dataBuf, svgBytes, 0, dataLen);

                    return new PdfPageData
                    {
                        Data = new MemoryStream(svgBytes, writable: false),
                        IsSvg = isSvg,
                    };
                }
                finally
                {
                    NativeMethods.pdf_release_buffer(dataBuf);
                }
            }
            finally
            {
                NativeMethods.pdf_close_doc(ptr);
            }
        }
        catch (Exception e)
        {
            throw new PopplerCairoConvertationException("Failed to convert PDF to SVG", e);
        }
        finally
        {
            handle.Free();
        }
    }
}
