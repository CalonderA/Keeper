using Keeper.Api.Data;
using Keeper.Api.Domain;
using Keeper.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Keeper.Api.Services;

public class KeeperService : IKeeperService
{
    private const string DefaultVisitorName = "Гость";

    private readonly KeeperDbContext _db;

    public KeeperService(KeeperDbContext db)
    {
        _db = db;
    }

    public async Task<Visitor> RegisterVisitorAsync(string? name, CancellationToken cancellationToken = default)
    {
        var visitor = new Visitor
        {
            Name = string.IsNullOrWhiteSpace(name) ? DefaultVisitorName : name.Trim(),
            TrustLevel = TrustRules.MinTrustLevel,
            CreatedAt = DateTime.UtcNow
        };

        _db.Visitors.Add(visitor);
        await _db.SaveChangesAsync(cancellationToken);
        return visitor;
    }

    public async Task<IReadOnlyList<Visitor>> GetVisitorsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Visitors
            .AsNoTracking()
            .Include(v => v.Actions)
            .OrderBy(v => v.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Visitor?> GetVisitorAsync(int visitorId, CancellationToken cancellationToken = default)
    {
        return _db.Visitors
            .AsNoTracking()
            .Include(v => v.Actions)
            .FirstOrDefaultAsync(v => v.Id == visitorId, cancellationToken);
    }

    public async Task<ActionOutcome> PerformActionAsync(int visitorId, VisitorActionType action, CancellationToken cancellationToken = default)
    {
        var visitor = await _db.Visitors.FirstOrDefaultAsync(v => v.Id == visitorId, cancellationToken);
        if (visitor is null)
        {
            return new ActionOutcome(ActionOutcomeStatus.VisitorNotFound);
        }

        var alreadyPerformed = await _db.VisitorActions
            .AnyAsync(a => a.VisitorId == visitorId && a.Action == action, cancellationToken);

        if (alreadyPerformed)
        {
            return new ActionOutcome(ActionOutcomeStatus.AlreadyPerformed, Visitor: visitor);
        }

        var change = TrustRules.GetChange(action);
        visitor.TrustLevel = TrustRules.Apply(visitor.TrustLevel, action);

        var record = new VisitorActionRecord
        {
            VisitorId = visitor.Id,
            Action = action,
            TrustChange = change,
            TrustLevelAfter = visitor.TrustLevel,
            PerformedAt = DateTime.UtcNow
        };

        _db.VisitorActions.Add(record);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await _db.Entry(visitor).ReloadAsync(cancellationToken);
            return new ActionOutcome(ActionOutcomeStatus.AlreadyPerformed, Visitor: visitor);
        }

        return new ActionOutcome(ActionOutcomeStatus.Success, record, visitor);
    }

    public Task<Visitor?> CheckAccessAsync(int visitorId, CancellationToken cancellationToken = default)
    {
        return _db.Visitors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == visitorId, cancellationToken);
    }
}
