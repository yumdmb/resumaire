using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Resumaire.Api.Configuration;
using Resumaire.Api.Contracts;

namespace Resumaire.Api.Export;

public sealed class TypstPdfRenderer : ITypstPdfRenderer, IDisposable
{
    private const string TemplateResourceName = "Resumaire.Api.Export.Typst.resume.typ";

    private static readonly Lazy<string> Template = new(LoadTemplate);

    private readonly TypstOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TypstPdfRenderer> _logger;
    private readonly SemaphoreSlim _concurrency;

    public TypstPdfRenderer(
        IOptions<TypstOptions> options,
        IMemoryCache cache,
        ILogger<TypstPdfRenderer> logger)
    {
        _options = options.Value;
        _cache = cache;
        _logger = logger;
        _concurrency = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentRenders));
    }

    public async Task<byte[]> RenderAsync(ResumeContentDto content, CancellationToken cancellationToken = default)
    {
        var dataJson = TypstResumeDataMapper.ToDataJson(content);
        var cacheKey = "typst-pdf:" + Hash(Template.Value + "\n" + dataJson);

        if (_cache.TryGetValue(cacheKey, out byte[]? cached) && cached is not null)
        {
            return cached;
        }

        await _concurrency.WaitAsync(cancellationToken);
        try
        {
            var pdf = await CompileAsync(dataJson, cancellationToken);
            _cache.Set(cacheKey, pdf, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)),
                Size = pdf.Length
            });

            return pdf;
        }
        finally
        {
            _concurrency.Release();
        }
    }

    private async Task<byte[]> CompileAsync(string dataJson, CancellationToken cancellationToken)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "resumaire-typst-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(workDir, "resume.typ"), Template.Value, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(workDir, "data.json"), dataJson, cancellationToken);

            var startInfo = new ProcessStartInfo(_options.Executable)
            {
                WorkingDirectory = workDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in new[] { "compile", "--root", workDir, "resume.typ", "out.pdf" })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                _logger.LogError(ex, "Could not start the Typst executable '{Executable}'.", _options.Executable);
                throw new ResumeRenderException("PDF rendering is unavailable: the Typst executable could not be started.", ex);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

            var stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                cancellationToken.ThrowIfCancellationRequested();
                throw new ResumeRenderException("PDF rendering timed out.");
            }

            await Task.WhenAll(stdout, stderr);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Typst compile failed with exit code {ExitCode}: {Error}", process.ExitCode, stderr.Result);
                throw new ResumeRenderException("PDF rendering failed.");
            }

            return await File.ReadAllBytesAsync(Path.Combine(workDir, "out.pdf"), cancellationToken);
        }
        finally
        {
            try
            {
                Directory.Delete(workDir, recursive: true);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete temporary Typst directory {Directory}.", workDir);
            }
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }
    }

    private static string LoadTemplate()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{TemplateResourceName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public void Dispose() => _concurrency.Dispose();
}
