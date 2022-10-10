using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RootFinding;

namespace FootballLib
{
    public static class Probability
    {
        // функции для рассчета всякой всячены с кефами

        /// <summary>
        /// Margin - возвращает величину маржи букмекера
        /// </summary>
        /// <param name="odd1"></param>
        /// <param name="odd2"></param>
        /// <param name="odd3"></param>
        /// <returns></returns>
        public static double Margin(double odd1, double odd2, double? odd3 = null)
        {
            return  -1 + 1 / odd1 + 1 / odd2 + (1 / odd3 ?? 0.0);
        }

        /// <summary>
        /// ImpliedProbability - возвращает подразумеваемые вероятности из кефов
        /// </summary>
        /// <param name="odd1"></param>
        /// <param name="odd2"></param>
        /// <param name="odd3"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static (double Prob1, double Prob2, double? Prob3) ImpliedProbability(double odd1, double odd2, double? odd3, Method method)
        {
            var probs = (1 / odd1, 1 / odd2, 1 / odd3);
            switch (method)
            {
                case Method.None:
                    return probs;
                case Method.Additional:
                    var m = Margin(odd1, odd2, odd3);
                    return odd3 == null ? (probs.Item1 - m/2, probs.Item2 - m/2, null) : (probs.Item1 - m / 3, probs.Item2 - m / 3, probs.Item3 - m / 3);
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }
        }


        /// <summary>
        /// ImpliedProbability - возвращает подразумеваемые вероятности после вычета маржи
        /// </summary>
        /// <param name="odd1"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public static double ImpliedProbability(double odd1, double margin)
        {
            return 1 / odd1 - margin;
        }

        /// <summary>
        /// ImpliedOdds -  обратная функция добавляет к вероятностям маржу(0 если не надо) и возвращает итоговые кефы
        /// </summary>
        /// <param name="prob1"></param>
        /// <param name="prob2"></param>
        /// <param name="prob3"></param>
        /// <param name="margin"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static (double Odd1, double Odd2, double? Odd3) ImpliedOdds(double prob1, double prob2, double? prob3, double margin, Method method)
        {
            var odds = (1 / prob1, 1 / prob2, 1 / prob3);
            if (Math.Abs(margin) < 0.000001) return odds;

            switch (method)
            {
                case Method.None:
                    return odds;
                case Method.Additional:
                    return prob3 == null ? (1 / (prob1 + margin / 2), 1 / (prob2 + margin / 2), null) : (1 / (prob1 + margin / 3), 1 / (prob2 + margin / 3), 1 / (prob3 + margin / 3));
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }
        }


        #region convert functions

        /// <summary>
        /// возвращает предполагаемые вероятности для F1(0),F2(0)
        /// </summary>
        /// <param name="W1"></param>
        /// <param name="X"></param>
        /// <param name="W2"></param>
        /// <returns></returns>
        public static (double F1, double F2) DNBLine(double W1, double X, double W2)
        {
            var F1 = W1 / (1 - X);
            var F2 = W2 / (1 - X);
           
            return (F1,F2);
        }
        
        
        // возвращает предполагаемые вероятности для F1(0.5),F2(-0.5)
        public static (double Prob1, double Prob2) F1_half(double W1, double X, double W2)
        {
            
            var F1 = 1 - W2;
            var F2 = W2;
            return (F1, F2);
        }

        /// <summary>
        /// возвращает предполагаемые вероятности для F2(0.5),F1(-0.5)
        /// </summary>
        /// <param name="W1"></param>
        /// <param name="X"></param>
        /// <param name="W2"></param>
        /// <returns></returns>
        public static (double Prob1, double Prob2) F2_half(double W1, double X, double W2)
        {
           
            var F1 = 1 - W1;
            var F2 = W1;
            return (F1, F2);
        }

