using System.Collections;
using System.Runtime.InteropServices;

namespace PDF2SVG.PopplerCairo.Bindings;


public sealed class PdfPageEnumerable : IEnumerable<PdfPageData>, IDisposable
{
    private readonly bool _isForceToPng;
    private readonly int _dpi;

    private GCHandle? _handle;
    private IntPtr _docPtr;

    public bool IsDocumentOpened;

    public int PageCount { get; }

    public PdfPageEnumerable(byte[] pdfBytes, bool isForceToPng, int dpi = 300)
    {
        _isForceToPng = isForceToPng;
        _dpi = dpi;

        // open document once
        _handle = GCHandle.Alloc(pdfBytes, GCHandleType.Pinned);
        _docPtr = Pdf2SvgInterop.NativeMethods.pdf_open_doc(
            _handle.Value.AddrOfPinnedObject(),
            pdfBytes.Length,
            out var count
        );

        IsDocumentOpened = _docPtr != IntPtr.Zero;

        PageCount = count;
    }

    public IEnumerator<PdfPageData> GetEnumerator()
    {
        if (IsDocumentOpened == false || _docPtr == IntPtr.Zero)
            throw new PopplerCairoConvertationException("Failed to open PDF.");

        for (var i = 0; i < PageCount; i++)
        {
            var dataBuf = Pdf2SvgInterop.NativeMethods.pdf_get_page_data(_docPtr, i, _isForceToPng, _dpi,
                                                            out var dataLen, out var isSvg);
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
                Pdf2SvgInterop.NativeMethods.pdf_release_buffer(dataBuf);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (IsDocumentOpened || _docPtr == IntPtr.Zero)
        {
            Pdf2SvgInterop.NativeMethods.pdf_close_doc(_docPtr);
            _docPtr = IntPtr.Zero;
        }

        if (_handle.HasValue)
        {
            _handle.Value.Free();
            _handle = null;
        }
    }

    public static PdfPageEnumerable ConvertPdfPages(byte[] pdfBytes, bool isForceToPng, int dpi = 300)
    {
        return new PdfPageEnumerable(pdfBytes, isForceToPng, dpi);
    }
}