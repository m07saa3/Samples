using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Juggernaut.Entity;
using Event = BetsAPILib.Event;
using BetsAPILib;
using Juggernaut.FonbetLib;
using Juggernaut.MarathonLib;
using MoreLinq;

namespace FootballLib
{
    //запихали сюда разные функции
    public static class Helper
    {
        #region betsapi
        public static (int Score1,int Score2)? GetCorrectScore(Event ev)
        {
            if (ev.Score == null)
                return null;
            var result = ev.Score.Split('-');
            if (result.Length != 2)
                return null;
            var res = int.TryParse(result[0], out int score1);
            if (!res)
                return null;
            res = int.TryParse(result[1], out int score2);
            if (!res)
                return null;

            // в рассчет скора идут пенки поэтому мы берем сначала рез второго тайма потом голы из статы а потом только скор
            if (ev?.PeriodScores?.Period2 != null)
            {
                var ft1 = ev.PeriodScores.Period2.Home;
                var ft2 = ev.PeriodScores.Period2.Away;
                return (ft1, ft2);
            }

            if (ev?.Stats?.Goals != null && ev.Stats.Goals.Length == 2)
            {
                var ft1 = ev.Stats.Goals[0];
                var ft2 = ev.Stats.Goals[1];
                return (ft1, ft2);
            }

            if (score1 + score2 > 10)
            {
                throw new Exception("sus score");
            }

            return (score1, score2);
        }

        public static string GetCountryFromBetsApi(Event ev)
        {
            var league = ev.League.Name.ToLower();
            var country = ev.League.Country;
            if (country == null)
            {
                if (league.Contains("zanzibar")) return "zz";
                if (league.Contains("australia")) return "au";
                if (league.Contains("belgium")) return "be";
                if (league.Contains("bhutan")) return "bt";
                if (league.Contains("bosnia")) return "ba";
                if (league.Contains("brazil")) return "br";
                if (league.Contains("costa rica")) return "cr";
                if (league.Contains("denmark")) return "dk";
                if (league.Contains("dominican republic")) return "dm";
                if (league.Contains("emirates")) return "ae";
                if (league.Contains("england")) return "gb";
                if (league.Contains("france")) return "fr";
                if (league.Contains("germany")) return "de";
                if (league.Contains("honduras")) return "hn";
                if (league.Contains("iceland")) return "is";
                if (league.Contains("hong kong")) return "hk";
                if (league.Contains("ivory coast")) return "ci";
                if (league.Contains("kazakhstan")) return "kz";
                if (league.Contains("kent ")) return "gb";
                if (league.Contains("kosovo")) return "xk";
                if (league.Contains("mexico")) return "mx";
                if (league.Contains("puerto rico")) return "pr";
                if (league.Contains("scotland")) return "gb";
                if (league.Contains("scottish")) return "gb";
                if (league.Contains("trinidad & tobago")) return "tt";
                if (league.Contains("turkey")) return "tr";
                if (league.Contains("wales")) return "gb";
            }

            if (country == "eu") return ""; // deprecated europa
            if (country == "cd") return "cg"; // congo and congo dr is one country
            if (country == "tw") return "cn"; // taiwan is china =)
            if (country == "n") return "gb"; // n.ireland is great britain
            if (country == "do") return "dm"; // dominica
            return country;

        }

        public static EventInfo [] ConvertBetsApiInfo(DateTime date, string input)
        {
            const SportType sport = SportType.Soccer;
            var ids = StorageCore.LoadEventsIndex(date, sport);
            if (ids == null) return null;
            var games = new List<EventInfo>();

            foreach (var id in ids)
            {
                var ev = StorageCore.LoadEvent(id, sport, date);
                if (ev == null) continue;
                if (ev.League.Name.Contains("Beach Soccer")) continue;
                if (ev.League.Name.Contains("Indoor Soccer")) continue;
                if (ev.League.Name.Contains("ESoccer Soccer")) continue;
                var info = new EventInfo();

                var i = info.LoadFromBetsApi(ev);
                if (i > 0) games.Add(info);
            }

            Console.WriteLine($"{date} : {games.Count} games");
            return games.ToArray();
        }

        public static List<Stats> GetStatsByMin(Event ev)
        {
            if (ev?.Statlines?.Attacks?.Home == null) return null;
            if (ev?.Statlines?.Attacks?.Away == null) return null;
            if (ev?.Statlines?.DangerousAttacks?.Home == null) return null;
            if (ev?.Statlines?.DangerousAttacks?.Away == null) return null;
            if (ev.Stats == null) return null;

            //var stats = new Stats();
            //stats.GetStatsFromEvent(ev);

            var s = new List<Stats>();
            for (var i = 0; i <= 44; ++i)
                s.Add(new Stats(i));


            #region Attacks
            s[0].A1 = 0;
            s[0].A2 = 0;

            var a = ev.Statlines.Attacks.Home.Where(t => t.TimeStr >= 0 & t.TimeStr < 45);
            if (a.Count() < 5) return null;
            a.ForEach(t =>
            {
                var line = s[t.TimeStr];
                line.A1 = t.Val;
                line.Active = true;
                if (t.UnixCreatedAt == 0) return;
                if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
            });

            a = ev.Statlines.Attacks.Away.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
            if (a.Count() < 5) return null;
            a.ForEach(t =>
            {
                var line = s[t.TimeStr];
                line.A2 = t.Val;
                line.Active = true;
                if (t.UnixCreatedAt == 0) return;
                if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
            });

            #endregion

            #region Dangerous Attacks

            s[0].DA1 = 0;
            s[0].DA2 = 0;

            a = ev.Statlines.DangerousAttacks.Home.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
            a.ForEach(t =>
            {
                var line = s[t.TimeStr];
                line.DA1 = t.Val;
                line.Active = true;
                if (t.UnixCreatedAt == 0) return;
                if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
            });

            a = ev.Statlines.DangerousAttacks.Away.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
            a.ForEach(t =>
            {
                var line = s[t.TimeStr];
                line.DA2 = t.Val;
                line.Active = true;
                if (t.UnixCreatedAt == 0) return;
                if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
            });


            #endregion

            #region Goals
            s[0].Score1 = 0;
            s[0].Score2 = 0;

            var p = ev?.PeriodScores?.Period1;
            var HTScore1 = p?.Home;
            var HTScore2 = p?.Away;

            if (HTScore1 == null | HTScore2 == null)
            {

            }
            else
            {
                //Score 1
                if (HTScore1 != 0)
                {
                    a = ev.Statlines?.Goals?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                    if (a != null)
                    {
                        a.ForEach(t =>
                        {
                            var line = s[t.TimeStr];
                            line.Score1 = t.Val;
                            line.Active = true;
                            if (t.UnixCreatedAt == 0) return;
                            if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                        });
                    }
                }

                //Score 2
                if (HTScore2 != 0)
                {
                    a = ev.Statlines?.Goals?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                    if (a != null)
                    {
                        a.ForEach(t =>
                        {
                            var line = s[t.TimeStr];
                            line.Score2 = t.Val;
                            line.Active = true;
                            if (t.UnixCreatedAt == 0) return;
                            if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                        });
                    }
                }
            }

            #endregion region

            #region OffTarget
            var of1 = ev.Statlines.OffTarget?.Home?.Length;
            var of2 = ev.Statlines.OffTarget?.Away?.Length;
            var nodata = (of1 == null || of1 == 0) & (of2 == null || of2 == 0);
            if (nodata)
            {

            }
            else
            {

                s[0].Off1 = 0;
                s[0].Off2 = 0;

                //OFF1
                if (of1 > 0)
                {

                    a = ev.Statlines?.OffTarget?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Off1 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }

                //OFF2
                if (of2 > 0)
                {
                    a = ev.Statlines.OffTarget?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Off2 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }

            }

            #endregion

            #region OnTarget

            var on1 = ev.Statlines.OnTarget?.Home?.Length;
            var on2 = ev.Statlines.OnTarget?.Away?.Length;
            nodata = (on1 == null || on1 == 0) & (on2 == null || on2 == 0);

            if (!nodata)
            {
                s[0].On1 = 0;
                s[0].On2 = 0;
                //On1
                if (on1 > 0)
                {

                    a = ev.Statlines.OnTarget?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.On1 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }

                //On2
                if (on2 > 0)
                {
                    a = ev.Statlines.OnTarget?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.On2 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }
            }

            #endregion

            #region Possession

            // Possession1
            a = ev.Statlines?.Possession?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
            if (a != null)
            {
                a.ForEach(t =>
                {
                    var line = s[t.TimeStr];
                    line.Pos1 = t.Val;
                    line.Active = true;
                    if (t.UnixCreatedAt == 0) return;
                    if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                });
            }

            a = ev.Statlines?.Possession?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
            if (a != null)
            {
                a.ForEach(t =>
                {
                    var line = s[t.TimeStr];
                    line.Pos2 = t.Val;
                    line.Active = true;
                    if (t.UnixCreatedAt == 0) return;
                    if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                });
            }

            #endregion

            #region YellowCards

            nodata =
                (ev?.Stats?.YellowCards?[0] != null && ev?.Stats?.YellowCards?[0] > 0 &&
                 (ev?.Statlines?.Yellowcards?.Home == null || ev.Statlines?.Yellowcards?.Home.Length == 0)) |
                (ev.Stats.YellowCards?[1] != null && ev.Stats.YellowCards?[1] > 0 &&
                 (ev?.Statlines?.Yellowcards?.Away == null || ev?.Statlines?.Yellowcards?.Away?.Length == 0));
            if (!nodata)
            {
                s[0].YC1 = 0;
                s[0].YC2 = 0;

                a = ev.Statlines.Yellowcards?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.YC1 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }


                a = ev.Statlines.Yellowcards?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.YC2 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }
            }

