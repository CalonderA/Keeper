using Keeper.Api.Models;

namespace Keeper.Api.Domain;

public static class TrustRules
{
    public const int MinTrustLevel = 0;
    public const int MaxTrustLevel = 100;
    public const int AccessThreshold = 50;

    public static int GetChange(VisitorActionType action) => action switch
    {
        VisitorActionType.Introduce => 20,
        VisitorActionType.OfferHelp => 30,
        VisitorActionType.Threaten => -20,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Неизвестное действие.")
    };

    public static int Apply(int currentTrustLevel, VisitorActionType action)
        => Math.Clamp(currentTrustLevel + GetChange(action), MinTrustLevel, MaxTrustLevel);
}
