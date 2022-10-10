using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using BetsAPILib;


namespace FootballLib
{
    public class Stats
    {
        public DateTime StatTime { get; set; }
        public bool Active { get; set; }
        public int Minute { get; set; }
        public int? Score1 { get; set; }
        public int? Score2 { get; set; }
        public int? A1 { get; set; }
        public int? A2 { get; set; }
        public int? DA1 { get; set; }
        public int? DA2 { get; set; }
        public int? Off1 { get; set; }
        public int? Off2 { get; set; }
        public int? On1 { get; set; }
        public int? On2 { get; set; }
        public int? Pos1 { get; set; }
        public int? Pos2 { get; set; }
        public int? YC1 { get; set; }
        public int? YC2 { get; set; }
        public int? RC1 { get; set; }
        public int? RC2 { get; set; }
        public int? Pen1 { get; set; }
        public int? Pen2 { get; set; }
        public int? Sub1 { get; set; }
        public int? Sub2 { get; set; }
        public int? Cor1 { get; set; }
        public int? Cor2 { get; set; }
        public string Comment { get; set; }

        public byte? Period { get; set; } // null - прем, 0 - прелайв, 1 2 понятно 3 4 этовсе ET 

        public Stats(int i)
        {
            Minute = i + 1;
            Active = false;

        }

        public Stats()
        {
            Active = false;
        }

        public override string ToString() =>
            String.Join(";", StatTime == DateTime.MinValue ? "" : StatTime.ToString(), Minute, Active ? "1" : "0", Score1, Score2, A1, A2, DA1, DA2, Pos1, Pos2,
                Off1, Off2,
                On1, On2, YC1, YC2, RC1, RC2, Sub1, Sub2, Pen1, Pen2, Cor1, Cor2, Period, Comment);

