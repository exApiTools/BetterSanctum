using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExileCore;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace BetterSanctum;

public class BetterSanctumPlugin : BaseSettingsPlugin<BetterSanctumSettings>
{
    private readonly Stopwatch _sinceLastReloadStopwatch = Stopwatch.StartNew();

    private Vector2 DrawTextWithBackground(string text, Vector2 position, Color color, Color backgroundColor)
    {
        var textSize = Graphics.MeasureText(text);
        Graphics.DrawBox(position, textSize + position, backgroundColor);
        Graphics.DrawText(text, position, color);
        return textSize;
    }

    public override void Render()
    {
        var floorWindow = GameController.IngameState.IngameUi.SanctumFloorWindow;
        if (!floorWindow.IsVisible)
        {
            return;
        }

        if (!GameController.Files.SanctumRooms.EntriesList.Any() && _sinceLastReloadStopwatch.Elapsed > TimeSpan.FromSeconds(5))
        {
            GameController.Files.LoadFiles();
            _sinceLastReloadStopwatch.Restart();
        }

        var hoveredRoom = floorWindow.Rooms.FirstOrDefault(x =>
            ImGui.IsMouseHoveringRect(x.GetClientRectCache.TopLeft.ToVector2Num(), x.GetClientRectCache.BottomRight.ToVector2Num(), false));
        var tooltipRect = RectangleF.Empty;
        if (hoveredRoom != null)
        {
            tooltipRect = hoveredRoom.Tooltip.GetClientRectCache;
        }

        var tierMap = new Dictionary<(int, int), (List<int> CurrencyTier, int? RoomTier, int? AfflictionTier)>();
        var roomsByLayer = floorWindow.RoomsByLayer;
        if (Settings.ConnectionLineThickness > 0)
        {
            for (var layerIndex = roomsByLayer.Count - 2; layerIndex >= 0; layerIndex--)
            {
                var roomLayer = roomsByLayer[layerIndex];
                for (var roomIndex = 0; roomIndex < roomLayer.Count; roomIndex++)
                {
                    var room = roomLayer[roomIndex];
                    (List<int> CurrencyTier, int? RoomTier, int? AfflictionTier) thisRoomData = (
                        room.Data.Rewards.Select(x => Settings.GetCurrencyTier(x.CurrencyName)).ToList(),
                        room.Data.RewardRoom?.RoomType?.Id switch
                        {
                            null => null,
                            var o => Settings.GetRoomTier(o)
                        },
                        (room.Data.RewardRoom?.RoomType?.Id, room.Data.RoomEffect?.ReadableName) switch
                        {
                            (not null, null) => 1,
                            (null, _) => null,
                            (not null, { } o) => Settings.GetAfflictionTier(o)
                        }
                    );
                    var connections = floorWindow.FloorData.RoomLayout[layerIndex][roomIndex];
                    var connectedRoomData = connections.Select(x => tierMap.GetValueOrDefault((layerIndex + 1, x)))
                        .Where(x => x != default).ToList();
                    if (connectedRoomData.Any())
                    {
                        var aggregateConnectionData = connectedRoomData
                            .Aggregate((current, connectionData) => (
                                current.CurrencyTier.Union(connectionData.CurrencyTier).ToList(),
                                (current.RoomTier, connectionData.RoomTier) switch
                                {
                                    ({ } tier1, { } tier2) => Math.Min(tier1, tier2),
                                    var (tier1, tier2) => tier1 ?? tier2
                                },
                                (current.AfflictionTier, connectionData.AfflictionTier) switch
                                {
                                    ({ } tier1, { } tier2) => Math.Min(tier1, tier2),
                                    var (tier1, tier2) => tier1 ?? tier2
                                }));
                        thisRoomData = (
                            thisRoomData.CurrencyTier.Union(aggregateConnectionData.CurrencyTier).ToList(),
                            (thisRoomData.RoomTier, aggregateConnectionData.RoomTier) switch
                            {
                                ({ } tier1, { } tier2) => Math.Min(tier1, tier2),
                                var (tier1, tier2) => tier1 ?? tier2
                            },
                            (thisRoomData.AfflictionTier, aggregateConnectionData.AfflictionTier) switch
                            {
                                ({ } tier1, { } tier2) => Math.Max(tier1, tier2),
                                var (tier1, tier2) => tier1 ?? tier2
                            });
                    }

                    tierMap[(layerIndex, roomIndex)] = thisRoomData;
                }
            }
        }

        for (var layerIndex = 0;
             layerIndex < roomsByLayer.Count;
             layerIndex++)
        {
            var roomLayer = roomsByLayer[layerIndex];
            for (var roomIndex = 0; roomIndex < roomLayer.Count; roomIndex++)
            {
                var room = roomLayer[roomIndex];
                var fightRoomId = room.Data.FightRoom?.RoomType?.Id;
                if (fightRoomId != null && Settings.ConnectionLineThickness > 0)
                {
                    var connections = floorWindow.FloorData.RoomLayout[layerIndex][roomIndex];
                    var connectedRoomData = connections.Select(index => (index, tierMap.GetValueOrDefault((layerIndex + 1, index))))
                        .Where(x => x.Item2 != default).ToList();
                    if (connectedRoomData.Any())
                    {
                        var leftPoint = new Vector2(room.GetClientRectCache.Right - 15, room.GetClientRectCache.Center.Y);
                        foreach (var (index, (currencyTier, roomTier, afflictionTier)) in connectedRoomData)
                        {
                            var connectedRoom = roomsByLayer[layerIndex + 1][index];
                            if (connectedRoom.Data.FightRoom?.RoomType?.Id == null)
                            {
                                continue;
                            }

                            var rightPoint = new Vector2(connectedRoom.GetClientRectCache.Left + 15, connectedRoom.GetClientRectCache.Center.Y);
                            if (tooltipRect.Intersects(new RectangleF(leftPoint.X, Math.Min(leftPoint.Y, rightPoint.Y),
                                    rightPoint.X - leftPoint.X,
                                    Math.Max(leftPoint.Y, rightPoint.Y) -
                                    Math.Min(leftPoint.Y, rightPoint.Y))))
                            {
                                continue;
                            }

                            var leftPointOffset = new Vector2(0, (rightPoint.Y - leftPoint.Y) * 0.25f);
                            var overlapOffsetVector = new Vector2(0,
                                Settings.ConnectionLineThickness * (0.5f + 0.5f * (rightPoint - leftPoint).Length() / (rightPoint.X - leftPoint.X)));
                            Graphics.DrawLine(leftPoint + leftPointOffset - overlapOffsetVector,
                                rightPoint - leftPointOffset - overlapOffsetVector,
                                Settings.ConnectionLineThickness,
                                currencyTier.Any() ? GetCurrencyColor(currencyTier.Min()) : Settings.EmptyColor);
                            Graphics.DrawLine(leftPoint + leftPointOffset,
                                rightPoint - leftPointOffset,
                                Settings.ConnectionLineThickness,
                                roomTier is { } ? GetRoomColor(roomTier.Value) : Settings.EmptyColor);
                            Graphics.DrawLine(leftPoint + leftPointOffset + overlapOffsetVector,
                                rightPoint - leftPointOffset + overlapOffsetVector,
                                Settings.ConnectionLineThickness,
                                afflictionTier is { } ? GetAfflictionColor(afflictionTier.Value) : Settings.EmptyColor);
                        }
                    }
                }

                if (room.GetClientRectCache.Intersects(tooltipRect))
                {
                    continue;
                }

                var textTopLeft = room.GetClientRectCache.TopLeft.ToVector2Num();
                var lineLocation = textTopLeft;
                var textSize = DrawTextWithBackground(fightRoomId ?? "??", lineLocation, GetRoomColor(fightRoomId), Settings.BackgroundColor);
                lineLocation.Y += textSize.Y;
                var rewardRoomId = room.Data.RewardRoom?.RoomType?.Id;
                textSize = DrawTextWithBackground($"->{rewardRoomId ?? "??"}", lineLocation, GetRoomColor(rewardRoomId), Settings.BackgroundColor);
                lineLocation.Y += textSize.Y;

                if (room.Data.Rewards is { Count: > 0 } rewards)
                {
                    textSize = DrawTextWithBackground("\nRewards:", lineLocation, Settings.TextColor, Settings.BackgroundColor);
                    lineLocation.Y += textSize.Y;
                    foreach (var reward in rewards)
                    {
                        var currencyName = reward.CurrencyName;
                        textSize = DrawTextWithBackground(currencyName, lineLocation, GetCurrencyColor(currencyName), Settings.BackgroundColor);
                        lineLocation.Y += textSize.Y;
                    }
                }

                if (room.Data.RoomEffect is { } effect)
                {
                    var text = "";
                    if (Settings.ShowEffectId)
                    {
                        text += $"{effect.Id}\n";
                    }

                    var effectName = effect.ReadableName;
                    if (Settings.ShowEffectName)
                    {
                        text += $"{effectName}\n";
                    }

                    if (Settings.ShowEffectDescription)
                    {
                        var maxWidth = room.GetClientRectCache.Width;
                        var splitDescription = effect.Description.Split(" ").Aggregate(new List<string> { "" }, (l, i) =>
                        {
                            if (l.Last().Length > 0 && Graphics.MeasureText(l.Last() + i).X > maxWidth)
                            {
                                return l.Append(i).ToList();
                            }

                            return l.SkipLast(1).Append($"{l.Last()} {i}").ToList();
                        });
                        text += $"{string.Join("\n", splitDescription)}\n";
                    }

                    textSize = DrawTextWithBackground(text, lineLocation, GetAfflictionColor(effectName), Settings.BackgroundColor);
                    lineLocation.Y += textSize.Y;
                }
            }
        }
    }

    private Color GetAfflictionColor(string effectName) => GetAfflictionColor(Settings.GetAfflictionTier(effectName));
    private Color GetCurrencyColor(string currencyName) => GetCurrencyColor(Settings.GetCurrencyTier(currencyName));
    private Color GetRoomColor(string fightRoomId) => GetRoomColor(Settings.GetRoomTier(fightRoomId));

    private ColorNode GetAfflictionColor(int afflictionTier)
    {
        return afflictionTier switch
        {
            1 => Settings.Tier1AfflictionColor,
            2 => Settings.Tier2AfflictionColor,
            3 => Settings.Tier3AfflictionColor,
        };
    }

    private ColorNode GetCurrencyColor(int currencyTier)
    {
        return currencyTier switch
        {
            1 => Settings.Tier1CurrencyColor,
            2 => Settings.Tier2CurrencyColor,
            3 => Settings.Tier3CurrencyColor,
        };
    }

    private ColorNode GetRoomColor(int roomTier)
    {
        return roomTier switch
        {
            1 => Settings.Tier1RoomColor,
            2 => Settings.Tier2RoomColor,
            3 => Settings.Tier3RoomColor,
        };
    }
}