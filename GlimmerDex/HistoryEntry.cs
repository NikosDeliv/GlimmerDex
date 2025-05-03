using System;

namespace GlimmerDex
{
    public class HistoryEntry
    {
        public required string PokemonName { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
