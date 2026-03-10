using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace PdfReader;

public partial class MainWindow
{
    private readonly PdfRenderService _renderService = new();
    private int _currentPage = 1;

    public MainWindow()
    {
        InitializeComponent();
        UpdatePageButtons();
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            Title = "Open PDF"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            OpenButton.IsEnabled = false;
            await _renderService.LoadAsync(dlg.FileName);
            Title = $"PDF Reader - {System.IO.Path.GetFileName(dlg.FileName)}";
            _currentPage = 1;
            UpdatePageButtons();
            await RefreshPageImageAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            OpenButton.IsEnabled = true;
        }
    }

    private async void PrevPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage <= 1) return;
        _currentPage--;
        UpdatePageButtons();
        await RefreshPageImageAsync();
    }

    private async void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage >= _renderService.PageCount) return;
        _currentPage++;
        UpdatePageButtons();
        await RefreshPageImageAsync();
    }

    private void UpdatePageButtons()
    {
        var hasDoc = _renderService.HasDocument;
        PrevPageButton.IsEnabled = hasDoc && _currentPage > 1;
        NextPageButton.IsEnabled = hasDoc && _currentPage < _renderService.PageCount;
        PageLabel.Text = hasDoc ? $"Page {_currentPage} of {_renderService.PageCount}" : "Page 0 of 0";
        ExportPageButton.IsEnabled = hasDoc;
        ExportAllButton.IsEnabled = hasDoc;
    }

    private async Task RefreshPageImageAsync()
    {
        if (!_renderService.HasDocument) return;
        var source = await _renderService.RenderPageForDisplayAsync(_currentPage);
        PageImage.Source = source;
    }

    private (int width, int height, double cropX, double cropY, double cropW, double cropH) GetExportParameters()
    {
        int w = 800, h = 600;
        int.TryParse(WidthBox.Text, out w);
        int.TryParse(HeightBox.Text, out h);
        w = Math.Max(1, Math.Min(w, 16384));
        h = Math.Max(1, Math.Min(h, 16384));

        double cropX = 0, cropY = 0, cropW = 0, cropH = 0;
        if (CropCheckBox.IsChecked == true)
        {
            double.TryParse(CropXBox.Text, out cropX);
            double.TryParse(CropYBox.Text, out cropY);
            double.TryParse(CropWBox.Text, out cropW);
            double.TryParse(CropHBox.Text, out cropH);
        }
        return (w, h, cropX, cropY, cropW, cropH);
    }

    private async void ExportPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_renderService.HasDocument) return;
        var (width, height, cropX, cropY, cropW, cropH) = GetExportParameters();

        var dlg = new SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
            FileName = $"page_{_currentPage}.png",
            Title = "Export current page"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            ExportPageButton.IsEnabled = false;
            await _renderService.ExportPageAsync(_currentPage, dlg.FileName, width, height, cropX, cropY, cropW, cropH);
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ExportPageButton.IsEnabled = true;
        }
    }

    private async void ExportAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_renderService.HasDocument) return;
        var (width, height, cropX, cropY, cropW, cropH) = GetExportParameters();

        var dlg = new SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
            FileName = "page_1.png",
            Title = "Choose folder and base name (files: page_1.png, page_2.png, ...)"
        };
        if (dlg.ShowDialog() != true) return;

        var folder = System.IO.Path.GetDirectoryName(dlg.FileName) ?? "";
        try
        {
            ExportAllButton.IsEnabled = false;
            for (int p = 1; p <= _renderService.PageCount; p++)
            {
                var path = System.IO.Path.Combine(folder, $"page_{p}.png");
                await _renderService.ExportPageAsync(p, path, width, height, cropX, cropY, cropW, cropH);
            }
            MessageBox.Show($"Exported {_renderService.PageCount} page(s) to {folder}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ExportAllButton.IsEnabled = true;
        }
    }
}