        /// <summary>
        /// возвращает предполагаемые вероятности F(X) если известно F(X-0.5), F(X+0.5)) 
        /// F1less, F1more = предполагаемые вероятности; обратная сделка F2(-X) = 1 - F
        /// </summary>
        /// <param name="Fless"></param>
        /// <param name="Fmore"></param>
        /// <returns></returns>
        public static double F_integer_prob(double Fless, double Fmore)
        {
            return Fless / (1 - Fmore + Fless);
        }


        /// <summary>
        /// возвращает предполагаемые вероятности TO(X) если известно TO(X-0.5), TO(X+0.5)) 
        /// TOless, TOmore = предполагаемые вероятности
        /// </summary>
        /// <param name="TOless"></param>
        /// <param name="TOmore"></param>
        /// <returns></returns>
        public static double TO_integer_prob(double TOless, double TOmore)
        {
            return TOmore / (1 - TOless + TOmore);
        }

        /// <summary>
        /// возвращает предполагаемые вероятности TO(X) если известно TO(X-0.5), TO(X+0.5)) 
        /// TOless, TOmore = предполагаемые вероятности
        /// </summary>
        /// <param name="TOless"></param>
        /// <param name="TOmore"></param>
        /// <returns></returns>
        public static double TU_integer_prob(double TUless, double TUmore)
        {
            return TUless / (1 - TUmore + TUless);
        }


        /// <summary>
        /// возвращает предполагаемые вероятности TO(X) если известно TO(X-0.5), TO(X-1)) 
        /// ex: ищем TO3.5  TOminus_half = TO3, TOminusOne = TO2.5
        /// </summary>
        /// <param name="TOminus_half"></param>
        /// <param name="TOless_one"></param>
        /// <returns></returns>
        public static double TO_right_prob(double TOminus_half, double TOless_one)
        {
            return TOminus_half *  (1 - TOless_one) / (1 - TOminus_half);
        }

        /// <summary>
        /// возвращает предполагаемые вероятности TO(X) если известно TO(X+0.5), TO(X+1)) 
        /// ex: ищем TO1.5  TOplus_half = TO2, TOplus_one = TO2.5
        /// </summary>
        /// <param name="TOplus_half"></param>
        /// <param name="TOplus_one"></param>
        /// <returns></returns>
        public static double TO_left_prob(double TOplus_half, double TOplus_one)
        {
            return 1 + TOplus_one - TOplus_one/TOplus_half;
        }


        #endregion

        /// <summary>
        /// передаем страйк на тоталы (нецелый!) и ожидаемое число забитых в будущем голов, текущую сумму голов
        /// возращает предполагаемую вероятность 
        /// </summary>
        /// <param name="strike"></param>
        /// <param name="expected_goals"></param>
        /// <param name="current_goals"></param>
        /// <returns></returns>
        public static (double, double) GetNonIntegerTotalsFromExpectedGoals(double strike, double expected_goals, int current_goals)
        {
            var k = Math.Floor(strike) - current_goals;
            var p = Poisson.CDF(expected_goals, k);
            return (p, 1 - p);
        }

        public static (double, double) GetIntegerTotalsFromExpectedGoals(int strike, double expected_goals, int current_goals)
        {
            var k = strike - current_goals;
            var p = Poisson.CDF(expected_goals, k);
            var pp = Poisson.PMF(expected_goals, k);
            return ((p - pp ) / (1 - pp), (1 - p) / (1 - pp));
        }

        /// <summary>
        /// работает только для нецелых strike!
        /// возращаем сколько еще забьется голов в среднем!
        /// </summary>
        /// <param name="TotalUnderImpliedProbability"></param>
        /// <param name="strike"></param>
        /// <param name="currentgoals"></param>
        /// <returns></returns>
        public static double GetExpectedGoalsFromTotal(double TotalUnderImpliedProbability, double strike, int currentgoals)
        {

            double s = Math.Floor(strike) - currentgoals;
            Func<double, double> f = x => TotalUnderImpliedProbability - Poisson.CDF(x, s);
            var res = Bisection.FindRoot(f, 1e-4, 10, maxIterations:1000);
            return res;
        }


    }

    public enum Method {None,Additional}
}
