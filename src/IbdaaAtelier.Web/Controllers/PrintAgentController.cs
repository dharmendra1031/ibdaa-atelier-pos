using IbdaaAtelier.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbdaaAtelier.Web.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/print-agent")]
public sealed class PrintAgentController(PrintQueueService jobs, InvoiceReportService invoices) : ControllerBase
{
    private async Task<int?> AuthorizedTerminalAsync(CancellationToken ct)
    {
        if (!int.TryParse(Request.Headers["X-Print-Terminal"], out var terminalId) ||
            !await jobs.AuthenticateAsync(terminalId, Request.Headers["X-Print-Token"].ToString(), ct))
            return null;

        return terminalId;
    }

    [HttpPost("next")]
    public async Task<IActionResult> Next(CancellationToken ct)
    {
        var terminalId = await AuthorizedTerminalAsync(ct);
        if (terminalId is null) return Unauthorized();

        var job = await jobs.ClaimAsync(terminalId.Value, ct);
        return job is null ? NoContent() : Ok(new { job.PrintJobId, job.OrderId });
    }

    [HttpGet("pdf/{jobId:long}")]
    public async Task<IActionResult> Pdf(long jobId, CancellationToken ct)
    {
        var terminalId = await AuthorizedTerminalAsync(ct);
        if (terminalId is null) return Unauthorized();

        var job = await jobs.GetClaimedAsync(terminalId.Value, jobId, ct);
        if (job is null) return NotFound();

        var invoice = await invoices.PrepareAsync(job.OrderId, ct);
        if (invoice is null) return NotFound();

        Response.Headers.CacheControl = "no-store";
        return File(invoices.RenderPdf(invoice, "80mm"), "application/pdf");
    }

    public sealed record PrintResult(bool Accepted, string? Error);

    [HttpPost("result/{jobId:long}")]
    public async Task<IActionResult> Result(long jobId, [FromBody] PrintResult? result, CancellationToken ct)
    {
        var terminalId = await AuthorizedTerminalAsync(ct);
        if (terminalId is null) return Unauthorized();
        if (result is null) return BadRequest();

        return await jobs.CompleteAsync(terminalId.Value, jobId, result.Accepted, result.Error, ct)
            ? Ok() : NotFound();
    }
}
