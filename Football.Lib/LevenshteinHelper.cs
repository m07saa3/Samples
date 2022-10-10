#region Usings

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace FootballLib
{
    public static class LevenshteinHelper
    {
        #region Methods

        public static int GetLevenshteinDistance(string source, string target)
        {
            
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;
            if (string.IsNullOrEmpty(target))
                return source.Length;

            var m = source.Length;
            var n = target.Length;
            var h = new int[m + 2, n + 2];

            var inf = m + n;
            h[0, 0] = inf;
            for (var i = 0; i <= m; i++)
            {
                h[i + 1, 1] = i;
                h[i + 1, 0] = inf;
            }
            for (var j = 0; j <= n; j++)
            {
                h[1, j + 1] = j;
                h[0, j + 1] = inf;
            }

            var sd = new SortedDictionary<char, int>();
            foreach (var letter in source + target)
            {
                if (!sd.ContainsKey(letter))
                    sd.Add(letter, 0);
            }

            for (var i = 1; i <= m; i++)
            {
                var db = 0;
                for (var j = 1; j <= n; j++)
                {
                    var i1 = sd[target[j - 1]];
                    var j1 = db;

                    if (source[i - 1] == target[j - 1])
                    {
                        h[i + 1, j + 1] = h[i, j];
                        db = j;
                    }
                    else
                        h[i + 1, j + 1] = Math.Min(h[i, j], Math.Min(h[i + 1, j], h[i, j + 1])) + 1;

                    h[i + 1, j + 1] = Math.Min(h[i + 1, j + 1], h[i1, j1] + (i - i1 - 1) + 1 + (j - j1 - 1));
                }

                sd[source[i - 1]] = i;
            }

            return h[m + 1, n + 1];
        }

        #endregion
    }

    public class Dict
    {
        public string Country;
        public string Type;
        public string Team1;
        public string Team2;
        public int Count;

        public override string ToString()
        {
            return string.Join(";", Country, Type, Team1, Team2, Count);
        }

        public override bool Equals(object o)
        {
            var game = o as Dict;
            return Country == game.Country & Type == game.Type & Team1 == game.Team1 & Team2 == game.Team2;
        }

        public void ReadFromCSV(string line)
        {
            var s = line.Split(';');
            Country = s[0];
            Type = s[1];
            Team1 = s[2];
            Team2 = s[3];
            Count = int.Parse(s[4]);
        }

        public static List<Dict> ReadDictionary(string input)
        {
            var dic = new List<Dict>();
            if (!File.Exists(input)) return dic;
            var lines = File.ReadAllLines(input);
            foreach (var line in lines)
            {
                var d = new Dict();
                d.ReadFromCSV(line);
                dic.Add(d);
            }
            return dic;
        }
    }
}