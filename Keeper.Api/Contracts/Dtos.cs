using System.ComponentModel.DataAnnotations;
using Keeper.Api.Domain;
using Keeper.Api.Models;

namespace Keeper.Api.Contracts;

public class CreateVisitorRequest
{
    [StringLength(100)]
    public string? Name { get; set; }
}

public class VisitorActionRequest
{
    public VisitorActionType? Action { get; set; }
}

public sealed record VisitorResponse(
    int Id,
    string Name,
    int TrustLevel,
    DateTime CreatedAt,
    IReadOnlyList<string> PerformedActions)
{
    public static VisitorResponse From(Visitor visitor) => new(
        visitor.Id,
        visitor.Name,
        visitor.TrustLevel,
        visitor.CreatedAt,
        visitor.Actions
            .OrderBy(a => a.PerformedAt)
            .Select(a => a.Action.ToString())
            .ToList());
}

public sealed record ActionResponse(
    int VisitorId,
    VisitorActionType Action,
    int TrustChange,
    int TrustLevel,
    DateTime PerformedAt,
    string Message);

public sealed record AccessResponse(
    int VisitorId,
    int TrustLevel,
    int RequiredTrustLevel,
    bool IsAllowed,
    string Message);

public sealed record ErrorResponse(string Message);
