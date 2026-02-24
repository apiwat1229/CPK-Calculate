using System;
using System.Collections.Generic;
using System.Linq;

namespace CPK_Calculate
{
    // คลาสข้อมูลสำหรับประวัติ (History) - ต้องเป็น public
    public class HistoryItem
    {
        public string PartName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
    }

    public class CPKResults
    {
        // Within Stats
        public double StdevWithin { get; set; } = 0;
        public double Cp { get; set; } = 0;
        public double Cpk { get; set; } = 0;
        public double PpmWithin { get; set; } = 0;

        // Overall Stats
        public double StdevOverall { get; set; } = 0;
        public double Pp { get; set; } = 0;
        public double Ppk { get; set; } = 0;
        public double Cpm { get; set; } = 0;
        public double PpmOverall { get; set; } = 0;

        public double Mean { get; set; } = 0;
    }

    public static class CPKEngine
    {
        public static CPKResults Calculate(List<double> data, double lsl, double usl, int subgroupSize)
        {
            if (data == null || data.Count < 2) return new CPKResults();

            double mean = data.Average();
            double target = (usl + lsl) / 2.0;

            // 1. Overall Statistics
            double sumSqOverall = data.Sum(x => Math.Pow(x - mean, 2));
            double stdevOverall = Math.Sqrt(sumSqOverall / (data.Count - 1));

            // 2. Within Statistics (Pooled Standard Deviation)
            double sumSqWithin = 0;
            int dfWithin = 0;
            for (int i = 0; i < data.Count; i += subgroupSize)
            {
                var group = data.Skip(i).Take(subgroupSize).ToList();
                if (group.Count > 1)
                {
                    double gMean = group.Average();
                    sumSqWithin += group.Sum(x => Math.Pow(x - gMean, 2));
                    dfWithin += (group.Count - 1);
                }
            }

            double sPooled = dfWithin > 0 ? Math.Sqrt(sumSqWithin / dfWithin) : stdevOverall;
            double c4 = GetC4(dfWithin);
            double stdevWithin = sPooled / c4;

            return new CPKResults
            {
                Mean = mean,
                StdevWithin = stdevWithin,
                Cp = (usl - lsl) / (6 * stdevWithin),
                Cpk = Math.Min((usl - mean) / (3 * stdevWithin), (mean - lsl) / (3 * stdevWithin)),
                PpmWithin = CalculatePPM(mean, stdevWithin, lsl, usl),
                StdevOverall = stdevOverall,
                Pp = (usl - lsl) / (6 * stdevOverall),
                Ppk = Math.Min((usl - mean) / (3 * stdevOverall), (mean - lsl) / (3 * stdevOverall)),
                Cpm = ((usl - lsl) / (6 * stdevWithin)) / Math.Sqrt(1 + Math.Pow((mean - target) / stdevWithin, 2)),
                PpmOverall = CalculatePPM(mean, stdevOverall, lsl, usl)
            };
        }

        private static double CalculatePPM(double mean, double std, double lsl, double usl)
        {
            double zUpper = (usl - mean) / std;
            double zLower = (mean - lsl) / std;
            return ((1.0 - NormalCDF(zUpper)) + (1.0 - NormalCDF(zLower))) * 1000000.0;
        }

        private static double NormalCDF(double z)
        {
            z = Math.Abs(z);
            double t = 1.0 / (1.0 + 0.2316419 * z);
            double d = 0.3989422804 * Math.Exp(-z * z / 2.0);
            double p = d * t * (0.31938153 + t * (-0.356563782 + t * (1.781477937 + t * (-1.821255978 + t * 1.330274429))));
            return 1.0 - p;
        }

        private static double GetC4(int df)
        {
            if (df <= 0) return 1.0;
            return Math.Sqrt(2.0 / df) * (Math.Exp(LogGamma((df + 1.0) / 2.0) - LogGamma(df / 2.0)));
        }

        private static double LogGamma(double x)
        {
            double[] coef = { 76.18009172947146, -86.50532032941677, 24.01409824083091, -1.231739572450155, 0.001208650973866179, -5.395239384953e-6 };
            double tmp = x + 5.5; tmp -= (x + 0.5) * Math.Log(tmp);
            double ser = 1.000000000190015;
            for (int j = 0; j < 6; j++) ser += coef[j] / (x + j + 1.0);
            return Math.Log(2.5066282746310005 * ser / x) - tmp;
        }
    }
}