using System;
using System.Globalization;

namespace CraneMachine
{
    // Shared number abbreviation so every display clumps big numbers the same way:
    // 999 -> "999", 1_000 -> "1k", 1_500 -> "1.5k", 1_000_000 -> "1M", etc.
    //
    // KISS: one place to change the style. Small numbers pass through unchanged; large ones
    // get one decimal of precision (trimmed when it's a round value) and a suffix.
    public static class NumberFormat
    {
        // k, M, B, T, then Qa, Qi for the truly silly incremental-game numbers.
        private static readonly string[] Suffixes =
            { "", "k", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

        // Below this, show the number as-is (no suffix).
        private const double Threshold = 1000d;

        public static string Abbreviate(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "0";

            bool negative = value < 0;
            double abs = Math.Abs(value);

            if (abs < Threshold)
            {
                // Whole numbers stay whole; keep it simple.
                long rounded = (long)Math.Round(abs);
                return negative ? "-" + rounded.ToString(CultureInfo.InvariantCulture)
                                : rounded.ToString(CultureInfo.InvariantCulture);
            }

            int tier = (int)Math.Floor(Math.Log10(abs) / 3d);
            if (tier >= Suffixes.Length) tier = Suffixes.Length - 1;

            double scaled = abs / Math.Pow(1000d, tier);

            // Rounding to one decimal can push a value up to 1000 (e.g. 999,999 -> 1000.0k);
            // roll it into the next tier so it reads "1M" rather than "1000k".
            if (scaled >= 999.95d && tier < Suffixes.Length - 1)
            {
                tier++;
                scaled = abs / Math.Pow(1000d, tier);
            }

            // One decimal, but drop a trailing ".0" so "1.0k" reads "1k".
            string text = scaled.ToString("0.#", CultureInfo.InvariantCulture);

            return (negative ? "-" : "") + text + Suffixes[tier];
        }

        public static string Abbreviate(int value) => Abbreviate((double)value);
        public static string Abbreviate(long value) => Abbreviate((double)value);

        // Convenience for money: "$1.5k".
        public static string Money(double value) => "$" + Abbreviate(value);
    }
}