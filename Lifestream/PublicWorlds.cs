using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace Lifestream;

/// <summary>
/// Provides the live-world view used by Lifestream.
/// </summary>
internal static class PublicWorlds
{
    // Taiwan 7.3 ships its eight production worlds with World.IsPublic = false.
    // Keep the compatibility exception narrow so lobby and development worlds
    // outside the Taiwan production data center remain filtered out.
    internal const uint TaiwanDataCenterId = 151;
    internal const uint TaiwanFirstWorldId = 4028;
    internal const uint TaiwanLastWorldId = 4035;

    private static readonly Dictionary<string, uint> TaiwanWorldAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["巴哈姆特"] = 4028,
        ["Bahamut"] = 4028,
        ["伊弗利特"] = 4029,
        ["Ifrit"] = 4029,
        ["利維坦"] = 4030,
        ["Leviathan"] = 4030,
        ["拉姆"] = 4031,
        ["Ramuh"] = 4031,
        ["迦樓羅"] = 4032,
        ["Garuda"] = 4032,
        ["泰坦"] = 4033,
        ["Titan"] = 4033,
        ["奧汀"] = 4034,
        ["Odin"] = 4034,
        ["鳳凰"] = 4035,
        ["Phoenix"] = 4035,
    };

    internal static bool IsTaiwanWorld(uint worldId)
        => worldId is >= TaiwanFirstWorldId and <= TaiwanLastWorldId;

    internal static bool IsTaiwanWorld(World world)
        => IsTaiwanWorld(world.RowId);

    internal static bool IsPublic(World world)
        => world.IsPublic
            || IsTaiwanWorld(world);

    internal static World[] Get(ExcelWorldHelper.Region? region = null)
        => [.. Svc.Data.GetExcelSheet<World>()
            .Where(world => IsPublic(world)
                && (region == null || world.GetRegion() == region.Value))];

    internal static World[] Get(uint dataCenter)
        => dataCenter == TaiwanDataCenterId
            ? GetTaiwanWorlds()
            : [.. Svc.Data.GetExcelSheet<World>()
                .Where(world => IsPublic(world) && world.DataCenter.RowId == dataCenter)];

    internal static World[] GetTaiwanWorlds()
        => [.. Svc.Data.GetExcelSheet<World>()
            .Where(IsTaiwanWorld)
            .OrderBy(world => world.RowId)];

    internal static string NormalizeTaiwanWorldName(string input)
    {
        var trimmed = input.Trim();
        if(!TaiwanWorldAliases.TryGetValue(trimmed, out var worldId))
            return trimmed;

        var world = Svc.Data.GetExcelSheet<World>().GetRowOrDefault(worldId);
        return world?.Name.ToString() ?? trimmed;
    }
}
