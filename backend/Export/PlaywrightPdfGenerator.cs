using Microsoft.Playwright;

namespace Resumaire.Api.Export;

public sealed class PlaywrightPdfGenerator : IResumePdfGenerator, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed;

    public async Task<byte[]> GenerateAsync(string html, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var browser = await GetBrowserAsync();
        var page = await browser.NewPageAsync();

        try
        {
            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            var pdfBytes = await page.PdfAsync(new PagePdfOptions
            {
                Format = "Letter",
                PrintBackground = true,
                Margin = new Margin
                {
                    Top = "0.4in",
                    Bottom = "0.4in",
                    Left = "0.4in",
                    Right = "0.4in"
                }
            });

            return pdfBytes;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsConnected: true })
            return _browser;

        await _semaphore.WaitAsync();
        try
        {
            if (_browser is { IsConnected: true })
                return _browser;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            return _browser;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        _semaphore.Dispose();
    }
}
