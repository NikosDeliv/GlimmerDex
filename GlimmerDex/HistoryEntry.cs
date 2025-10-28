using System;

namespace GlimmerDex
{
    public class HistoryEntry
    {
        public required string PokemonName { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Game { get; set; }
        public required EncounterData EncounterData { get; set; }

        // Add PokemonId to construct the shiny icon URL
        public int PokemonId { get; set; }

        // Shiny box sprite URL
        public string ShinyIconUrl
        {
            get
            {
                if (PokemonId > 0)
                {
                    return $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/shiny/{PokemonId}.png";
                }
                // If PokemonId is not set, return empty string to not show broken image
                return string.Empty;
            }
        }

        // Sum up all encounter types to show a total
        public int TotalEncounters =>
            (EncounterData?.Encounters ?? 0) +
            (EncounterData?.EggsHatched ?? 0) +
            (EncounterData?.SoftResets ?? 0) +
            (EncounterData?.SOSEncounters ?? 0) +
            (EncounterData?.CatchComboEncounters ?? 0) +
            (EncounterData?.OutbreakEncounters ?? 0) +
            (EncounterData?.DexNavEncounters ?? 0);

        // Return the first non-zero encounter type for easier display
        public string EncounterType
        {
            get
            {
                if (EncounterData == null) return "Unknown";
                if (EncounterData.EggsHatched > 0) return "Egg Hatch";
                if (EncounterData.SoftResets > 0) return "Soft Reset";
                if (EncounterData.SOSEncounters > 0) return "SOS Chaining";
                if (EncounterData.CatchComboEncounters > 0) return "Catch Combo";
                if (EncounterData.OutbreakEncounters > 0) return "Outbreak";
                if (EncounterData.DexNavEncounters > 0) return "DexNav";
                if (EncounterData.Encounters > 0) return "Wild Encounter";
                return "Unknown";
            }
        }
    }
}