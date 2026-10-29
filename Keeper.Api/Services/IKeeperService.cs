using Keeper.Api.Models;

namespace Keeper.Api.Services;

public enum ActionOutcomeStatus
{
    Success,
    VisitorNotFound,
    AlreadyPerformed
}

public sealed record ActionOutcome(
    ActionOutcomeStatus Status,
    VisitorActionRecord? Record = null,
    Visitor? Visitor = null);

public interface IKeeperService
{
    Task<Visitor> RegisterVisitorAsync(string? name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Visitor>> GetVisitorsAsync(CancellationToken cancellationToken = default);

    Task<Visitor?> GetVisitorAsync(int visitorId, CancellationToken cancellationToken = default);

    Task<ActionOutcome> PerformActionAsync(int visitorId, VisitorActionType action, CancellationToken cancellationToken = default);

    Task<Visitor?> CheckAccessAsync(int visitorId, CancellationToken cancellationToken = default);
}
