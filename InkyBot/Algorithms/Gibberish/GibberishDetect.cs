using System.Text.RegularExpressions;

namespace InkyBot.Algorithms.Gibberish
{
    public class Gibberish
    {
        public static List<string> SplitInChunks(string text, int chunkSize)
        {
            List<string> chunks = new List<string>();
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                int size = Math.Min(chunkSize, text.Length - i);
                chunks.Add(text.Substring(i, size));
            }
            int lastIndex = chunks.Count - 1;
            if (chunks.Count > 1 && chunks[lastIndex].Length < 10)
            {
                chunks[chunks.Count - 2] += chunks[lastIndex];
                chunks.RemoveAt(lastIndex);
            }
            return chunks;
        }

        public static double UniqueCharsPerChunkPercentage(string text, int chunkSize)
        {
            List<string> chunks = SplitInChunks(text, chunkSize);
            double[] uniqueCharsPercentages = new double[chunks.Count];
            for (int x = 0; x < chunks.Count; x++)
            {
                int total = chunks[x].Length;
                int unique = chunks[x].Distinct().Count();
                uniqueCharsPercentages[x] = (double)unique / (double)total;
            }
            return uniqueCharsPercentages.Average() * 100;
        }

        public static double VowelsPercentage(string text)
        {
            int vowels = 0, total = 0;
            foreach (char c in text)
            {
                if (!char.IsLetter(c))
                {
                    continue;
                }
                total++;
                if ("aeiouAEIOU".Contains(c))
                {
                    vowels++;
                }
            }
            if (total != 0)
            {
                return vowels / (double)total * 100;
            }
            else
            {
                return 0;
            }
        }

        public static double DeviationScore(double percentage, double lowerBound, double upperBound)
        {
            if (percentage < lowerBound)
            {
                return Math.Log(lowerBound - percentage, lowerBound) * 100;
            }
            else if (percentage > upperBound)
            {
                return Math.Log(percentage - upperBound, 100 - upperBound) * 100;
            }
            else
            {
                return 0;
            }
        }

        public static double WordToCharRatio(string text)
        {
            int chars = text.Length;
            int words = Regex.Split(text, @"[\W_]")
                             .Where(x => !String.IsNullOrWhiteSpace(x))
                             .Count();
            return words / (double)chars * 100;
        }

        public static double Classify(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return 0;
            }
            double ucpcp = UniqueCharsPerChunkPercentage(text, 35);
            double vp = VowelsPercentage(text);
            double wtcr = WordToCharRatio(text);

            double ucpcpDev = Math.Max(DeviationScore(ucpcp, 45, 50), 1);
            double vpDev = Math.Max(DeviationScore(vp, 35, 45), 1);
            double wtcrDev = Math.Max(DeviationScore(wtcr, 15, 20), 1);

            return Math.Max((Math.Log10(ucpcpDev) + Math.Log10(vpDev) + Math.Log10(wtcrDev)) / 6 * 100, 1);
        }
    }
}
