using System.Text;
using System.Text.RegularExpressions;

namespace FootballLib
{
    public static class StringExtention
    {
        public static string Clean(this string name)
        {
            name = name.ToLower();
            name = Regex.Replace(name, @"\p{C}+", string.Empty);
            StringBuilder sb = new StringBuilder(name);
            sb.Replace("u-15", "");
            sb.Replace("u-16", "");
            sb.Replace("u-17", "");
            sb.Replace("u-18", "");
            sb.Replace("u-19", "");
            sb.Replace("u-20", "");
            sb.Replace("u-21", "");
            sb.Replace("u-22", "");
            sb.Replace("u-23", "");
            sb.Replace("-", " ");
            sb.Replace("(w)", "");
            sb.Replace("(r)", "");
            sb.Replace(" ii", " 2");
            sb.Replace("women", "");
            sb.Replace("reserves", "");
            sb.Replace("res.", "");
            sb.Replace("youth", "");
            sb.Replace("deportivo ", "");
            sb.Replace("res.", "");
            sb.Replace(".", "");
            sb.Replace("'", "");
            sb.Replace("(", "");
            sb.Replace(")", "");

            var ss = sb.ToString().Split(' ');
            for (int i = 0; i < ss.Length; i++)
            {
                string s = ss[i];
                if (s == "fc" | s == "fk" | s == "sc" | s == "afc" | s == "al" | s == "cd" | s == "cf" | s == "al"|
                    s == "u15" |  s == "u16"| s == "u17" | s == "u18" | s == "u19" | s == "u20" | s == "u21" | s == "u22" | s == "u23")
                    ss[i] = "";

                if (s == "utd") 
                    ss[i] = "united";
            }

            return string.Join(" ", ss).Trim(' ');
        }
    }
}