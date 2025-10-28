using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GlimmerDex
{
    public class TopThreeMethodsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 0 || values[0] is not EncounterData data)
                return null;

            var methods = new Dictionary<string, int>
            {
                { "Encounters", data.Encounters },
                { "Eggs Hatched", data.EggsHatched },
                { "Soft Resets", data.SoftResets },
                { "SOS", data.SOSEncounters },
                { "Catch Combo", data.CatchComboEncounters },
                { "Outbreak", data.OutbreakEncounters },
                { "DexNav", data.DexNavEncounters }
            };

            return methods
                .Where(m => m.Value > 0)
                .OrderByDescending(m => m.Value)
                .Take(3)
                .Select(m => $"{m.Key}: {m.Value}")
                .ToList();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
