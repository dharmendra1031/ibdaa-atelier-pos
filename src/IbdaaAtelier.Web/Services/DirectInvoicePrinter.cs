using System.Diagnostics;

namespace IbdaaAtelier.Web.Services;

public sealed class DirectInvoicePrinter(IConfiguration configuration, ILogger<DirectInvoicePrinter> logger)
{
    public async Task PrintAsync(byte[] pdf, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
            throw new InvalidOperationException("Direct printing requires a Windows cashier host.");

        var executable = configuration["Printing:SumatraPdfPath"];
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            throw new InvalidOperationException("SumatraPDF direct-print tool is not configured on this cashier PC.");

        var file = Path.Combine(Path.GetTempPath(), "ibdaa-invoice-" + Guid.NewGuid().ToString("N") + ".pdf");
        await File.WriteAllBytesAsync(file, pdf, ct);
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            process.StartInfo.ArgumentList.Add("-print-to-default");
            process.StartInfo.ArgumentList.Add("-silent");
            process.StartInfo.ArgumentList.Add(file);
            if (!process.Start())
                throw new InvalidOperationException("Invoice printer process did not start.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                throw new InvalidOperationException("Printer did not accept the invoice within 30 seconds.");
            }
            if (process.ExitCode != 0)
            {
                logger.LogWarning("Direct invoice print failed with SumatraPDF exit code {ExitCode}", process.ExitCode);
                throw new InvalidOperationException($"Printer rejected the invoice (code {process.ExitCode}).");
            }
        }
        finally
        {
            try { File.Delete(file); }
            catch (IOException error) { logger.LogWarning(error, "Temporary invoice PDF could not be removed"); }
        }
    }
}
