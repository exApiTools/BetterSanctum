using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using Color = SharpDX.Color;

namespace BetterSanctum;

public class BetterSanctumSettings : ISettings
{
    private static readonly IReadOnlyList<string> CurrencyTypes = new List<string>
    {
        "Orbs of Alteration",
        "Orbs of Chance",
        "Glassblower's Baubles",
        "Chromatic Orbs",
        "Jeweller's Orbs",
        "Orbs of Alchemy",
        "Orbs of Fusing",
        "Orbs of Scouring",
        "Cartographer's Chisels",
        "Chaos Orbs",
        "Orbs of Binding",
        "Orbs of Regret",
        "Gemcutter's Prisms",
        "Blessed Orbs",
        "Vaal Orbs",
        "Orbs of Horizon",
        "Instilling Orbs",
        "Regal Orbs",
        "Enkindling Orbs",
        "Orbs of Unmaking",
        "Awakened Sextants",
        "Stacked Decks",
        "Veiled Chaos Orbs",
        "Orbs of Annulment",
        "Divine Orbs",
        "Exalted Orbs",
        "Divine Vessels",
        "Sacred Orbs",
        "Mirrors of Kalandra",
        "Blacksmith's Whetstones",
        "Armourer's Scraps",
        "Orbs of Transmutation",
        "Orbs of Augmentation",
    };

    private static readonly IReadOnlyList<string> RoomTypes = new List<string>
    {
        "Explore",
        "Arena",
        "Lair",
        "Maze",
        "Gauntlet",
        "Miniboss",
        "Vault",
        "Puzzle",
        "Boss",
        "Merchant",
        "Fountain",
        "Deal",
        "Deferral",
        "CurseFountain",
        "BoonFountain",
        "RainbowFountain",
        "Treasure",
        "TreasureMinor",
        "Final",
    };

    private static readonly IReadOnlyList<(string, string)> AfflictionTypes = new List<(string, string)>
    {
        ("Accursed Prism", "When you gain an Affliction, gain an additional random Minor Affliction"),
        ("Poisoned Water", "Gain a random Minor Affliction when you use a Fountain"),
        ("Glass Shard", "The next Boon you gain is converted into a random Minor Affliction"),
        ("Cutpurse", "You cannot gain Aureus coins"),
        ("Corrupted Lockpick", "Chests in rooms explode when opened"),
        ("Voodoo Doll", "100% more Resolve lost while Resolve is below 50%"),
        ("Phantom Illusion", "Every room grants a random Minor Affliction, Afflictions granted this way are removed on room completion"),
        ("Gargoyle Totem", "Guards are accompanied by a Gargoyle"),
        ("Purple Smoke", "Afflictions are unknown on the Sanctum Map"),
        ("Veiled Sight", "Rooms are unknown on the Sanctum Map"),
        ("Red Smoke", "Room types are unknown on the Sanctum Map"),
        ("Golden Smoke", "Rewards are unknown on the Sanctum Map"),
        ("Blunt Sword", "You and your Minions deal 25% less Damage"),
        ("Charred Coin", "50% less Aureus coins found"),
        ("Deadly Snare", "Traps impact infinite Resolve"),
        ("Spiked Exit", "Lose 5% of current Resolve on room completion"),
        ("Floor Tax", "Lose all Aureus on floor completion"),
        ("Door Tax", "Lose 30 Aureus coins on room completion"),
        ("Spilt Purse", "Lose 20 Aureus coins when you lose Resolve from a Hit"),
        ("Liquid Cowardice", "Lose 10 Resolve when you use a Flask"),
        ("Tight Choker", "You can have a maximum of 5 Boons"),
        ("Unhallowed Ring", "50% increased Merchant prices"),
        ("Unhallowed Amulet", "The Merchant offers 50% fewer choices"),
        ("Rusted Coin", "The Merchant only offers one choice"),
        ("Honed Claws", "Monsters deal 25% more Damage"),
        ("Spiked Shell", "Monsters have 30% increased Maximum Life"),
        ("Chiselled Stone", "Monsters Petrify on Hit"),
        ("Hungry Fangs", "Monsters impact 25% increased Resolve"),
        ("Chains of Binding", "Monsters inflict Binding Chains on Hit"),
        ("Rusted Mallet", "Monsters always Knockback, Monsters have increased Knockback Distance"),
        ("Fiendish Wings", "Monsters' Action Speed cannot be slowed below base, Monsters have 30% increased Attack, Cast and Movement Speed"),
        ("Mark of Terror", "Monsters inflict Resolve Weakness on Hit"),
        ("Concealed Anomaly", "Guards release a Volatile Anomaly on Death"),
        ("Empty Trove", "Chests no longer drop Aureus coins"),
        ("Death Toll", "Monsters no longer drop Aureus coins"),
        ("Tattered Blindfold", "90% reduced Light Radius, Minimap is hidden"),
        ("Haemorrhage", "You cannot recover Resolve (removed after killing the next Floor Boss)"),
        ("Demonic Skull", "Cannot recover Resolve"),
        ("Unassuming Brick", "You cannot gain any more Boons"),
        ("Unholy Urn", "50% reduced Effect of your Relics"),
        ("Weakened Flesh", "-100 to Maximum Resolve"),
        ("Worn Sandals", "40% reduced Movement Speed"),
        ("Orb of Negation", "Relics have no Effect"),
        ("Ghastly Scythe", "Losing Resolve ends your Sanctum"),
        ("Unquenched Thirst", "50% reduced Resolve recovered"),
        ("Dark Pit", "Traps impact 100% increased Resolve"),
        ("Rapid Quicksand", "Traps are faster"),
        ("Anomaly Attractor", "Rooms spawn Volatile Anomalies"),
        ("Black Smoke", "You can see one fewer room ahead on the Sanctum Map"),
        ("Deceptive Mirror", "You are not always taken to the room you select"),
    };

