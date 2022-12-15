using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;

namespace BetterSanctum;

public class BetterSanctumPlugin : BaseSettingsPlugin<BetterSanctumSettings>
{
    public override void Render()
    {
        var floorWindow = GameController.IngameState.IngameUi.SanctumFloorWindow;
        if (!floorWindow.IsVisible)
        {
            return;
        }

        var hoveredRoom = floorWindow.Rooms.FirstOrDefault(x =>
            ImGui.IsMouseHoveringRect(x.GetClientRectCache.TopLeft.ToVector2Num(), x.GetClientRectCache.BottomRight.ToVector2Num(), false));
        var tooltipRect = RectangleF.Empty;
        if (hoveredRoom != null)
        {
            tooltipRect = hoveredRoom.Tooltip.GetClientRectCache;
        }

        foreach (var room in floorWindow.Rooms)
        {
            var text = "";
            if (room.FightRoom?.RoomType?.Id is "Explore" or "Maze" or "Gauntlet" or "Puzzle")
            {
                text += "!";
            }

            text += $"{room.FightRoom?.RoomType?.Id ?? "??"}\n->{room.RewardRoom?.RoomType?.Id}\n";
            if (room.Rewards is { Count: > 0 } rewards)
            {
                text += "Rewards:\n";
                text += string.Join("\n", rewards.Select(x => x.CurrencyName switch
                {
                    "Chaos Orb" or
                        "Awakened Sextant" or
                        "Stacked Deck" or
                        "Veiled Chaos Orb" or
                        "Orb of Annulment" or
                        "Divine Orb" or
                        "Exalted Orb" or
                        "Sacred Orb" or
                        "Mirror of Kalandra" => $"!{x.CurrencyName}",
                    _ => x.CurrencyName
                }));
            }

            if (room.RoomEffect is { } effect)
            {
                if (Settings.ShowEffectId)
                {
                    text += $"{effect.Id}\n";
                }

                if (Settings.ShowEffectName)
                {
                    text += $"{effect.ReadableName}\n";
                }

                if (Settings.ShowEffectDescription)
                {
                    var splitDescription = effect.Description.Split(" ").Aggregate(new List<string> { "" }, (l, i) =>
                    {
                        if (l.Last().Length > 0 && Graphics.MeasureText(l.Last() + i).X > room.GetClientRectCache.Width)
                        {
                            return l.Append(i).ToList();
                        }

                        return l.SkipLast(1).Append($"{l.Last()} {i}").ToList();
                    });
                    text += $"{string.Join("\n", splitDescription)}\n";
                }
            }

            text = text.Trim();
            var textSize = Graphics.MeasureText(text);

            var textTopLeft = room.GetClientRectCache.TopLeft.ToVector2Num();
            if (tooltipRect.Intersects(new RectangleF(textTopLeft.X, textTopLeft.Y, textSize.X, textSize.Y)))
            {
                continue;
            }

            Graphics.DrawBox(textTopLeft, textTopLeft + textSize, Settings.BackgroundColor);
            var textLocation = textTopLeft;
            foreach (var line in text.Split("\n").Select(x => x.Trim()))
            {
                textLocation.Y += Graphics.DrawText(line, textLocation, line.StartsWith("!") ? Settings.GoodTextColor : Settings.TextColor).Y;
            }
        }
    }
}