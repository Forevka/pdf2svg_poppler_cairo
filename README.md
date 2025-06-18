# PDF2SVG Poppler/Cairo .NET Bindings

This repository contains a minimal set of .NET bindings that expose a native `pdf2svgwrapper` library built with [Poppler](https://poppler.freedesktop.org/) and [Cairo](https://www.cairographics.org/). The wrapper allows converting pages of a PDF document into SVG data entirely in memory.

The native libraries for both **Windows x64** and **Linux x64** are included under `PDF2SVG.PopplerCairo.Bindings`. They are packaged as part of the `PDF2SVG.PopplerCairo.Bindings` project so that the native code is copied to the output directory when building.

A small console project (`PDF2SVG.PopplerCairo.Use`) demonstrates how to invoke the binding. It reads `input.pdf` and writes the first page to `output.svg`.

## Building

This solution targets **.NET 8.0**. After installing the .NET 8 SDK you can build everything by running:

```bash
dotnet build PDF2SVG.PopplerCairo.NetBindings/PDF2SVG.PopplerCairo.NetBindings.sln
```

The build will produce the binding DLL along with the native libraries inside `bin/Debug/net8.0/` for each project.

## Example usage

The sample console program shows basic usage:

```csharp
byte[] pdfBytes = File.ReadAllBytes("./input-2.pdf");

var pageData = Pdf2SvgInterop.ConvertPdfPages(pdfBytes);

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
```

Run it with:

```bash
dotnet run --project PDF2SVG.PopplerCairo.NetBindings/PDF2SVG.PopplerCairo.Use
```

This will read `input.pdf` from the project directory and generate `output.svg`.

## Notes

Only x64 builds of the native wrapper are included. Building for other platforms requires compiling `pdf2svgwrapper` with Poppler and Cairo for the desired target.