        public void FromCSV(string s)
        {
            var l = s.Split(';');
            if (l[0] != "") StatTime = DateTime.ParseExact(l[0], "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture);
            Minute = int.Parse(l[1]);
            Active = l[2] == "1";
            if (l[3] != "") Score1 = int.Parse(l[3]);
            if (l[4] != "") Score2 = int.Parse(l[4]);
            if (l[5] != "") A1 = int.Parse(l[5]);
            if (l[6] != "") A2 = int.Parse(l[6]);
            if (l[7] != "") DA1 = int.Parse(l[7]);
            if (l[8] != "") DA2 = int.Parse(l[8]);
            if (l[9] != "") Pos1 = int.Parse(l[9]);
            if (l[10] != "") Pos2 = int.Parse(l[10]);
            if (l[11] != "") Off1 = int.Parse(l[11]);
            if (l[12] != "") Off2 = int.Parse(l[12]);
            if (l[13] != "") On1 = int.Parse(l[13]);
            if (l[14] != "") On2 = int.Parse(l[14]);
            if (l[15] != "") YC1 = int.Parse(l[15]);
            if (l[16] != "") YC2 = int.Parse(l[16]);
            if (l[17] != "") RC1 = int.Parse(l[17]);
            if (l[18] != "") RC2 = int.Parse(l[18]);
            if (l[19] != "") Sub1 = int.Parse(l[19]);
            if (l[20] != "") Sub2 = int.Parse(l[20]);
            if (l[21] != "") Pen1 = int.Parse(l[21]);
            if (l[22] != "") Pen2 = int.Parse(l[22]);
            if (l[23] != "") Cor1 = int.Parse(l[23]);
            if (l[24] != "") Cor2 = int.Parse(l[24]);
            if (l[25] != "") Period = byte.Parse(l[25]);
            if (l.Length > 26)
                Comment = l[26];
        }

        public const string Header = "StatTime;Minute;Active;Score1;Score2;A1;A2;DA1;DA2;Pos1;Pos2;Off1;Off2;On1;On2;YC1;YC2;RC1;RC2;Sub1;Sub2;Pen1;Pen2;Cor1;Cor2;Period;Comment";

        public int GetStatsFromEvent(Event ev)
        {
            if (ev?.Stats == null) return 0;
            if (ev?.Stats?.Goals == null | ev?.Stats?.Attacks == null | ev?.Stats?.DangerousAttacks == null)
                return 0;
            if (ev?.Stats?.Goals.Length != 2 | ev?.Stats?.Attacks.Length != 2 |
                ev?.Stats?.DangerousAttacks.Length != 2) return 0;
            StatTime = ev.Time;
            Score1 = ev.Stats.Goals[0];
            Score2 = ev.Stats.Goals[1];

            A1 = ev.Stats.Attacks[0];
            A2 = ev.Stats.Attacks[1];

            DA1 = ev.Stats.DangerousAttacks[0];
            DA2 = ev.Stats.DangerousAttacks[1];

            if (ev?.Stats?.PossessionRt != null && ev?.Stats?.PossessionRt.Length == 2)
            {
                Pos1 = ev.Stats.PossessionRt[0];
                Pos2 = ev.Stats.PossessionRt[1];
            }

            if (ev?.Stats?.OnTarget != null && ev?.Stats?.OnTarget.Length == 2)
            {
                On1 = ev.Stats.OnTarget[0];
                On2 = ev.Stats.OnTarget[1];
            }

            if (ev?.Stats?.OffTarget != null && ev?.Stats?.OffTarget.Length == 2)
            {
                Off1 = ev.Stats.OffTarget[0];
                Off2 = ev.Stats.OffTarget[1];
            }

            if (ev?.Stats?.YellowCards != null && ev?.Stats?.YellowCards.Length == 2)
            {
                YC1 = ev.Stats.YellowCards[0];
                YC2 = ev.Stats.YellowCards[1];
            }

            if (ev?.Stats?.Redcards != null && ev?.Stats?.Redcards.Length == 2)
            {
                RC1 = ev.Stats.Redcards[0];
                RC2 = ev.Stats.Redcards[1];
            }

            if (ev?.Stats?.Corners != null && ev?.Stats?.Corners.Length == 2)
            {
                Cor1 = ev.Stats.Corners[0];
                Cor2 = ev.Stats.Corners[1];
            }

            if (ev?.Stats?.Penalties != null && ev?.Stats?.Penalties.Length == 2)
            {
                Pen1 = ev.Stats.Penalties[0];
                Pen2 = ev.Stats.Penalties[1];
            }

            if (ev?.Stats?.Substitutions != null && ev?.Stats?.Substitutions.Length == 2)
            {
                Sub1 = ev.Stats.Substitutions[0];
                Sub2 = ev.Stats.Substitutions[1];
            }

            return 1;
        }

        public static Stats[] ReadFromFile(string directory, DateTime date, string id)
        {
            var file = Path.Combine(directory, Helper.DateToString(date), id + ".csv");
            if (!File.Exists(file)) return null;
            var lines = File.ReadAllLines(file).Skip(1);
            var s = new List<Stats>();
            foreach (var line in lines)
            {
                var e = new Stats();
                e.FromCSV(line);
                s.Add(e);
            }

            return s.ToArray();
        }

        public void ReplaceNullValues()
        {
            if (Score1 == null)
                Score1 = 0;

            if (Score2 == null)
                Score2 = 0;

            if (A1 == null)
                A1 = 0;

            if (A2 == null)
                A2 = 0;

            if (DA1 == null)
                DA1 = 0;

            if (DA2 == null)
                DA2 = 0;

            if (Off1 == null)
                Off1 = 0;

            if (Off2 == null)
                Off2 = 0;

            if (On1 == null)
                On1 = 0;

            if (On2 == null)
                On2 = 0;

            if (Pos1 == null)
                Pos1 = 0;

            if (Pos2 == null)
                Pos2 = 0;

            if (YC1 == null)
                YC1 = 0;

            if (YC2 == null)
                YC2 = 0;

            if (RC1 == null)
                RC1 = 0;

            if (RC2 == null)
                RC2 = 0;

            if (Pen1 == null)
                Pen1 = 0;

            if (Pen2 == null)
                Pen2 = 0;

            if (YC1 == null)
                YC1 = 0;

            if (YC2 == null)
                YC2 = 0;

            if (RC1 == null)
                RC1 = 0;

            if (RC2 == null)
                RC2 = 0;

            if (Pen1 == null)
                Pen1 = 0;

            if (Pen2 == null)
                Pen2 = 0;

            if (Sub1 == null)
                Sub1 = 0;

            if (Sub2 == null)
                Sub2 = 0;

            if (Cor1 == null)
                Cor1 = 0;

            if (Cor2 == null)
                Cor2 = 0;

        }
    }
}