# PDF Reader – Resize & Crop (Windows 10)

Native **Windows 10** WPF application that opens PDFs and lets you **resize and crop** pages to **custom width and height in pixels**, then export as PNG.

## Features

- **Open PDF** – Load any PDF file.
- **Page navigation** – Previous/Next to move between pages.
- **Custom output size** – Set **width** and **height** in **pixels** (e.g. 800×600).
- **Optional crop** – Crop to a rectangle (X, Y, W, H) in pixels of the rendered page; result is scaled to your chosen width×height.
- **Export current page** – Save the current page as PNG at the specified dimensions (and optional crop).
- **Export all pages** – Save every page as PNG (`page_1.png`, `page_2.png`, …) at the same dimensions.

## Requirements

- **Windows 10** (or later)
- **.NET 9 SDK** (for building)
- Build and run on Windows (WPF is Windows-only)

## Build and run

From the solution directory (e.g. `pdfreader`):

```bash
cd PdfReader
dotnet restore
dotnet build
dotnet run
```

Or open `PdfReader.sln` in Visual Studio 2022 and press F5.

## Usage

1. **Open PDF** – Click “Open PDF” and select a file.
2. Set **Output width (px)** and **height (px)** to the desired size in pixels.
3. (Optional) Enable **“Crop to selection”** and set **X, Y, W, H** in pixels of the default-rendered page. Use **W=0, H=0** to use full page (no crop).
4. **Export current page** – Saves the current page as PNG at the given size.
5. **Export all pages** – Choose a save path (e.g. `C:\Export\page_1.png`); the app will write `page_1.png`, `page_2.png`, … in that folder.

## Project layout

- `PdfReader.sln` – Solution file  
- `PdfReader/` – WPF app  
  - `App.xaml`, `MainWindow.xaml` – UI  
  - `MainWindow.xaml.cs` – Open, navigate, export handlers  
  - `PdfRenderService.cs` – Load PDF (Melville.Pdf), render at custom size/crop (Melville.Pdf.SkiaSharp), export PNG  

## Dependencies

- **Melville.Pdf.SkiaSharp** – PDF rendering and rendering to images at custom pixel dimensions (MIT-style license).

If the Melville API differs in your package version (e.g. `PdfDocument.OpenAsync` vs `PdfReader.ReadFromFileAsync`), adjust the calls in `PdfRenderService.cs` to match the [Melville.PDF](https://github.com/DrJohnMelville/Pdf) documentation for your version.
