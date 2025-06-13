namespace PDF2SVG.PopplerCairo.Bindings;

using System;
using System.IO;
using System.Runtime.InteropServices;

public static class Pdf2SvgInterop
{
    // Native conversion function
    [DllImport("PDF2SVG.PopplerCairo", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pdf_page_to_svg_mem(
        IntPtr pdfData,        // pointer to PDF bytes
        int pdfLen,          // length of PDF in bytes
        int pageNum,         // zero-based page index
        out int svgLen        // output length of SVG buffer
    );                                         

    // CRT free() to release malloc'd buffer
    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void free(IntPtr ptr);

    /// <summary>
    /// Converts the specified page of a PDF (provided as a MemoryStream) to an SVG MemoryStream.
    /// </summary>
    public static MemoryStream ConvertPdfPageToSvg(byte[] pdfBytes, int pageNum)
    {
        // Pin the array so the GC won't move it
        GCHandle handle = GCHandle.Alloc(pdfBytes, GCHandleType.Pinned);
        
        try
        {
            // Call the native function
            IntPtr nativePtr = pdf_page_to_svg_mem(
                handle.AddrOfPinnedObject(),
                pdfBytes.Length,
                pageNum,
                out int svgLen);

            if (nativePtr == IntPtr.Zero)
                throw new InvalidOperationException("PDF-to-SVG conversion failed.");

            // Copy native buffer into managed byte[]
            byte[] svgBytes = new byte[svgLen];
            Marshal.Copy(nativePtr, svgBytes, 0, svgLen);

            // free the native buffer
            free(nativePtr);

            return new MemoryStream(svgBytes);
        }
        finally
        {
            handle.Free();
        }
    }
}