            #endregion

            #region RedCards

            nodata =
                (ev?.Stats?.Redcards?[0] != null && ev?.Stats?.Redcards?[0] > 0 &&
                 (ev?.Statlines?.Redcards?.Home == null || ev.Statlines?.Redcards?.Home.Length == 0)) |
                (ev.Stats.Redcards?[1] != null && ev.Stats.Redcards?[1] > 0 &&
                 (ev?.Statlines?.Redcards?.Away == null || ev?.Statlines?.Redcards?.Away?.Length == 0));
            if (!nodata)
            {
                s[0].RC1 = 0;
                a = ev.Statlines.Redcards?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.RC1 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }

                s[0].RC2 = 0;
                a = ev.Statlines.Redcards?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.RC2 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }
            }

            #endregion

            #region Subs

            nodata =
                (ev?.Stats?.Substitutions?[0] != null && ev?.Stats?.Substitutions?[0] > 0 &&
                 (ev?.Statlines?.Substitutions?.Home == null || ev.Statlines?.Substitutions?.Home.Length == 0)) |
                (ev.Stats.Substitutions?[1] != null && ev.Stats.Substitutions?[1] > 0 &&
                 (ev?.Statlines?.Substitutions?.Away == null || ev?.Statlines?.Substitutions?.Away?.Length == 0));
            if (!nodata)
            {
                s[0].Sub1 = 0;
                a = ev.Statlines.Substitutions?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Sub1 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }

                s[0].Sub2 = 0;
                a = ev.Statlines.Substitutions?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Sub2 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }
            }

            #endregion

            #region Penalty

            nodata = (ev?.Stats?.Penalties?[0] != null && ev?.Stats?.Penalties?[0] > 0 &&
                      (ev?.Statlines?.Penalties?.Home == null || ev?.Statlines?.Penalties?.Home?.Length == 0)) |
                     (ev?.Stats?.Penalties?[1] != null && ev?.Stats?.Penalties?[1] > 0 &&
                      (ev?.Statlines?.Penalties?.Away == null || ev?.Statlines?.Penalties?.Away?.Length == 0));
            if (!nodata)
            {
                s[0].Pen1 = 0;
                a = ev.Statlines.Penalties?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Pen1 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }

                s[0].Pen2 = 0;
                a = ev.Statlines.Penalties?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Pen2 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }
            }

            #endregion

            #region Corners

            nodata = (ev?.Stats?.Corners?[0] != null && ev?.Stats?.Corners?[0] > 0 && (ev?.Statlines?.Corners?.Home == null || ev.Statlines?.Corners?.Home.Length == 0)) |
                     (ev.Stats.Corners?[1] != null && ev.Stats.Corners?[1] > 0 && (ev?.Statlines?.Corners?.Away == null || ev?.Statlines?.Corners?.Away?.Length == 0));
            if (!nodata)
            {
                s[0].Cor1 = 0;
                a = ev.Statlines.Corners?.Home?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Cor1 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }

                s[0].Cor2 = 0;
                a = ev.Statlines.Corners?.Away?.Where(t => t.TimeStr > 0 & t.TimeStr < 45);
                if (a != null)
                {
                    a.ForEach(t =>
                    {
                        var line = s[t.TimeStr];
                        line.Cor2 = t.Val;
                        line.Active = true;
                        if (t.UnixCreatedAt == 0) return;
                        if (t.CreatedAt > line.StatTime) line.StatTime = t.CreatedAt;
                    });
                }
            }

            #endregion

            #region Fill Gaps and write

            for (var i = 1; i < s.Count; ++i)
            {
                var prev = s[i - 1];
                var line = s[i];
                if (line.Score1 == null) line.Score1 = prev.Score1;
                if (line.Score2 == null) line.Score2 = prev.Score2;
                if (line.A1 == null) line.A1 = prev.A1;
                if (line.A2 == null) line.A2 = prev.A2;
                if (line.DA1 == null) line.DA1 = prev.DA1;
                if (line.DA2 == null) line.DA2 = prev.DA2;
                if (line.Off1 == null) line.Off1 = prev.Off1;
                if (line.Off2 == null) line.Off2 = prev.Off2;
                if (line.On1 == null) line.On1 = prev.On1;
                if (line.On2 == null) line.On2 = prev.On2;
                if (line.YC1 == null) line.YC1 = prev.YC1;
                if (line.YC2 == null) line.YC2 = prev.YC2;
                if (line.RC1 == null) line.RC1 = prev.RC1;
                if (line.RC2 == null) line.RC2 = prev.RC2;
                if (line.Pen1 == null) line.Pen1 = prev.Pen1;
                if (line.Pen2 == null) line.Pen2 = prev.Pen2;
                if (line.Sub1 == null) line.Sub1 = prev.Sub1;
                if (line.Sub2 == null) line.Sub2 = prev.Sub2;
                if (line.Cor1 == null) line.Cor1 = prev.Cor1;
                if (line.Cor2 == null) line.Cor2 = prev.Cor2;

            }
            #endregion


            //возвращаем с первого active до последнего


            //return s.SkipWhile(t => !t.Active).ToList();
            return s;



        }

        #endregion

        #region marathon
        public static string GetCountryFromMarathon(string s)
        {
            s = s.ToLower();
            var ss = s.Split('.');
            var res = ss[0].Trim(' ');
            if (ss[0] == "women") res = ss[1].ToLower().Trim(' ');
            res = res.Replace("-", "").Replace("u20 ", "").Replace("u21 ", "").Replace("u19 ", "").Replace("u18 ", "")
                .Replace("u22 ", "").Replace("u23 ", "");

            if (s.StartsWith("north macedonia")) return "mk";
            if (s.StartsWith("netherlands")) return "nl";
            switch (res)
            {
                case "afc champions league":
                case "afc cup":
                case "aff championship":
                case "africa cup of nations":
                case "asia cup":
                case "asian cup":
                case "caf champions league":
                case "caf confederation cup":
                case "caf super cup":
                case "club world cup":
                case "concacaf champions league":
                case "concacaf championship":
                case "concacaf nations league":
                case "copa libertadores":
                case "copa libertadores u20":
                case "european championship":
                case "finalissima":
                case "friendlies":
                case "friendly tournaments":
                case "recopa sudamericana":
                case "saff championship":
                case "south american championship":
                case "southeast asian games":
                case "sudamericana cup":
                case "uefa champions league":
                case "uefa europa conference league":
                case "uefa europa league":
                case "uefa nations league":
                case "uefa youth league":
                case "world cup":
                case "caribbean club championship":
                case "caribbean club shield":
                case "waff championship":
                case "waff club championship":
                case "uefa euro":
                case "uefa super cup":
                case "ofc champions league":
                case "ofc nations cup":
                case "intercontinental cup u20":
                case "east asian championship":
                case "islamic solidarity games":
                case "cosafa cup":
                case "cosafa championship":
                case "cosafa champions league":
                case "copa america":
                case "concacaf league":
                case "campeones cup":
                case "cafa championship":
                case "bolivarian games":
                case "baltic league":
                case "arab cup u20":
                case "afc club championship":
                case "african nations championship":
                    return ("");
                case "albania":
                    return ("al");
                case "algeria":
                    return ("dz");
                case "andorra":
                    return ("ad");
                case "angola":
                    return ("ao");
                case "argentina":
                    return ("ar");
                case "armenia":
                    return ("am");
                case "aruba":
                    return ("aw");
                case "australia":
                    return ("au");
                case "austria":
                    return ("at");
                case "azerbaijan":
                    return ("az");
                case "bahrain":
                    return ("bh");
                case "bangladesh":
                    return ("bd");
                case "belarus":
                    return ("by");
                case "belgium":
                    return ("be");
                case "belize":
                    return ("bz");
                case "benin":
                    return ("bj");
                case "bhutan":
                    return ("bt");
                case "bolivia":
                    return ("bo");
                case "bosnia and herzegovina":
                    return ("ba");
                case "botswana":
                    return ("bw");
                case "brazil":
                    return ("br");
                case "bulgaria":
                    return ("bg");
                case "burkina faso":
                    return ("bf");
                case "burundi":
                    return ("bi");
                case "cambodia":
                    return ("kh");
                case "cameroon":
                    return ("cm");
                case "canada":
                    return ("ca");
                case "chile":
                    return ("cl");
                case "china pr":
                case "chinese taipei":
                    return ("cn");
                case "colombia":
                    return ("co");
                case "costa rica":
                    return ("cr");
                case "cote d'ivoire":
                    return ("ci");
                case "croatia":
                    return ("hr");
                case "cuba":
                    return ("cu");
                case "curacao":
                    return ("cw");
                case "cyprus":
                    return ("cy");
                case "czech republic":
                    return ("cz");
                case "denmark":
                    return ("dk");
                case "djibouti":
                    return ("dj");
                case "dominican republic":
                    return ("dm");
                case "ecuador":
                    return ("ec");
                case "egypt":
                    return ("eg");
                case "el salvador":
                    return ("sv");
                case "england":
                    return ("gb");
                case "estonia":
                    return ("ee");
                case "ethiopia":
                    return ("et");
                case "faroe islands":
                    return ("fo");
                case "fiji":
                    return ("fj");
                case "finland":
                    return ("fi");
                case "france":
                    return ("fr");
                case "gambia":
                    return ("gm");
                case "georgia":
                    return ("ge");
                case "germany":
                    return ("de");
                case "ghana":
                    return ("gh");
                case "gibraltar":
                    return ("gi");
                case "greece":
                    return ("gr");
                case "grenada":
                    return ("gd");
                case "guatemala":
                    return ("gt");
                case "honduras":
                    return ("hn");
                case "hong kong":
                    return ("hk");
                case "hungary":
                    return ("hu");
                case "iceland":
                    return ("is");
                case "india":
                    return ("in");
                case "indonesia":
                    return ("id");
                case "iran":
                    return ("ir");
                case "iraq":
                    return ("iq");
                case "israel":
                    return ("il");
                case "italy":
                    return ("it");
                case "jamaica":
                    return ("jm");
                case "japan":
                    return ("jp");
                case "jordan":
                    return ("jo");
                case "kazakhstan":
                    return ("kz");
                case "kenya":
                    return ("ke");
                case "korea republic":
                    return ("kr");
                case "kosovo":
                    return ("xk");
                case "kuwait":
                    return ("kw");
                case "kyrgyzstan":
                    return ("kg");
                case "laos":
                    return ("la");
                case "latvia":
                    return ("lv");
                case "lebanon":
                    return ("lb");
                case "liberia":
                    return ("lr");
                case "liechtenstein":
                    return ("li");
                case "lithuania":
                    return ("lt");
                case "luxembourg":
                    return ("lu");
                case "macau":
                    return ("mo");
                case "malawi":
                    return ("mw");
                case "malaysia":
                    return ("my");
                case "maldives":
                    return ("mv");
                case "mali":
                    return ("ml");
                case "malta":
                    return ("mt");
                case "mauritania":
                    return ("mr");
                case "mexico":
                    return ("mx");
                case "moldova":
                    return ("md");
                case "mongolia":
                    return ("mn");
                case "montenegro":
                    return ("me");
                case "morocco":
                    return ("ma");
                case "mozambique":
                    return ("mz");
                case "myanmar":
                    return ("mm");
                case "nepal":
                    return ("np");
                case "netherlands":
                    return ("nl");
                case "new zealand":
                    return ("nz");
                case "nicaragua":
                    return ("ni");
                case "niger":
                    return ("ne");
                case "nigeria":
                    return ("ng");
                case "north macedonia":
                    return ("mk");
                case "northern ireland":
                    return ("gb");
                case "norway":
                    return ("no");
                case "oman":
                    return ("om");
                case "palestine":
                    return ("ps");
                case "panama":
                    return ("pa");
                case "paraguay":
                    return ("py");
                case "peru":
                    return ("pe");
                case "philippines":
                    return ("ph");
                case "poland":
                    return ("pl");
                case "portugal":
                    return ("pt");
                case "puerto rico":
                    return ("pr");
                case "qatar":
                    return ("qa");
                case "republic of ireland":
                    return ("ie");
                case "romania":
                    return ("ro");
                case "russia":
                    return ("ru");
                case "rwanda":
                    return ("rw");
                case "saint kitts and nevis":
                    return ("kn");
                case "san marino":
                    return ("sm");
                case "saudi arabia":
                    return ("sa");
                case "scotland":
                    return ("gb");
                case "senegal":
                    return ("sn");
                case "serbia":
                    return ("rs");
                case "seychelles":
                    return ("sc");
                case "singapore":
                    return ("sg");
                case "slovakia":
                    return ("sk");
                case "slovenia":
                    return ("si");
                case "solomon islands":
                    return ("so");
                case "south africa":
                    return ("za");
                case "spain":
                    return ("es");
                case "sri lanka":
                    return ("lk");
                case "sweden":
                    return ("se");
                case "switzerland":
                    return ("ch");
                case "syria":
                    return ("sy");
                case "tajikistan":
                    return ("tj");
                case "tanzania":
                    return ("tz");
                case "thailand":
                    return ("th");
                case "togo":
                    return ("tg");
                case "tunisia":
                    return ("tn");
                case "turkey":
                    return ("tr");
                case "turkmenistan":
                    return ("tm");
                case "ukraine":
                    return ("ua");
                case "uae":
                    return ("ae");
                case "uganda":
                    return ("ug");
                case "uruguay":
                    return ("uy");
                case "usa":
                    return ("us");
                case "uzbekistan":
                    return ("uz");
                case "venezuela":
                    return ("ve");
                case "vietnam":
                    return ("vn");
                case "wales":
                    return ("gb");
                case "zambia":
                    return ("zm");
                case "zimbabwe":
                    return ("zw");
                case "vanuatu":
                    return ("vu");

               default:
                    return(res);


            }
        }

        public static EventInfo[] ConvertMarathonInfo(DateTime date, string input, bool isLive, bool addBegintime)
        {
            var sport = Juggernaut.Entity.Sport.Football;
            EventHistoryReader reader = new EventHistoryReader(input, new MarathonOddsProcessor());
            var index = reader.LoadEventIndex(sport, isLive, date);
            var games = new List<EventInfo>();

            foreach (var id in index)
            {
                if (id.League.Contains("eSports") || id.League.Contains("Liga Pro") || id.League.Contains("ESportsBattle") || id.League.Contains("League Pro") || id.League.Contains("FIFA 22."))
                    continue;
                var ev = reader.LoadEvent(id);
                if (ev == null)
                    continue;

                if (ev.StateHistory == null) continue;

                var info = new EventInfo();
                info.LoadFromMarathon(ev);
                // ДОБАВЛЯЕМ ВРЕМЯ НАЧАЛА МАТЧА
                if (addBegintime && info.Begin == null && ev.StateHistory != null)
                {
                   for (var i = 0; i < ev.StateHistory.Count; ++i)
                    {
                        var last = ev.StateHistory[i];
                        var C = new Coef();

                        C.Time = last.TimeCreate;
                        if (last.Info == "") continue;

                        var s = last.Info;
                        s = s.Replace(" ", "").Replace("\r", "").Replace(")", "").Replace("ET", "(")
                            .Replace("\n\n", "@");


                        var ss = s.Split('@');
                        if (ss.Length == 1) continue;
                        C.RawTime = ss[1];
                        if (C.RawTime != null && C.RawTime != "HT" && C.RawTime != "Break" && C.RawTime != "0:00")
                        {
                            C.Minute = int.Parse(C.RawTime.Split('+')[0].Split(':')[0]) + 1;
                            if (info.Begin == null && C.Minute != null)
                            {
                                info.Begin = C.Time.AddMinutes(-(double) C.Minute);
                                break;
                            }
                        }
                    }
                }
                // ДОБАВЛЯЕМ СЧЕТ
                if (isLive & info.Begin != null)
                {
                    for (var i = ev.StateHistory.Count - 1; i >= 0; i--)
                    {
                        var s = ev.StateHistory[i];
                        if (s.Info.Contains("ET"))
                            continue;
                        var scores = s.Info.Split(' ')[0].Split(':');
                        info.Result1 = int.Parse(scores[0]);
                        info.Result2 = int.Parse(scores[1]);
                        break;
                    }
                  
                }

                games.Add(info);


            }

            Console.WriteLine($"{date} : {games.Count} games");
            return games.ToArray();
        }

        public static string SimpifyLeague(string league)
        {
            league = league.Replace("Final Tournament", "");
            var s = league.Split('.');
            if (s.Length == 1) return s[0];
            var res = String.Join(".", s[0], s[1]);
            if (s.Length > 2 & (s[0] == "Women" | s[0] == "Australia" | s[0] == "Brazil")) res += "." + s[2];
            if (league.Contains("Final")) res += ".Finals";
            return res;
        }
        #endregion

        #region common


        /// <summary>
        /// Convert Unix time value to a DateTime object.
        /// </summary>
        /// <param name="unixtime">The Unix time stamp you want to convert to DateTime.</param>
        /// <returns>Returns a DateTime object that represents value of the Unix time.</returns>
        public static DateTime UnixTimeToDateTime(long unixtime, bool useMilliSeconds = true)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return useMilliSeconds ? dtDateTime.AddMilliseconds(unixtime).ToLocalTime() : dtDateTime.AddSeconds(unixtime).ToLocalTime();
        }
        public static string DateToString(DateTime date) => date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        public static string GetType(string home, string away, string league)
        {
            // return "w" if women; "u" if reserve or youth; "" otherwise

            foreach (var s in new[] {home.ToLower(), away.ToLower(), league.ToLower()})
            {
                if (s.Contains("women") | s.Contains("femine") | s.Contains("wom.") | s.Contains("(w)")
                    | s.Contains("feminin") | s.Contains("femrave") | s.Contains("nöi") | s.Contains("féminin") | s.Contains("femenin") |
                    s.Contains("frauen")) return ("w");
            }


            foreach (var s in new[]
                {home.ToLower().Replace("-", ""), away.ToLower().Replace("-", ""), league.ToLower().Replace("-", "")})
            {
                if (s.Contains("(r)") | s.Contains("reserve") | s.Contains("res.") |
                    s.Contains("u23") | s.Contains("u22") | s.Contains("u21") | s.Contains("u20") | s.Contains("u19") | s.Contains("u18") | s.Contains("u17") | s.Contains("u16") | s.Contains("u15") |
                    s.Contains("youth") | s.Contains("juvenil") | s.Contains("juniori") | s.Contains("juniorská")) return ("u");
            }

            return "";
        }

        public static EventInfo GetLevenstein(EventInfo game, IEnumerable<EventInfo> games, int diffMinuteTime, bool useScore=false)
        {
            if (game.Begin == null) return null;
            // вставка для фильтрации матчей
            if (useScore & game.Result1 != null & game.Result2 != null)
            {
                // дополнительно профильтруем только те игру у которых счет или пустой или такой же
                games = games.Where(t =>
                    (t.Result1 == null || t.Result1 == game.Result1) &
                    (t.Result2 == null || t.Result2 == game.Result2));
            }
            // ищем матч с минимальным расстоянием левенштейна. в окрестности +- diffminutetime 
            var events = games.Where(t =>
                t.Begin != null && Math.Abs(((DateTime) t.Begin - (DateTime) game.Begin).TotalMinutes) < diffMinuteTime &&
                t.Country == game.Country && t.Type == game.Type).ToArray();

            // не нашли так не нашли
            if (!events.Any())
                return null;

            var bEventsDistance = events.Select(s => new
            {
                Event = s,
                HomeDistance = LevenshteinHelper.GetLevenshteinDistance(s.CleanHome, game.CleanHome),
                AwayDistance = LevenshteinHelper.GetLevenshteinDistance(s.CleanAway, game.CleanAway),
            }).OrderBy(o => o.HomeDistance + o.AwayDistance).ToArray();

            var min = bEventsDistance.First().AwayDistance + bEventsDistance.First().HomeDistance;



            // проверяем равенство одной из сторон
            var res = events.Where(s => (s.CleanHome==game.CleanHome) | (s.CleanAway==game.CleanAway)).ToList();
            if (res.Count == 1)
                return res[0];



            // проверяем есть ли почти полное соответствие:
            res = bEventsDistance.Where(f => (game.Home.Length > 5 & f.HomeDistance <= 2 & f.Event.Home.Length > 5) | (game.Away.Length > 5 &f.AwayDistance <= 2 & f.Event.Away.Length > 5)).Select(t => t.Event).ToList();
            if (res.Count == 1)
                return (res[0]);
          

            // находим вхождение с двух сторон
            res = events.Where(s => (s.CleanHome.Contains(game.CleanHome) | game.CleanHome.Contains(s.CleanHome)) &
                                    (s.CleanAway.Contains(game.CleanAway) | game.CleanAway.Contains(s.CleanAway))).ToList();
            if (res.Count == 1)
                return res[0];

            //находим частичное вхождение
            res = events.Where(s => 
                      (s.CleanHome.Contains(game.CleanHome) && game.CleanHome.Length > 4) |
                      (s.CleanAway.Contains(game.CleanAway) && game.CleanAway.Length > 4) |
                      (game.CleanHome.Contains(s.CleanHome) && s.CleanHome.Length > 4) |
                      (game.CleanAway.Contains(s.CleanAway) && s.CleanAway.Length > 4) ).ToList();

            if (res.Count == 1)
                return res[0];


            // Общий левенштейн если ошибок 50%
            var treshold = 0.5;
            res = bEventsDistance.Where(f =>
                    (double)(f.HomeDistance + f.AwayDistance) / (double)(game.CleanHome.Length + game.CleanAway.Length) < treshold &&
                           f.HomeDistance + f.AwayDistance == min).Select(t => t.Event).ToList();
            if (res.Count == 1)
                return res[0];
            if (res.Count > 1)
                return null;

            return null;
        }

        public static EventInfo GetByNames(EventInfo game, IEnumerable<EventInfo> games, int diffMinuteTime, List<Dict> dictionary,bool useScore = false )
        {
            if (game.Begin == null) return null;
            // вставка для фильтрации матчей
            if (useScore & game.Result1 != null & game.Result2 != null)
            {
                // дополнительно профильтруем только те игру у которых счет или пустой или такой же
                games = games.Where(t =>
                    (t.Result1 == null || t.Result1 == game.Result1) &
                    (t.Result2 == null || t.Result2 == game.Result2)).ToList();
            }
            // ищем матч с минимальным расстоянием левенштейна. в окрестности +- diffminutetime 
            var events = games.Where(t =>
                t.Begin != null && Math.Abs(((DateTime)t.Begin - (DateTime)game.Begin).TotalMinutes) < diffMinuteTime &&
                t.Country == game.Country && t.Type == game.Type).ToArray();

            // не нашли так не нашли
            if (!events.Any())
                return null;

            // ищем такое же название хотябы одной из команд
            // проверяем равенство одной из сторон
            var res = events.Where(s => (s.CleanHome == game.CleanHome) | (s.CleanAway == game.CleanAway)).ToList();
            if (res.Count == 1)
                return res[0];


            // проверяем вхождение по словарю
            var d = dictionary.Find(t => t.Country == game.Country & t.Type == game.Type & t.Team1 == game.CleanHome);
            if (d != null) game.CleanHome = d.Team2;
            d = dictionary.Find(t => t.Country == game.Country & t.Type == game.Type & t.Team1 == game.CleanAway);
            if (d != null) game.CleanAway = d.Team2;
            res = events.Where(s => (s.CleanHome == game.CleanHome) | (s.CleanAway == game.CleanAway)).ToList();
            if (res.Count == 1)
                return res[0];
            


            // находим вхождение с двух сторон
            res = events.Where(s => (s.CleanHome.Contains(game.CleanHome) | game.CleanHome.Contains(s.CleanHome)) &
                                    (s.CleanAway.Contains(game.CleanAway) | game.CleanAway.Contains(s.CleanAway))).ToList();
            if (res.Count == 1)
                return res[0];

            // проверяем есть ли почти полное соответствие:
            var bEventsDistance = events.Select(s => new
            {
                Event = s,
                HomeDistance = LevenshteinHelper.GetLevenshteinDistance(s.CleanHome, game.CleanHome),
                AwayDistance = LevenshteinHelper.GetLevenshteinDistance(s.CleanAway, game.CleanAway),
            }).OrderBy(o => o.HomeDistance + o.AwayDistance).ToArray();

            var min = bEventsDistance.First().AwayDistance + bEventsDistance.First().HomeDistance;

            res = bEventsDistance.Where(f => (game.Home.Length > 5 & f.HomeDistance <= 2 & f.Event.Home.Length > 5) | (game.Away.Length > 5 & f.AwayDistance <= 2 & f.Event.Away.Length > 5)).Select(t => t.Event).ToList();
            if (res.Count == 1)
                return (res[0]);

            return null;

        }
        public static List<EventInfo> ReadInfo(DateTime date, string path)
        {
            var info = new List<EventInfo>();
            var index = Path.Combine(path, "index", Helper.DateToString(date) + ".csv");
            if (!File.Exists(index)) return (info);
            var s = File.ReadAllLines(index).Skip(1);
            foreach (var line in s) 
            {
                var i = new EventInfo();
                i.FromCSV(line);
                info.Add(i);
            }
            info.ForEach(g=>g.CleanTeams());
            return info;
        }
        public static EventInfo [] ReadInfoInterval(DateTime from, DateTime to, string path)
        {
            var result = new List<EventInfo>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var i = ReadInfo(date, path);
                result.AddRange(i);
            }

            return result.ToArray();
        }

        public static Coef[] ReadCoef(DateTime date, string path, string id)
        {
            var coefs = new List<Coef>();
            var f = Path.Combine(path, "Coef", Helper.DateToString(date), id + ".csv");
            if (!File.Exists(f))
            {
                // проверим предыдущий день!
                f = Path.Combine(path, "Coef", Helper.DateToString(date.AddDays(-1)), id + ".csv");
                if (!File.Exists(f))
                {
                    f = Path.Combine(path, "Coef", Helper.DateToString(date.AddDays(+1)), id + ".csv");
                    if (!File.Exists(f))
                        return null;
                }
            }

            var s = File.ReadAllLines(f).Skip(1);
            foreach (var line in s)
            {
                var c = new Coef();
                c.FromCSV(line);
                coefs.Add(c);
            }

            return coefs.ToArray();
        }

        #endregion

        #region fonbet
        public static string GetCountryFromFonbet(string s)
        {
            var country = "";
            var ss = s.ToLower().Split('.');

            for (int i = 0; i < ss.Length; ++i)
                ss[i] = ss[i].Trim(' ');

            string[] stop = { "wom", "u17", "u18", "u19", "u20", "u21", "u22", "u23", "up to 19 years old", "youth teams", "international tournament", "league 1" };

            if (ss.Length == 1)
                return ss[0];
            if (!stop.Contains(ss[1]))
                country = ss[1];
            else if (ss.Length > 2 && !stop.Contains(ss[2]))
                country = ss[2];
            else if (ss.Length > 3 && !stop.Contains(ss[3]))
                country = ss[3];
            switch (country)
            {
                case "afc champions league":
                case "afc cup":
                case "africa champions league":
                case "africa cup of nations":
                case "african nations championat":
                case "africa supercup":
                case "arab nations cup":
                case "asean championship":
                case "africa confederations cup":
                case "asia championship":
                case "asian championship":
                case "asian club championship":
                case "asian cup":
                case "azia cup":
                case "baltii cup":
                case "baltic cup":
                case "baltic league":
                case "bolivarian games":
                case "caf champions league":
                case "cafa championship":
                case "cafa cup":
                case "campeones cup":
                case "capixaba":
                case "cecafa champions liga":
                case "central asian championship":
                case "champions league caf":
                case "champions league ofc":
                case "concacaf league":
                case "copa america":
                case "copa intercontinental":
                case "cosafa":
                case "cosafa champions liga":
                case "cosafa cup":
                case "cfu club championship":
                case "champions league uefa":
                case "concacaf champions league":
                case "concacaf championship":
                case "concacaf nations league":
                case "conmebol championship":
                case "copa libertadores":
                case "copa sudamericana":
                case "european championship":
                case "finalissima":
                case "friendly":
                case "friendly matches":
                case "friendly games":
                case "friendly tournaments":
                case "friendly tournament":
                case "southeast asian games":
                case "caribbean club championship":
                case "caribbean club shield":
                case "toulon":
                case "u23 international friendlies":
                case "uefa europa conference league":
                case "uefa europe league":
                case "uefa nations league":
                case "wc-2022":
                case "world championship 2023":
                case "world cup 2022":
                case "youth league champions":
                case "youth league uefa":
                case "east asian championship":
                case "european university games":
                case "inter sud ladies cup":
                case "international champions cup":
                case "islamic solidarity games":
                case "ofc nations cup":
                case "premier league international cup":
                case "russian railways international cup":
                case "south asian championship":
                case "sud ladies cup":
                case "supercup uefa":
                case "uncaf championship":
                case "waff championship":
                case "wafu":
                case "wafu championship":
                case "women's european championship 2022":
                case "world championship":
                case "world cup":
                    return ("");
                case "albania":
                    return ("al");
                case "algeria":
                    return ("dz");
                case "andorra":
                    return ("ad");
                case "angola":
                    return ("ao");
                case "argentina":
                    return ("ar");
                case "armenia":
                    return ("am");
                case "aruba":
                case "championship": // WARNING!
                    return ("aw");
                case "australia":
                case "australia capital ff cup":
                case "queensland premier league 4 south coast":
                case "western australia":
                    return ("au");
                case "austria":
                    return ("at");
                case "azerbaijan":
                    return ("az");
                case "bahrain":
                    return ("bh");
                case "bangladesh":
                    return ("bd");
                case "belarus":
                    return ("by");
                case "belgium":
                    return ("be");
                case "belize":
                    return ("bz");
                case "benin":
                    return ("bj");
                case "bhutan":
                    return ("bt");
                case "bolivia":
                    return ("bo");
                case "bosnia and herzegovina":
                case "bosnia & herzeg":
                case "bosnia & herzegovina":
                case "bosnia and herzeg":
                case "bosnia and herzegov":
                    return ("ba");
                case "botswana":
                    return ("bw");
                case "brazil":
                case "u20 brazil":
                    return ("br");
                case "bulgaria":
                    return ("bg");
                case "burkina faso":
                    return ("bf");
                case "burundi":
                    return ("bi");
                case "cambodia":
                case "cambogia":
                    return ("kh");
                case "cameroon":
                    return ("cm");
                case "canada":
                    return ("ca");
                case "champioship san-marino":
                    return ("sm");
                case "chile":
                    return ("cl");
                case "china":
                case "taiwan":
                    return ("cn");
                case "colombia":
                    return ("co");
                case "costa rica":
                    return ("cr");
                case "cote d'ivoire":
                case "ivore coast":
                    return ("ci");
                case "croatia":
                    return ("hr");
                case "cuba":
                    return ("cu");
                case "curacao":
                    return ("cw");
                case "cyprus":
                    return ("cy");
                case "czech republic":
                case "czech":
                case "czech rep":
                    return ("cz");
                case "denmark":
                    return ("dk");
                case "djibouti":
                    return ("dj");
                case "dominica":
                case "dominicana":
                case "dominican republic":
                    return ("dm");
                case "ecuador":
                case "equador":
                    return ("ec");
                case "egypt":
                    return ("eg");
                case "el salvador":
                case "salvador":
                    return ("sv");
                case "england":
                case "spfl reserve league":
                    return ("gb");
                case "estonia":
                    return ("ee");
                case "ethiopia":
                    return ("et");
                case "faroe islands":
                    return ("fo");
                case "guyane francaise":
                    return ("gf");
                case "fiji":
                    return ("fj");
                case "finland":
                    return ("fi");
                case "france":
                    return ("fr");
                case "gambia":
                    return ("gm");
                case "georgia":
                    return ("ge");
                case "germany":
                    return ("de");
                case "ghana":
                    return ("gh");
                case "gibraltar":
                    return ("gi");
                case "greece":
                    return ("gr");
                case "grenada":
                    return ("gd");
                case "guatemala":
                    return ("gt");
                case "honduras":
                    return ("hn");
                case "hong kong":
                case "hong-kong":
                    return ("hk");
                case "hungary":
                    return ("hu");
                case "iceland":
                    return ("is");
                case "india":
                case "puttaiah memorial trophy super division": 
                    return ("in");
                case "indonesia":
                    return ("id");
                case "iran":
                    return ("ir");
                case "iraq":
                    return ("iq");
                case "israel":
                    return ("il");
                case "italy":
                    return ("it");
                case "jamaica":
                    return ("jm");
                case "japan":
                    return ("jp");
                case "jordan":
                    return ("jo");
                case "kazakhstan":
                case "kazahstan":
                    return ("kz");
                case "kenya":
                case "kenia":
                    return ("ke");
                case "korea republic":
                case "s":
                case "south korea":
                    return ("kr");
                case "kosovo":
                    return ("xk");
                case "kuwait":
                case "emir cup":
                    return ("kw");
                case "kyrgyzstan":
                    return ("kg");
                case "laos":
                    return ("la");
                case "latvia":
                    return ("lv");
                case "lebanon":
                    return ("lb");
                case "liberia":
                    return ("lr");
                case "liechtenstein":
                case "lichtenstein":
                    return ("li");
                case "lithuania":
                    return ("lt");
                case "luxembourg":
                case "luxemburg":
                    return ("lu");
                case "macau":
                case "macao":
                    return ("mo");
                case "macedonia":
                    return ("mk");
                case "malawi":
                    return ("mw");
                case "malaysia":
                    return ("my");
                case "maldives":
                    return ("mv");
                case "mali":
                    return ("ml");
                case "malta":
                    return ("mt");
                case "mauritania":
                    return ("mr");
                case "mexico":
                    return ("mx");
                case "moldova":
                    return ("md");
                case "mongolia":
                    return ("mn");
                case "montenegro":
                    return ("me");
                case "morocco":
                    return ("ma");
                case "mozambique":
                    return ("mz");
                case "myanmar":
                    return ("mm");
                case "nepal":
                    return ("np");
                case "netherlands":
                    return ("nl");
                case "new zealand":
                case "nea zealand":
                    return ("nz");
                case "nicaragua":
                case "nikaragua":
                    return ("ni");
                case "niger":
                    return ("ne");
                case "nigeria":
                    return ("ng");
                case "north macedonia":
                    return ("mk");
                case "northern ireland":
                case "north ireland":
                    return ("gb");
                case "norway":
                    return ("no");
                case "oman":
                    return ("om");
                case "palestine":
                case "palestina":
                    return ("ps");
                case "panama":
                    return ("pa");
                case "paraguay":
                    return ("py");
                case "peru":
                    return ("pe");
                case "philippines":
                    return ("ph");
                case "poland":
                    return ("pl");
                case "portugal":
                    return ("pt");
                case "puerto rico":
                    return ("pr");
                case "qatar":
                    return ("qa");
                case "republic of ireland":
                case "ireland":
                    return ("ie");
                case "romania":
                    return ("ro");
                case "russia":
                case "fnl 2":
                case "lfk":
                case "fonbet russia":
                case "fonbet russia cup":
                case "udmurtia cup":
                    return ("ru");
                case "rwanda":
                    return ("rw");
                case "saint kitts":
                    return ("kn");
                case "san marino":
                    return ("sm");
                case "saudi arabia":
                case "saud":
                    return ("sa");
                case "scotland":
                    return ("gb");
                case "senegal":
                    return ("sn");
                case "serbia":
                    return ("rs");
                case "seychelles":
                    return ("sc");
                case "singapore":
                    return ("sg");
                case "slovakia":
                    return ("sk");
                case "slovenia":
                    return ("si");
                case "solomon islands":
                    return ("so");
                case "south africa":
                case "rsa":
                    return ("za");
                case "spain":
                    return ("es");
                case "sri lanka":
                    return ("lk");
                case "sweden":
                    return ("se");
                case "switzerland":
                    return ("ch");
                case "syria":
                    return ("sy");
                case "tajikistan":
                    return ("tj");
                case "tanzania":
                    return ("tz");
                case "thailand":
                    return ("th");
                case "togo":
                    return ("tg");
                case "tunisia":
                case "tunis":
                    return ("tn");
                case "turkey":
                    return ("tr");
                case "turkmenistan":
                    return ("tm");
                case "trinidad & tob":
                    return ("tt");
                case "uae":
                    return ("ae");
                case "uganda":
                    return ("ug");
                case "uruguay":
                    return ("uy");
                case "usa":
                case "сша":
                    return ("us");
                case "uzbekistan":
                    return ("uz");
                case "vanuatu":
                    return ("vu");
                case "venezuela":
                    return ("ve");
                case "vietnam":
                    return ("vn");
                case "wales":
                    return ("gb");
                case "zambia":
                    return ("zm");
                case "zimbabwe":
                    return ("zw");
                case "ukraine":
                    return ("ua");
            }

            if (s.ToLower().Contains("north makedonia")) return "mk";
            if (s.ToLower().Contains("nederlands")) return "nl";
            if (s.ToLower().Contains("israel")) return "il";


            return country;
        }
        public static EventInfo [] ConvertFonbetInfo(DateTime date, string input, bool isLive)
        {
            var sport = Sport.Football;
            
            EventHistoryReader reader = new EventHistoryReader(input, new FonbetOddsProcessor());
            var index = reader.LoadEventIndex(sport, isLive, date);

            var games = new List<EventInfo>();

            foreach (var id in index)
            {
                if (id.League.Contains("eSports") || id.League.Contains("Liga Pro") ||
                    id.League.Contains("ESportsBattle") || id.League.Contains("League Pro") ||
                    id.League.Contains("FIFA 22.") || id.League.Contains("FIFA22.") || id.League.Contains("6x6") ||
                    id.League.Contains("3x3")|| id.League.Contains("6x6"))
                    continue;
                try
                {
                    var ev = reader.LoadEvent(id);
                    if (ev?.StateHistory == null) continue;

                    var info = new EventInfo();
                    info.LoadFromFonbet(ev);


                    // добавляем счет
                    if (isLive & info.Begin != null)
                    {
                        for (var i = ev.StateHistory.Count - 1; i >= 0; i--)
                        {
                            var s = ev.StateHistory[i];
                            if (s.Info.Contains("ET"))
                                continue;
                            if (!s.Info.Contains("END"))
                                continue;
                            var str = s.Info.Split('~')[0].Split('#');
                            info.Result1 = int.Parse(str[0].Split(';')[1]);
                            info.Result2 = int.Parse(str[1]);
                            break;
                        }

                    }

                    games.Add(info);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{date}: {id} error");
                }
            }

            Console.WriteLine($"{date} : {games.Count} games");
            return games.ToArray();
        }
        #endregion

        #region soccerway
        public static string GetCountryFromSoccerway(string s)
        {
            var country = s.ToLower().Replace(" - ","-");
            switch (country)
            {
                case "afghanistan": return ("af");
                case "africa": return ("");
                case "albania": return ("al");
                case "algeria": return ("dz");
                case "andorra": return ("ad");
                case "angola": return ("ao");
                case "antigua-and-barbuda": return ("ag");
                case "argentina": return ("ar");
                case "armenia": return ("am");
                case "aruba": return ("aw");
                case "asia": return ("");
                case "australia": return ("au");
                case "austria": return ("at");
                case "azerbaijan": return ("az");
                case "bahrain": return ("bh");
                case "bangladesh": return ("bd");
                case "barbados": return ("bb");
                case "belarus": return ("by");
                case "belgium": return ("be");
                case "belize": return ("bz");
                case "benin": return ("bj");
                case "bermuda": return ("bm");
                case "bhutan": return ("bt");
                case "bolivia": return ("bo");
                case "bosnia-herzegovina": return ("ba");
                case "botswana": return ("bw");
                case "brazil": return ("br");
                case "british-virgin-islands": return ("vi");
                case "brunei-darussalam": return ("bn");
                case "bulgaria": return ("bg");
                case "burkina-faso": return ("bf");
                case "burundi": return ("bi");
                case "cabo-verde": return ("cv");
                case "cambodia": return ("kh");
                case "cameroon": return ("cm");
                case "canada": return ("ca");
                case "cayman-islands": return ("cy");
                case "chad": return ("cc");
                case "chile": return ("cl");
                case "china-pr": return ("cn");
                case "chinese-taipei": return ("cn");
                case "colombia": return ("co");
                case "congo": return ("cg");
                case "congo-dr": return ("cg");
                case "cook-islands": return ("ck");
                case "costa-rica": return ("cr");
                case "cote-divoire": return ("ci");
                case "croatia": return ("hr");
                case "cuba": return ("cu");
                case "curacao": return ("cw");
                case "cyprus": return ("cy");
                case "czech-republic": return ("cz");
                case "denmark": return ("dk");
                case "djibouti": return ("dj");
                case "dominican-republic": return ("dm");
                case "ecuador": return ("ec");
                case "egypt": return ("eg");
                case "el-salvador": return ("sv");
                case "england": return ("gb");
                case "estonia": return ("ee");
                case "ethiopia": return ("et");
                case "europe": return ("");
                case "faroe-islands": return ("fo");
                case "fiji": return ("fj");
                case "finland": return ("fi");
                case "france": return ("fr");
                case "french-guyana": return ("gf");
                case "gabon": return ("ga");
                case "gambia": return ("gm");
                case "georgia": return ("ge");
                case "germany": return ("de");
                case "ghana": return ("gh");
                case "gibraltar": return ("gi");
                case "greece": return ("gr");
                case "grenada": return ("gd");
                case "guadeloupe": return ("gu");
                case "guam": return ("us");
                case "guatemala": return ("gt");
                case "guinea": return ("gn");
                case "guyana": return ("gy");
                case "haiti": return ("ht");
                case "honduras": return ("hn");
                case "hong-kong": return ("hk");
                case "hungary": return ("hu");
                case "iceland": return ("is");
                case "india": return ("in");
                case "indonesia": return ("id");
                case "iran": return ("ir");
                case "iraq": return ("iq");
                case "ireland-republic": return ("ie");
                case "israel": return ("il");
                case "italy": return ("it");
                case "jamaica": return ("jm");
                case "japan": return ("jp");
                case "jordan": return ("jo");
                case "kazakhstan": return ("kz");
                case "kenya": return ("ke");
                case "korea-republic": return ("kr");
                case "kosovo": return ("xk");
                case "kuwait": return ("kw");
                case "kyrgyzstan": return ("kg");
                case "laos": return ("la");
                case "latvia": return ("lv");
                case "lebanon": return ("lb");
                case "lesotho": return ("ls");
                case "liberia": return ("lr");
                case "libya": return ("ly");
                case "liechtenstein": return ("li");
                case "lithuania": return ("lt");
                case "luxembourg": return ("lu");
                case "macao": return ("mo");
                case "macedonia-fyr": return ("mk");
                case "madagascar": return ("mg");
                case "malawi": return ("mw");
                case "malaysia": return ("my");
                case "maldives": return ("mv");
                case "mali": return ("ml");
                case "malta": return ("mt");
                case "martinique": return ("fr");
                case "mauritania": return ("mu");
                case "mauritius": return ("mu");
                case "mexico": return ("mx");
                case "moldova": return ("md");
                case "mongolia": return ("mn");
                case "montenegro": return ("me");
                case "morocco": return ("ma");
                case "mozambique": return ("mz");
                case "myanmar": return ("mm");
                case "nc-america": return ("");
                case "nepal": return ("np");
                case "netherlands": return ("nl");
                case "new-caledonia": return ("fr");
                case "new-zealand": return ("nz");
                case "nicaragua": return ("ni");
                case "niger": return ("ne");
                case "nigeria": return ("ng");
                case "northern-ireland": return ("gb");
                case "norway": return ("no");
                case "oceania": return ("");
                case "oman": return ("om");
                case "pakistan": return ("pk");
                case "palestine": return ("ps");
                case "panama": return ("pa");
                case "papua-new-guinea": return ("pg");
                case "paraguay": return ("py");
                case "peru": return ("pe");
                case "philippines": return ("ph");
                case "poland": return ("pl");
                case "portugal": return ("pt");
                case "puerto-rico": return ("pr");
                case "qatar": return ("qa");
                case "reunion": return ("fr");
                case "romania": return ("ro");
                case "russia": return ("ru");
                case "rwanda": return ("rw");
                case "samoa": return ("ws");
                case "san-marino": return ("sm");
                case "sao-tome-e-principe": return ("");
                case "saudi-arabia": return ("sa");
                case "scotland": return ("gb");
                case "senegal": return ("sn");
                case "serbia": return ("rs");
                case "sierra-leone": return ("sl");
                case "singapore": return ("sg");
                case "slovakia": return ("sk");
                case "slovenia": return ("si");
                case "solomon-islands": return ("so");
                case "somalia": return ("ss");
                case "south-africa": return ("za");
                case "south-america": return ("");
                case "spain": return ("es");
                case "sri-lanka": return ("lk");
                case "st-kitts-and-nevis": return ("kn");
                case "sudan": return ("su");
                case "surinam": return ("sr");
                case "swaziland": return ("sz");
                case "sweden": return ("se");
                case "switzerland": return ("ch");
                case "syria": return ("sy");
                case "tahiti": return ("");
                case "tajikistan": return ("tj");
                case "tanzania": return ("tz");
                case "thailand": return ("th");
                case "togo": return ("tg");
                case "trinidad-and-tobago": return ("tt");
                case "tunisia": return ("tn");
                case "turkey": return ("tr");
                case "turkmenistan": return ("tm");
                case "turks-and-caicos-islands": return ("gb");
                case "uganda": return ("ug");
                case "ukraine": return ("ua");
                case "united-arab-emirates": return ("ae");
                case "united-states": return ("us");
                case "uruguay": return ("uy");
                case "uzbekistan": return ("uz");
                case "venezuela": return ("ve");
                case "vietnam": return ("vn");
                case "wales": return ("gb");
                case "world": return ("");
                case "yemen": return ("ye");
                case "zambia": return ("zm");
                case "zimbabwe": return ("zw");

            }

            return country;
        }

        public static EventInfo[] ConvertSoccerWayInfo(DateTime date, string input)
        {
            var games = new List<EventInfo>();

            var file = Path.Combine(input, Helper.DateToString(date) + ".csv");
            if (!File.Exists(file))
            {
                Console.WriteLine("NO DATA!");
                return games.ToArray();
                }

            var lines = File.ReadAllLines(file).Skip(1);
            foreach (var line in lines)
            {
                var game = new EventInfo();
                var i = game.LoadFromSoccerWay(line);
                if (i > 0) games.Add(game);
            }

            return games.ToArray();
        }
        #endregion


    }
}
