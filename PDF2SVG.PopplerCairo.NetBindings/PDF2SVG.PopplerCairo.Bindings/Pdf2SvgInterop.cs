namespace PDF2SVG.PopplerCairo.Bindings;

using System;
using System.IO;
using System.Runtime.InteropServices;

public static class Pdf2SvgInterop
{
    static class NativeMethods
    {
        [DllImport("pdf2svgwrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pdf_open_doc(
            IntPtr pdfData,
            int pdfLen,
            out int pageCount
        );

        [DllImport("pdf2svgwrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pdf_get_page_svg(
            IntPtr docHandle,
            int pageNum,
            out int svgLen
        );

        [DllImport("pdf2svgwrapper.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void pdf_close_doc(IntPtr docHandle);

        [DllImport("pdf2svgwrapper.dll", CallingConvention = CallingConvention.Cdecl)]
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

    public static IEnumerable<MemoryStream> ConvertPdfToSvgs(byte[] pdfBytes)
    {
        // pin the managed array
        var handle = GCHandle.Alloc(pdfBytes, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = NativeMethods.pdf_open_doc(
                handle.AddrOfPinnedObject(),
                pdfBytes.Length,
                out int pageCount
            );

            if (ptr == IntPtr.Zero)
                throw new PopplerCairoConvertationException("Failed to open PDF.");

            try
            {
                for (int i = 0; i < pageCount; i++)
                {
                    IntPtr svgBuf = NativeMethods.pdf_get_page_svg(ptr, i, out int svgLen);
                    if (svgBuf == IntPtr.Zero)
                        throw new PopplerCairoConvertationException($"Page {i} conversion failed.");

                    try
                    {
                        var svgBytes = new byte[svgLen];
                        Marshal.Copy(svgBuf, svgBytes, 0, svgLen);

                        yield return new MemoryStream(svgBytes, writable: false);
                    }
                    finally
                    {
                        NativeMethods.pdf_release_buffer(svgBuf);
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


    public static MemoryStream ConvertPdfPageToSvg(byte[] pdfBytes, int page)
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
                IntPtr svgBuf = NativeMethods.pdf_get_page_svg(ptr, page, out int svgLen);
                if (svgBuf == IntPtr.Zero)
                    throw new PopplerCairoConvertationException($"Page {page} conversion failed.");

                try
                {
                    var svgBytes = new byte[svgLen];
                    Marshal.Copy(svgBuf, svgBytes, 0, svgLen);

                    return new MemoryStream(svgBytes, writable: false);
                }
                finally
                {
                    NativeMethods.pdf_release_buffer(svgBuf);
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
