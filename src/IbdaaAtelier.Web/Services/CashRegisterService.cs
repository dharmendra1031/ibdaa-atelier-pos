using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Registers;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Services;

public sealed class CashRegisterService(AtelierDbContext db)
{
    private const decimal MaxAmount = 999999999999999.999m;

    public async Task<RegisterPage> GetPageAsync(CancellationToken ct)
    {
        var open = await db.CashRegisterSessions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.ClosedAtUtc == null, ct);
        var recent = await db.CashRegisterSessions.AsNoTracking()
            .Where(s => s.ClosedAtUtc != null)
            .OrderByDescending(s => s.SessionId).Take(10).ToListAsync(ct);

        var receivedCash = 0m;
        var refundedCash = 0m;
        var expenses = 0m;
        var receivedOther = 0m;
        var refundedOther = 0m;
        if (open is not null)
        {
            var receipts = await db.OrderPayments.AsNoTracking()
                .Where(p => p.RegisterSessionId == open.SessionId)
                .GroupBy(p => p.Method)
                .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount) })
                .ToListAsync(ct);
            var refunds = await db.OrderRefunds.AsNoTracking()
                .Where(r => r.RegisterSessionId == open.SessionId)
                .GroupBy(r => r.Method)
                .Select(g => new { Method = g.Key, Amount = g.Sum(r => r.Amount) })
                .ToListAsync(ct);

            receivedCash = receipts.Where(x => x.Method == "Cash").Sum(x => x.Amount);
            refundedCash = refunds.Where(x => x.Method == "Cash").Sum(x => x.Amount);
            receivedOther = receipts.Where(x => x.Method != "Cash").Sum(x => x.Amount);
            refundedOther = refunds.Where(x => x.Method != "Cash").Sum(x => x.Amount);
            expenses = await db.CashExpenses.AsNoTracking()
                .Where(x => x.SessionId == open.SessionId)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        }

        var openingRequired = await db.ShopSettings.AsNoTracking()
            .Select(s => (bool?)s.RequireRegisterOpen).SingleOrDefaultAsync(ct) ?? false;
        return new RegisterPage
        {
            OpenSession = open,
            RecentSessions = recent,
            Cash = new RegisterTotals(receivedCash, refundedCash, expenses),
            ReceivedOther = receivedOther,
            RefundedOther = refundedOther,
            OpeningRequired = openingRequired
        };
    }

    public async Task<long> OpenAsync(decimal openingCash, int userId, CancellationToken ct)
    {
        ValidateAmount(openingCash, allowZero: true);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var open = await db.CashRegisterSessions.FromSqlRaw(
            "SELECT * FROM dbo.CashRegisterSessions WITH (UPDLOCK, HOLDLOCK) WHERE ClosedAtUtc IS NULL")
            .SingleOrDefaultAsync(ct);
        if (open is not null)
        {
            await transaction.CommitAsync(ct);
            return open.SessionId;
        }

        var session = new CashRegisterSession
        {
            OpeningCash = openingCash,
            OpenedByUserId = userId
        };
        db.CashRegisterSessions.Add(session);
        await db.SaveChangesAsync(ct);
        AddAudit("CashRegister.Open", session.SessionId, userId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return session.SessionId;
    }

    public async Task<long> ReserveExpenseAsync(long sessionId, int userId, CancellationToken ct)
    {
        if (!await db.CashRegisterSessions.AnyAsync(s =>
            s.SessionId == sessionId && s.ClosedAtUtc == null, ct))
            throw new InvalidOperationException("Open a register before recording cash expenses.");

        var request = new CashExpenseRequest { SessionId = sessionId, UserId = userId };
        db.CashExpenseRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request.RequestId;
    }

    public async Task<bool> AddExpenseAsync(CashExpenseForm form, int userId, CancellationToken ct)
    {
        ValidateAmount(form.Amount, allowZero: false);
        form.Reason = form.Reason?.Trim() ?? "";
        if (form.SessionId <= 0 || form.RequestId <= 0 || form.Reason.Length is < 10 or > 500)
            throw new ArgumentException("Valid expense request and reason of 10–500 characters required.");

        var input = form.Amount.ToString("0.000", CultureInfo.InvariantCulture) + "|" + form.Reason;
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var request = await db.CashExpenseRequests.FromSqlInterpolated(
            $"SELECT * FROM dbo.CashExpenseRequests WITH (UPDLOCK, HOLDLOCK) WHERE RequestId = {form.RequestId}")
            .SingleOrDefaultAsync(ct);
        if (request is null || request.SessionId != form.SessionId || request.UserId != userId)
            throw new ArgumentException("Expense request belongs to another session or user.");

        var existing = await db.CashExpenses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RequestId == request.RequestId, ct);
        if (existing is not null)
        {
            if (request.Fingerprint != fingerprint)
                throw new InvalidOperationException("Expense request already saved with different values.");
            await transaction.CommitAsync(ct);
            return true;
        }

        var session = await db.CashRegisterSessions.FromSqlInterpolated(
            $"SELECT * FROM dbo.CashRegisterSessions WITH (UPDLOCK, HOLDLOCK) WHERE SessionId = {form.SessionId}")
            .SingleOrDefaultAsync(ct);
        if (session is null || session.ClosedAtUtc is not null)
            throw new InvalidOperationException("Cannot record an expense after closing the register.");

        request.Fingerprint = fingerprint;
        db.CashExpenses.Add(new CashExpense
        {
            RequestId = request.RequestId,
            SessionId = session.SessionId,
            RecordedByUserId = userId,
            Amount = form.Amount,
            Reason = form.Reason
        });
        AddAudit("CashExpense.Record", session.SessionId, userId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return false;
    }

    public async Task<RegisterCloseResult> CloseAsync(
        long sessionId, decimal countedCash, int userId, CancellationToken ct)
    {
        ValidateAmount(countedCash, allowZero: true);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var session = await db.CashRegisterSessions.FromSqlInterpolated(
            $"SELECT * FROM dbo.CashRegisterSessions WITH (UPDLOCK, HOLDLOCK) WHERE SessionId = {sessionId}")
            .SingleOrDefaultAsync(ct);
        if (session is null)
            throw new ArgumentException("Register session does not exist.");

        if (session.ClosedAtUtc.HasValue)
        {
            if (session.CountedCash != countedCash)
                throw new InvalidOperationException("Register already closed with a different cash count.");

            await transaction.CommitAsync(ct);
            return new RegisterCloseResult(sessionId, session.ExpectedCash!.Value,
                session.Difference!.Value, true);
        }

        var cashPayments = await db.OrderPayments.AsNoTracking()
            .Where(p => p.RegisterSessionId == sessionId && p.Method == "Cash")
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var cashRefunds = await db.OrderRefunds.AsNoTracking()
            .Where(r => r.RegisterSessionId == sessionId && r.Method == "Cash")
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
        var expenses = await db.CashExpenses.AsNoTracking()
            .Where(e => e.SessionId == sessionId)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var expected = checked(session.OpeningCash + cashPayments - cashRefunds - expenses);
        var difference = checked(countedCash - expected);
        session.ExpectedCash = expected;
        session.CountedCash = countedCash;
        session.Difference = difference;
        session.ClosedAtUtc = DateTime.UtcNow;
        session.ClosedByUserId = userId;
        AddAudit("CashRegister.Close", sessionId, userId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new RegisterCloseResult(sessionId, expected, difference, false);
    }

    public async Task ApproveAsync(long sessionId, int userId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var session = await db.CashRegisterSessions.FromSqlInterpolated(
            $"SELECT * FROM dbo.CashRegisterSessions WITH (UPDLOCK, HOLDLOCK) WHERE SessionId = {sessionId}")
            .SingleOrDefaultAsync(ct);

        if (session is null || !session.ClosedAtUtc.HasValue)
            throw new InvalidOperationException("Only a closed register can be approved.");

        if (!session.ApprovedAtUtc.HasValue)
        {
            session.ApprovedAtUtc = DateTime.UtcNow;
            session.ApprovedByUserId = userId;
            AddAudit("CashRegister.Approve", sessionId, userId);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    private static void ValidateAmount(decimal amount, bool allowZero)
    {
        if (amount < (allowZero ? 0m : 0.001m) ||
            amount > MaxAmount || amount != decimal.Round(amount, 3))
            throw new ArgumentException("Enter a valid cash amount with up to three decimals.");
    }

    private void AddAudit(string action, long sessionId, int userId) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = "CashRegisterSession",
            EntityId = sessionId
        });
}
