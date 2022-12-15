using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace BetterSanctum;

public class BetterSanctumSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    public ColorNode TextColor { get; set; } = new ColorNode(Color.White);
    public ColorNode GoodTextColor { get; set; } = new ColorNode(Color.Green);
    public ColorNode BackgroundColor { get; set; } = new ColorNode(Color.Black with { A = 128 });
    public ToggleNode ShowEffectId { get; set; } = new ToggleNode(false);
    public ToggleNode ShowEffectName { get; set; } = new ToggleNode(true);
    public ToggleNode ShowEffectDescription { get; set; } = new ToggleNode(true);
}