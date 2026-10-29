namespace Keeper.Api.Models;

public class VisitorActionRecord
{
    public int Id { get; set; }

    public int VisitorId { get; set; }

    public VisitorActionType Action { get; set; }

    public int TrustChange { get; set; }

    public int TrustLevelAfter { get; set; }

    public DateTime PerformedAt { get; set; }

    public Visitor? Visitor { get; set; }
}
