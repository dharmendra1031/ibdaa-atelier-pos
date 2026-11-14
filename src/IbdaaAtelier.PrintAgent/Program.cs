using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

if (!OperatingSystem.IsWindows())
    throw new InvalidOperationException("The print agent runs on the cashier's Windows PC.");

var server = Environment.GetEnvironmentVariable("IBDAA_AGENT_SERVER")?.TrimEnd('/');
var token = Environment.GetEnvironmentVariable("IBDAA_AGENT_TOKEN");
var printer = Environment.GetEnvironmentVariable("IBDAA_AGENT_PRINTER");
var sumatra = Environment.GetEnvironmentVariable("IBDAA_AGENT_SUMATRA");
if (!int.TryParse(Environment.GetEnvironmentVariable("IBDAA_AGENT_TERMINAL"), out var terminalId) ||
    terminalId <= 0 || token?.Length != 64 || !token.All(Uri.IsHexDigit) ||
    !Uri.TryCreate(server, UriKind.Absolute, out var serverUri) ||
    (serverUri.Scheme != Uri.UriSchemeHttps &&
        !(serverUri.IsLoopback && serverUri.Scheme == Uri.UriSchemeHttp)) ||
    !File.Exists(sumatra))
    throw new InvalidOperationException(
        "Configure IBDAA_AGENT_SERVER, TERMINAL, TOKEN and SUMATRA before starting the agent.");

using var http = new HttpClient
{
    BaseAddress = new Uri(serverUri, "/"),
    Timeout = TimeSpan.FromSeconds(60)
};
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

Console.WriteLine($"IBDAA printer agent connected for terminal #{terminalId}.");
while (!stop.IsCancellationRequested)
{
    try
    {
        using var next = await SendAsync(HttpMethod.Post, "api/print-agent/next", null, stop.Token);
        if (next.StatusCode == HttpStatusCode.NoContent)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stop.Token);
            continue;
        }

        next.EnsureSuccessStatusCode();
        var job = await next.Content.ReadFromJsonAsync<PendingJob>(cancellationToken: stop.Token)
            ?? throw new InvalidOperationException("Print job response is empty.");

        string? error = null;
        try
        {
            await PrintAsync(job.PrintJobId, stop.Token);
            Console.WriteLine($"Printer accepted invoice #{job.OrderId} (job #{job.PrintJobId}).");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            error = exception.Message;
            Console.Error.WriteLine($"Print job #{job.PrintJobId} failed: {error}");
        }

        using var result = await SendAsync(HttpMethod.Post,
            $"api/print-agent/result/{job.PrintJobId}",
            JsonContent.Create(new { accepted = error is null, error }), stop.Token);
        result.EnsureSuccessStatusCode();
    }
    catch (OperationCanceledException) when (stop.IsCancellationRequested)
    {
        break;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Print agent connection error: {exception.Message}");
        await Task.Delay(TimeSpan.FromSeconds(5), stop.Token);
    }
}

async Task<HttpResponseMessage> SendAsync(
    HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
{
    using var request = new HttpRequestMessage(method, path);
    request.Headers.Add("X-Print-Terminal", terminalId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    request.Headers.Add("X-Print-Token", token);
    request.Content = content;
    return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
}

async Task PrintAsync(long jobId, CancellationToken cancellationToken)
{
    using var response = await SendAsync(HttpMethod.Get, $"api/print-agent/pdf/{jobId}", null, cancellationToken);
    response.EnsureSuccessStatusCode();

    var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pdf");
    try
    {
        await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 81920, useAsync: true))
            await response.Content.CopyToAsync(file, cancellationToken);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(sumatra!)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        if (string.IsNullOrWhiteSpace(printer))
            process.StartInfo.ArgumentList.Add("-print-to-default");
        else
        {
            process.StartInfo.ArgumentList.Add("-print-to");
            process.StartInfo.ArgumentList.Add(printer);
        }

        process.StartInfo.ArgumentList.Add("-silent");
        process.StartInfo.ArgumentList.Add(path);
        if (!process.Start())
            throw new InvalidOperationException("SumatraPDF did not start.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("Printer command timed out.");
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Printer command failed (exit code {process.ExitCode}).");
    }
    finally
    {
        try { File.Delete(path); }
        catch (IOException) { Console.Error.WriteLine("Could not delete temporary invoice PDF."); }
    }
}

internal sealed record PendingJob(long PrintJobId, long OrderId);
