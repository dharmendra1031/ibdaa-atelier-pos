using System.Data;
using System.Security.Cryptography;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Printing;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Services;

public sealed class PrintQueueService(AtelierDbContext db)
{
    public async Task<bool> AuthenticateAsync(int terminalId, string? token, CancellationToken ct)
    {
        if (terminalId <= 0 || token?.Length != 64 || !token.All(Uri.IsHexDigit))
            return false;

        var hash = await db.PrintTerminals.AsNoTracking()
            .Where(t => t.TerminalId == terminalId && t.IsActive)
            .Select(t => t.TokenHash).SingleOrDefaultAsync(ct);
        if (hash is null) return false;

        var actual = SHA256.HashData(Convert.FromHexString(token));
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(hash));
    }

    public async Task<PrintJob> QueueAsync(long orderId, int terminalId, CancellationToken ct)
    {
        if (orderId <= 0 || terminalId <= 0)
            throw new ArgumentException("Valid invoice and cashier printer are required.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.Orders.AnyAsync(o => o.OrderId == orderId, ct))
            throw new ArgumentException("Invoice not found.");
        if (!await db.PrintTerminals.AnyAsync(t => t.TerminalId == terminalId && t.IsActive, ct))
            throw new InvalidOperationException("This cashier printer is not configured or is disabled.");

        var job = await db.PrintJobs.FromSqlInterpolated(
            $"SELECT * FROM dbo.PrintJobs WITH (UPDLOCK, HOLDLOCK) WHERE OrderId = {orderId} AND TerminalId = {terminalId}")
            .SingleOrDefaultAsync(ct);

        if (job is null)
        {
            job = new PrintJob { OrderId = orderId, TerminalId = terminalId };
            db.PrintJobs.Add(job);
        }
        else if (job.Status == "Failed")
        {
            // Only an explicit cashier retry revives a failed job.
            job.Status = "Pending";
            job.ErrorMessage = null;
            job.StartedAtUtc = null;
            job.FinishedAtUtc = null;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return job;
    }

    public async Task<PrintJob?> GetStatusAsync(long jobId, CancellationToken ct) =>
        await db.PrintJobs.AsNoTracking().SingleOrDefaultAsync(j => j.PrintJobId == jobId, ct);

    public static string DisplayStatus(PrintJob job) =>
        job.Status == "Processing" && job.StartedAtUtc < DateTime.UtcNow.AddMinutes(-2)
            ? "NeedsReview" : job.Status;

    public async Task<PrintJob?> ClaimAsync(int terminalId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var job = await db.PrintJobs.FromSqlInterpolated(
            $"SELECT * FROM dbo.PrintJobs WITH (UPDLOCK, HOLDLOCK) WHERE TerminalId = {terminalId} AND Status = {"Pending"}")
            .OrderBy(j => j.PrintJobId).FirstOrDefaultAsync(ct);
        if (job is null)
        {
            await transaction.CommitAsync(ct);
            return null;
        }

        job.Status = "Processing";
        job.Attempts++;
        job.StartedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return job;
    }

    public async Task<PrintJob?> GetClaimedAsync(int terminalId, long jobId, CancellationToken ct) =>
        await db.PrintJobs.AsNoTracking().SingleOrDefaultAsync(
            j => j.TerminalId == terminalId && j.PrintJobId == jobId && j.Status == "Processing", ct);

    public async Task<bool> CompleteAsync(int terminalId, long jobId, bool accepted, string? error, CancellationToken ct)
    {
        var job = await db.PrintJobs.SingleOrDefaultAsync(
            j => j.TerminalId == terminalId && j.PrintJobId == jobId, ct);
        if (job is null) return false;
        if (job.Status is "Completed" or "Failed") return true;
        if (job.Status != "Processing") return false;

        job.Status = accepted ? "Completed" : "Failed";
        job.ErrorMessage = accepted ? null : (string.IsNullOrWhiteSpace(error) ? "Printer command failed." : error.Trim()[..Math.Min(error.Trim().Length, 300)]);
        job.FinishedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