    public BetterSanctumSettings()
    {
        var currencyFilter = "";
        var roomFilter = "";
        var afflictionFilter = "";
        TieringNode = new CustomNode
        {
            DrawDelegate = () =>
            {
                var (profileName, profile) = GetCurrentProfile();
                foreach (var key in Profiles.Keys.OrderBy(x => x).ToList())
                {
                    if (key == profileName)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, Color.Green.ToImgui());
                        var editedKey = key;
                        if (ImGui.InputText("Edit current profile name", ref editedKey, 200))
                        {
                            Profiles[editedKey] = Profiles[key];
                            Profiles.Remove(key);
                        }

                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        if (ImGui.Button($"Activate profile {key}##profile"))
                        {
                            CurrentProfile = key;
                        }
                    }
                }

                if (ImGui.Button("Add profile##addProfile"))
                {
                    var newProfileName = Enumerable.Range(0, 100).Select(x => $"New profile {x}").First(x => !Profiles.ContainsKey(x));
                    Profiles[newProfileName] = new ProfileContent();
                }

                if (ImGui.TreeNode("Currency tiering"))
                {
                    ImGui.InputTextWithHint("##CurrencyFilter", "Filter", ref currencyFilter, 100);
                    foreach (var type in CurrencyTypes.Where(t => t.Contains(currencyFilter, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var currentValue = GetCurrencyTier(type);
                        if (ImGui.SliderInt(type, ref currentValue, 1, 3))
                        {
                            profile.CurrencyTiers[type] = currentValue;
                        }
                    }

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Room tiering"))
                {
                    ImGui.InputTextWithHint("##RoomFilter", "Filter", ref roomFilter, 100);
                    foreach (var type in RoomTypes.Where(t => t.Contains(roomFilter, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var currentValue = GetRoomTier(type);
                        if (ImGui.SliderInt(type, ref currentValue, 1, 3))
                        {
                            profile.RoomTiers[type] = currentValue;
                        }
                    }

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Affliction tiering"))
                {
                    ImGui.InputTextWithHint("##AfflictionFilter", "Filter", ref afflictionFilter, 100);
                    foreach (var (type, description) in AfflictionTypes.Where(t =>
                                 t.Item1.Contains(afflictionFilter, StringComparison.InvariantCultureIgnoreCase) ||
                                 t.Item2.Contains(afflictionFilter, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var currentValue = GetAfflictionTier(type);
                        if (ImGui.SliderInt(type, ref currentValue, 1, 3))
                        {
                            profile.AfflictionTiers[type] = currentValue;
                        }

                        ImGui.SameLine();
                        ImGui.TextDisabled("(?)");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(description);
                        }
                    }

                    ImGui.TreePop();
                }
            }
        };
    }

    private (string profileName, ProfileContent profile) GetCurrentProfile()
    {
        var profileName = CurrentProfile != null && Profiles.ContainsKey(CurrentProfile) ? CurrentProfile : Profiles.Keys.FirstOrDefault() ?? "Default";
        if (!Profiles.ContainsKey(profileName))
        {
            Profiles[profileName] = new ProfileContent();
        }

        var profile = Profiles[profileName];
        return (profileName, profile);
    }

    public int GetRoomTier(string type)
    {
        return GetCurrentProfile().profile.RoomTiers.GetValueOrDefault(type ?? "", 2);
    }

    public int GetCurrencyTier(string type)
    {
        return GetCurrentProfile().profile.CurrencyTiers.GetValueOrDefault(type ?? "", 3);
    }

    public int GetAfflictionTier(string type)
    {
        return GetCurrentProfile().profile.AfflictionTiers.GetValueOrDefault(type ?? "", 2);
    }

    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    public ColorNode TextColor { get; set; } = new ColorNode(Color.White);
    public ColorNode BackgroundColor { get; set; } = new ColorNode(Color.Black with { A = 128 });
    public ToggleNode ShowEffectId { get; set; } = new ToggleNode(false);
    public ToggleNode ShowEffectName { get; set; } = new ToggleNode(true);
    public ToggleNode ShowEffectDescription { get; set; } = new ToggleNode(true);

    public ColorNode Tier1RoomColor { get; set; } = new(Color.Green);
    public ColorNode Tier2RoomColor { get; set; } = new(Color.White);
    public ColorNode Tier3RoomColor { get; set; } = new(Color.Red);

    public ColorNode Tier1CurrencyColor { get; set; } = new(Color.Violet);
    public ColorNode Tier2CurrencyColor { get; set; } = new(Color.Green);
    public ColorNode Tier3CurrencyColor { get; set; } = new(Color.White);

    public ColorNode Tier1AfflictionColor { get; set; } = new(Color.Green);
    public ColorNode Tier2AfflictionColor { get; set; } = new(Color.White);
    public ColorNode Tier3AfflictionColor { get; set; } = new(Color.Red);
    public ColorNode EmptyColor { get; set; } = new(Color.Gray);

    public RangeNode<int> ConnectionLineThickness { get; set; } = new RangeNode<int>(5, 0, 10);

    public Dictionary<string, ProfileContent> Profiles = new Dictionary<string, ProfileContent>
    {
        ["Default"] = new ProfileContent()
    };

    public string CurrentProfile;

    [JsonIgnore]
    public CustomNode TieringNode { get; set; }
}

public class ProfileContent
{
    public Dictionary<string, int> CurrencyTiers = new()
    {
        ["Mirrors of Kalandra"] = 1,
        ["Divine Orbs"] = 1,
        ["Chaos Orbs"] = 2,
        ["Awakened Sextants"] = 2,
        ["Stacked Decks"] = 2,
        ["Veiled Chaos Orbs"] = 2,
        ["Orbs of Annulment"] = 2,
        ["Exalted Orbs"] = 2,
    };

    public Dictionary<string, int> RoomTiers = new()
    {
        ["Explore"] = 1,
        ["Merchant"] = 1,
        ["CurseFountain"] = 3,
        ["Arena"] = 3,
    };

    public Dictionary<string, int> AfflictionTiers = new()
    {
        ["Accursed Prism"] = 3,
        ["Poisoned Water"] = 3,
        ["Cutpurse"] = 3,
        ["Purple Smoke"] = 3,
        ["Veiled Sight"] = 3,
        ["Red Smoke"] = 3,
        ["Golden Smoke"] = 3,
        ["Deadly Snare"] = 3,
        ["Floor Tax"] = 3,
        ["Liquid Cowardice"] = 3,
        ["Unhallowed Amulet"] = 3,
        ["Rusted Coin"] = 3,
        ["Chiselled Stone"] = 3,
        ["Fiendish Wings"] = 3,
        ["Empty Trove"] = 3,
        ["Demonic Skull"] = 3,
        ["Unassuming Brick"] = 3,
        ["Worn Sandals"] = 3,
        ["Ghastly Scythe"] = 3,
        ["Rapid Quicksand"] = 3,
        ["Black Smoke"] = 3,
        ["Deceptive Mirror"] = 3,
        ["Tattered Blindfold"] = 1,
    };
}