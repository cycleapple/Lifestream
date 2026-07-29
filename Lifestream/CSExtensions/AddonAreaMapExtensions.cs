using FFXIVClientStructs.FFXIV.Client.UI;
namespace Lifestream.CSExtensions;

public static unsafe class AddonAreaMapExtensions
{
    public static Vector2 GetHoveredCoords(AddonAreaMap* addon)
    {
        var hoveredCoords = (short*)((nint)addon + 1968);
        var hoveredX      = hoveredCoords[0] + hoveredCoords[1] / 10f;
        var hoveredY      = hoveredCoords[2] + hoveredCoords[3] / 10f;
        return new(hoveredX, hoveredY);
    }
}
