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
    private const uint TaiwanDataCenterId = 151;
    private const uint TaiwanFirstWorldId = 4028;
    private const uint TaiwanLastWorldId = 4035;

    internal static bool IsPublic(World world)
        => world.IsPublic
            || world.DataCenter.RowId == TaiwanDataCenterId
            && world.RowId is >= TaiwanFirstWorldId and <= TaiwanLastWorldId;

    internal static World[] Get(ExcelWorldHelper.Region? region = null)
        => [.. Svc.Data.GetExcelSheet<World>()
            .Where(world => IsPublic(world)
                && (region == null || world.GetRegion() == region.Value))];

    internal static World[] Get(uint dataCenter)
        => [.. Svc.Data.GetExcelSheet<World>()
            .Where(world => IsPublic(world) && world.DataCenter.RowId == dataCenter)];
}
