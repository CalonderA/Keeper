namespace Keeper.Api.Models;

public class Visitor
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int TrustLevel { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<VisitorActionRecord> Actions { get; set; } = new List<VisitorActionRecord>();
}
