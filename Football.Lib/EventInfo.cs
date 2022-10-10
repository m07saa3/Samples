using System;
using System.Linq;

namespace FootballLib
{
    public class EventInfo
    {
        public string Id { get; set; }
        public DateTime? Begin { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string League { get; set; }
        public string Country { get; set; }
        public string Type { get; set; }
        public int? Result1 { get; set; }
        public int? Result2 { get; set; }
        public string Periods { get; set; }
        public string CleanHome { get; set; }
        public string CleanAway { get; set; }

        public string HomeId { get; set; }
        public string AwayId { get; set; }
        public string LeagueId { get; set; }
        public string Season { get; set; }
        public string Round { get; set; }

        public EventInfo()
        {
        }

        public EventInfo(string id, string home, string away, string league, DateTime? beginTime)
        {
            Id = id;
            Home = home;
            Away = away;
            League = league;
            Begin = beginTime;
            CleanTeams();
        }

        public void CleanTeams()
        {
            CleanHome = Home.Clean();
            CleanAway = Away.Clean();
        }

        public override string ToString() => string.Join(";", Id, Begin, Home, Away, League, Country, Type, Result1, Result2, Periods,HomeId,AwayId,LeagueId,Season,Round);
     
        public void FromCSV(string line)
        {
            var s = line.Split(';');
            Id = s[0];
            if(s[1]!="")
            Begin = DateTime.Parse(s[1]);
            Home = s[2];
            Away = s[3];
            League = s[4];
            Country = s[5];
            Type = s[6];
            if (s[7] != "") Result1 = int.Parse(s[7]);
            if (s[8] != "") Result2 = int.Parse(s[8]);
            Periods = s[9];
            HomeId = s[10];
            AwayId = s[11];
            LeagueId = s[12];
            Season = s[13];
            Round = s[14];
        }

        public const string Header = "Id;BeginTime;Home;Away;League;Country;Type;Result1;Result2;Periods;HomeId;AwayId;LeagueId;Season;Round";

        public int LoadFromMarathon(Juggernaut.Entity.Event ev)
        {
          
            Id = ev.Id.ToString();
            Begin = ev.TimeStart;
            Home = ev.Home;
            Away = ev.Away;
            League = ev.League;
            Country = Helper.GetCountryFromMarathon(ev.League);
            Type = Helper.GetType(ev.Home, ev.Away, ev.League);
            return 1;
        }
        public int LoadFromFonbet(Juggernaut.Entity.Event ev)
        {

            Id = ev.Id.ToString();
            Begin = ev.TimeStart;
            Home = ev.Home;
            Away = ev.Away;
            League = ev.League;
            Country = Helper.GetCountryFromFonbet(ev.League);
            Type = Helper.GetType(ev.Home, ev.Away, ev.League);
            return 1;
        }
        public int LoadFromBetsApi(BetsAPILib.Event ev)
        {
            Id = ev.Id.ToString();
            Begin = ev.Time;
            Home = ev.Home.Name;
            Away = ev.Away.Name;
            League = ev.League.Name;
            Country = Helper.GetCountryFromBetsApi(ev);
            Type = Helper.GetType(ev.Home.Name, ev.Away.Name, ev.League.Name);
            
            var score = Helper.GetCorrectScore(ev);
            if (score == null)
                return 0;

            Result1 = score.Value.Score1;
            Result2 = score.Value.Score2;
            if (ev.PeriodScores != null) Periods = ev.PeriodScores.ToString();
            return 1;
        }
        public int LoadFromSoccerWay(string line)
        {
            var s = line.Split(';');
            if (s[0] != "Played")
                return 0;
            if (s.Contains("1yigspelv7rgbtz4qyg6s6rro"))
            {

            }
            // url ------------------
            var leagueurl = s[9];
            var roundurl = s[10];
            var homeurl = s[12];
            var awayurl = s[13];
            var gameurl = s[11];
            var url = (roundurl == "" ? leagueurl : roundurl).Split('/');
            //EventInfo fields
            Id = s[14];
            Begin = Helper.UnixTimeToDateTime(long.Parse(s[7]), false);
            Home = s[3];
            Away = s[5];
            League = s[1].ToLower();
            Country = url[4].ToLower();
           // Country = Helper.GetCountryFromSoccerway(Country);
            Type = Helper.GetType(Home, Away, League);
            var score = s[4].Replace("E", "").Replace("P", "").Replace(" ", "").Split('-');
            if (score.Length == 2)
            {
                var good  = int.TryParse(score[0],out int i);
                if (good) Result1 = i;
                else return 0;
                good = int.TryParse(score[1], out int j);
                if (good) Result2 = j;
                else return 0;
            }

            AwayId = awayurl.Split('/').Length >= 7 ? awayurl.Split('/')[6] : "";
            HomeId = homeurl.Split('/').Length >= 7 ? homeurl.Split('/')[6] : "";
            Season = url[6];
            Round = url[7];
            LeagueId = leagueurl.TrimEnd('/').Split('/').Last();
            
            return 1;
        }
       
    }
}
