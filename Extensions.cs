using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Elements.Sanctum;
using ExileCore.PoEMemory.FilesInMemory.Sanctum;

namespace BetterSanctum;

public static class Extensions
{
    public static List<(SanctumDeferredRewardCategory room, int order)> GetRoomsWithOrder(this SanctumRoomElement room)
    {
        return new[] { room.Data.Reward1, room.Data.Reward2, room.Data.Reward3 }.Select((x, i) => (room: x, i)).Where(x => x.room is { Address: not 0 }).ToList();
    }
}