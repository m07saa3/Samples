using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FootballLib
{
    public class Coef
    {
        public DateTime Time { get; set; } //время коэффициента
        public bool Active { get; set; } // флаг
        public int? Minute { get; set; } // Поле минута
        public string RawTime { get; set; } // сюда пихаем время с инфо букмекера
        public int? Score1 { get; set; }
        public int? Score2 { get; set; }
        public int? Period { get; set; } // номер тайма 0 = до начала матча
        public decimal? Value { get; set; } // поле для Total,Fora
        public decimal? W1 { get; set; }
        public decimal? W2 { get; set; }
        public decimal? WX { get; set; }


        public Coef()
        {
        }

        public Coef(DateTime t, decimal w1, decimal wx, decimal w2)
        {
            Time = t;
            W1 = w1;
            WX = wx;
            W2 = w2;
        }

        public override string ToString()
        {
            return $"{Time} [{Period} : {Minute}'  {Score1}-{Score2}] {W1} {WX} {W2} ({RawTime})";
        }

        public  string ToCSV() =>
            String.Join(";", Time == DateTime.MinValue ? (object) "" : Time, Minute, RawTime, Score1, Score2, Period, Value, W1, WX, W2);

        public void FromCSV(string line)
        {
            var s = line.Split(';');
            Time = DateTime.Parse(s[0]);
            if (s[1] != "") Minute = int.Parse(s[1]);
            RawTime = s[2];
            if (s[3] != "") Score1 = int.Parse(s[3]);
            if (s[4] != "") Score2 = int.Parse(s[4]);
            if (s[5] != "") Period = int.Parse(s[5]);
            if (s[6] != "") Value = decimal.Parse(s[6]);
            if (s[7] != "") W1 = decimal.Parse(s[7], CultureInfo.InvariantCulture);
            if (s[8] != "") WX = decimal.Parse(s[8], CultureInfo.InvariantCulture);
            if (s[9] != "") W2 = decimal.Parse(s[9], CultureInfo.InvariantCulture);

        }

        public const string HEADER = "Time;Minute;RawTime;Score1;Score2;Period;V;W1;WX;W2";

        public static Coef[] ReadFromFile(string directory, DateTime date, string id, int? period)
        {
            var file = Path.Combine(directory, Helper.DateToString(date), id + ".csv");
            if (!File.Exists(file)) return null;
            var lines = File.ReadAllLines(file).Skip(1);
            var s = new List<Coef>();
            foreach (var line in lines)
            {
                var e = new Coef();
                e.FromCSV(line);
                s.Add(e);
            }

            if (period != null)
            {
                s = s.Where(t => t.Period == period).ToList();
            }
            return s.ToArray();
        }
    }
}