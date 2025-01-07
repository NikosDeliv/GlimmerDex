using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlimmerDex
{
    internal class HistoryEntry
    {
        public string PokemonName { get; set; }
        public int Encounters { get; set; }
        public bool IsShiny { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
