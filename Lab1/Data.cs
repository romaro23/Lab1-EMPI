using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Distributions;
using ScottPlot.MultiplotLayouts;
using ScottPlot.Plottables;

namespace Lab1
{
    public class Data
    {
        public static Tuple<List<double>, List<double>> Data_ { get; set; } = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
        public static List<DataView> DataView_ { get; set; } = new List<DataView>();
        public static List<StatsView> XStatsView { get; set; } = new List<StatsView>();
        public static List<StatsView> YStatsView { get; set; } = new List<StatsView>();
        public static List<CriteriaView> CriteriaViews { get; set; } = new List<CriteriaView>();
        public static Stats Stats_ { get; set; } = new Stats();

        public class DataView
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class StatsView
        {
            public string Name { get; set; }
            public double Estimate { get; set; }
            public double Deviation { get; set; }
            public double LeftInterval { get; set; }
            public double RightInterval { get; set; }
        }
        public class CriteriaView
        {
            public string? Name { get; set; }
            public double Estimate { get; set; }
            public double LeftInterval { get; set; }
            public double RightInterval { get; set; }
            public double Statistics { get; set; }
            public double Quantile { get; set; }
            public string? Conclusion { get; set; }
            public string? RelationConclusion { get; set; }

        }

        public class Stats
        {
            public int N;
            public double Mean, Med, S_, S, A_, A, E_, E, MeanDeviation, S_Deviation, A_Deviation, E_Deviation, T, MeanLow, MeanHigh, MedLow, MedHigh, S_Low, S_High, A_Low, A_High, E_Low, E_High;
            public Stats XStats { get; set; }
            public Stats YStats { get; set; }

            public void InitializeStats()
            {
                XStats = new Stats();
                YStats = new Stats();
            }

            public void CalculateStats(List<double> data)
            {
                N = data.Count;
                Mean = data.Sum() / data.Count();
                var sorted = data.OrderBy(x => x).ToList();
                if (N % 2 == 0)
                {
                    Med = (sorted[N / 2 - 1] + sorted[N / 2]) / 2.0;
                }
                else
                {
                    Med = sorted[N / 2];
                }
                List<double> Sum2 = new List<double>();
                List<double> Sum3 = new List<double>();
                List<double> Sum4 = new List<double>();
                for (int i = 0; i < data.Count; i++)
                {
                    var x = data[i] - Mean;
                    Sum2.Add(Math.Pow(x, 2));
                    Sum3.Add(Math.Pow(x, 3));
                    Sum4.Add(Math.Pow(x, 4));
                }
                S = Math.Sqrt(Sum2.Sum() / N);
                A = Sum3.Sum() / (N * Math.Pow(S, 3));
                E = Sum4.Sum() / (N * Math.Pow(S, 4)) - 3;
                S_ = Math.Sqrt(Sum2.Sum() / (N - 1));
                A_ = (Math.Sqrt(N * (N - 1)) / (N - 2)) * A;
                E_ = (Math.Pow(N, 2) - 1) / ((N - 2) * (N - 3)) * (E + (6.0 / (N + 1)));
                MeanDeviation = S_ / Math.Sqrt(N);
                S_Deviation = S_ / Math.Sqrt(2 * N);
                A_Deviation = Math.Sqrt((double)(6 * N * (N - 1)) / ((N - 2) * (N + 1) * (N + 3)));
                double firstPart = (N - 3) * (N - 2);
                double secondPart = (N + 3) * (N + 5);
                double result = firstPart * secondPart;
                E_Deviation = Math.Sqrt(24 * N * Math.Pow(N - 1, 2) / result);
                T = GetStudentQuantile(N, 0.05, 1);
                MeanLow = Mean - T * MeanDeviation;
                MeanHigh = Mean + T * MeanDeviation;
                S_Low = S_ - T * S_Deviation;
                S_High = S_ + T * S_Deviation;
                A_Low = A_ - T * A_Deviation;
                A_High = A_ + T * A_Deviation;
                E_Low = E_ - T * E_Deviation;
                E_High = E_ + T * E_Deviation;

                int J = (int)(Math.Round((double)N / 2 - 1.96 * (Math.Sqrt(N) / 2)));
                int K = (int)(Math.Round((double)N / 2 + 1 + 1.96 * (Math.Sqrt(N) / 2)));
                var sortedData = data.OrderBy(x => x).ToList();
                MedLow = sortedData[J - 1];
                MedHigh = sortedData[K - 1];

            }

            public CriteriaView CalculatePearsonCriteria(Tuple<List<double>, List<double>> data)
            {
                var n = XStats.N;
                var xs = data.Item1;
                var ys = data.Item2;

                var multiplication = xs.Zip(ys, (x, y) => x * y).ToList();
                var multiplicationAverage = multiplication.Average();
                double pearson = (multiplicationAverage - XStats.Mean * YStats.Mean) / (XStats.S * YStats.S);
                double t = (pearson * Math.Sqrt(n - 2)) / Math.Sqrt(1 - Math.Pow(pearson, 2));
                double absT = Math.Abs(t);
                double student = GetStudentQuantile(n, 0.05, 2);
                double low = pearson + (pearson * (1 - Math.Pow(pearson, 2)) / (2 * n)) -
                             1.96 * ((1 - Math.Pow(pearson, 2)) / Math.Sqrt(n - 1));
                double high = pearson + (pearson * (1 - Math.Pow(pearson, 2)) / (2 * n)) + 
                              1.96 * ((1 - Math.Pow(pearson, 2)) / Math.Sqrt(n - 1));
                string conclusion;
                string relationConclusion;
                conclusion = "Не значущий";
                relationConclusion = "Лінійного зв'язку немає";
                if (absT > student)
                {
                    conclusion = "Значущий";
                    relationConclusion = "Є лінійний зв'язок";
                }
                return (new CriteriaView
                {
                    Name = "Пірсона",
                    Estimate = pearson,
                    LeftInterval = low,
                    RightInterval = high,
                    Statistics = absT,
                    Quantile = student,
                    Conclusion = conclusion,
                    RelationConclusion = relationConclusion
                });
                
                
            }

            public CriteriaView CalculateKendallCriteria(Tuple<List<double>, List<double>> data)
            {
                var xsRanks = CalculateRanks(data.Item1);
                var ysRanks = CalculateRanks(data.Item2);
                var ysRanksUnsorted = data.Item2
                    .Select(ys => ysRanks.First(y => y.Item1 == ys).Item2)
                    .ToList();
                var xsRanksUnsorted = data.Item1
                    .Select(xs => xsRanks.First(x => x.Item1 == xs).Item2)
                    .ToList();
                var combRanks = xsRanksUnsorted.Zip(ysRanksUnsorted, (x, y) => new Tuple<double, double>(x, y))
                    .OrderBy(pair => pair.Item1)
                    .ToList();
                double v = 0;
                for(int j = 0; j < combRanks.Count; j++)
                {
                    for (int i = j + 1; i < combRanks.Count; i++)
                    {
                        if (combRanks[j].Item2 > combRanks[i].Item2) v -= 1;
                        else if (combRanks[j].Item2 < combRanks[i].Item2) v += 1;
                    }
                }
                int n = data.Item1.Count;
                double kendall;
                if (xsRanksUnsorted.GroupBy(x => x).Any(y => y.Count() > 1) ||
                    ysRanksUnsorted.GroupBy(x => x).Any(y => y.Count() > 1))
                {
                    double c = 0, d = 0;
                    var z = xsRanksUnsorted.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
                    double sumC = 0, sumD = 0;
                    foreach (var group in z)
                    {
                        sumC += group.Count() * (group.Count() - 1);
                    }

                    c = 0.5 * sumC;

                    var p = ysRanksUnsorted.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
                    foreach (var group in p)
                    {
                        sumD += group.Count() * (group.Count() - 1);
                    }
                    d = 0.5 * sumD;
                    kendall = v / Math.Sqrt((0.5 * n * (n - 1) - c) * (0.5 * n * (n - 1) - d));
                }
                else
                {
                    kendall = 2 * v / (n * (n - 1));
                }
                var u = 3 * kendall * Math.Sqrt(n * (n - 1)) / Math.Sqrt(2 * (2 * n + 5));
                var absU = Math.Abs(u);
                var normal = MathNet.Numerics.Distributions.Normal.InvCDF(0, 1, 1 - 0.05 / 2);
                string conclusion;
                string relationConclusion;
                conclusion = "Не значущий";
                relationConclusion = "Монотонного зв'язку немає";
                if (absU > normal)
                {
                    conclusion = "Значущий";
                    relationConclusion = "Є монотонний зв'язок";
                }
                return (new CriteriaView
                {
                    Name = "Кендалла",
                    Estimate = kendall,
                    LeftInterval = 0,
                    RightInterval = 0,
                    Statistics = absU,
                    Quantile = normal,
                    Conclusion = conclusion,
                    RelationConclusion = relationConclusion
                });
            }

            public CriteriaView CalculateCorrelationRatio(Tuple<List<double>, List<double>> data)
            {
                int k = (int)(1 + 1.44 * Math.Log(data.Item1.Count));
                double h = (data.Item1.Max() - data.Item1.Min()) / k;
                int l = 1;
                double g = 0;
                List<Tuple<double, double>> classes = new List<Tuple<double, double>>();
                for (int i = l; i <= k + 1; i++)
                {
                    var temp = g;
                    g = data.Item1.Min() + (i - 1) * h;
                    if (i > 1)
                    {
                        classes.Add(new Tuple<double, double>(temp, g));
                    }
                }
                Dictionary<double, List<double>> newData = new Dictionary<double, List<double>>();
                List<double> ys = new List<double>();
                for (int i = 0; i < classes.Count(); i++)
                {
                    var x = 0.5 * (classes[i].Item1 + classes[i].Item2);
                    foreach (var pair in data.Item1.Zip(data.Item2))
                    {
                        if (i + 1 == classes.Count())
                        {
                            if (pair.First >= classes[i].Item1 && pair.First <= classes[i].Item2)
                            {
                                ys.Add(pair.Second);
                            }
                        }
                        else
                        {
                            if (pair.First >= classes[i].Item1 && pair.First < classes[i].Item2)
                            {
                                ys.Add(pair.Second);
                            }
                        }
                        
                    }
                    newData.Add(x, new List<double>(ys));
                    ys.Clear();
                }

                double n = data.Item2.Count;
                var yAvg = data.Item2.Sum() / n;
                double groupVariance = 0;
                foreach (var list in newData)
                {
                    var diff = list.Value.Average() - yAvg;
                    groupVariance += Math.Pow(diff, 2) * list.Value.Count;
                }

                groupVariance = groupVariance / n;
                double variance = 0;
                foreach (var y in data.Item2)
                {
                    var diff = y - yAvg;
                    variance += Math.Pow(diff, 2);
                }
                variance = variance / n;
                
                var correlationRatio = Math.Sqrt(groupVariance / variance);
                var f = (correlationRatio / (k - 1)) / ((1 - correlationRatio) / (n - k));
                var fisher = GetFisherQuantile(k - 1, n, 0.05);
                string conclusion;
                string relationConclusion;
                conclusion = "Не значущий";
                relationConclusion = "Стохастичного зв'язку немає";
                if (f > fisher)
                {
                    conclusion = "Значущий";
                    relationConclusion = "Є стохастичний зв'язок";
                    
                }
                return new CriteriaView
                {
                    Name = "Кореляційне відношення",
                    Estimate = correlationRatio,
                    LeftInterval = 0,
                    RightInterval = 0,
                    Statistics = f,
                    Quantile = fisher,
                    Conclusion = conclusion,
                    RelationConclusion = relationConclusion
                };
                Console.ReadLine();
            }
            public CriteriaView CalculateSpearmanCriteria(Tuple<List<double>, List<double>> data)
            {
                var xsRanks = CalculateRanks(data.Item1);
                var ysRanks = CalculateRanks(data.Item2);
                var ysRanksUnsorted = data.Item2
                    .Select(ys => ysRanks.First(y => y.Item1 == ys).Item2)
                    .ToList();
                var xsRanksUnsorted = data.Item1
                    .Select(xs => xsRanks.First(x => x.Item1 == xs).Item2)
                    .ToList();
                var combRanks = xsRanksUnsorted.Zip(ysRanksUnsorted, (x, y) => new Tuple<double, double>(x, y))
                    .OrderBy(pair => pair.Item1)
                    .ToList();
                var diffRanks = combRanks.Select(pair => pair.Item1 - pair.Item2);
                var diffRanksSquared = diffRanks.Select(x => Math.Pow(x, 2));
                var diffSquaredSum = diffRanksSquared.Sum();
                int n = data.Item1.Count;
                double spearman;
                if (xsRanksUnsorted.GroupBy(x => x).Any(y => y.Count() > 1) || ysRanksUnsorted.GroupBy(x => x).Any(y => y.Count() > 1))
                {
                    double a = 0, b = 0;
                    var z = xsRanksUnsorted.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
                    double sumA = 0, sumB = 0;
                    foreach (var group in z)
                    {
                        sumA += Math.Pow(group.Count(), 3) - group.Count();
                    }

                    a = 1.0 / 12 * sumA;

                    var p = ysRanksUnsorted.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
                    foreach (var group in p)
                    {
                        sumB += Math.Pow(group.Count(), 3) - group.Count();
                    }
                    b = 1.0 / 12 * sumB;

                    spearman = (1.0 / 6 * n * (Math.Pow(n, 2) - 1) - diffSquaredSum - a - b) / (Math.Sqrt((1.0 / 6 * n * (Math.Pow(n, 2) - 1) - 2 * a) * (1.0 / 6 * n * (Math.Pow(n, 2) - 1) - 2 * b)));
                }
                else
                {
                    spearman = 1 - (6 / (n * (Math.Pow(n, 2) - 1))) * diffSquaredSum; 
                }
                var t = spearman * Math.Sqrt(n - 2) / (Math.Sqrt(1 - Math.Pow(spearman, 2)));
                var absT = Math.Abs(t);
                var student = GetStudentQuantile(n, 0.05, 2);
                string conclusion;
                string relationConclusion;
                conclusion = "Не значущий";
                relationConclusion = "Монотонного зв'язку немає";
                if (absT > student)
                {
                    conclusion = "Значущий";
                    relationConclusion = "Є монотонний зв'язок";
                }
                return (new CriteriaView
                {
                    Name = "Спірмена",
                    Estimate = spearman,
                    LeftInterval = 0,
                    RightInterval = 0,
                    Statistics = absT,
                    Quantile = student,
                    Conclusion = conclusion,
                    RelationConclusion = relationConclusion
                });
            }
            public void ShowStats(List<Data.StatsView> statsView)
            {
                statsView.Add(new StatsView
                {
                    Name = "Середнє арифметичне",
                    Estimate = Mean,
                    Deviation = MeanDeviation,
                    LeftInterval = MeanLow,
                    RightInterval = MeanHigh
                });
                statsView.Add(new StatsView
                {
                    Name = "Медіана",
                    Estimate = Med,
                    Deviation = 0,
                    LeftInterval = MedLow,
                    RightInterval = MedHigh
                });
                statsView.Add(new StatsView
                {
                    Name = "Середньоквадратичне відхилення",
                    Estimate = S_,
                    Deviation = S_Deviation,
                    LeftInterval = S_Low,
                    RightInterval = S_High
                });
                statsView.Add(new StatsView
                {
                    Name = "Коефіцієнт асиметрії",
                    Estimate = A_,
                    Deviation = A_Deviation,
                    LeftInterval = A_Low,
                    RightInterval = A_High
                });
                statsView.Add(new StatsView
                {
                    Name = "Коефіцієнт ексцесу",
                    Estimate = E_,
                    Deviation = E_Deviation,
                    LeftInterval = E_Low,
                    RightInterval = E_High
                });
            }

            public void CalculateCriteria(Tuple<List<double>, List<double>> data)
            {
                var pearsonCriteria = CalculatePearsonCriteria(data);
                var spearmanCriteria = CalculateSpearmanCriteria(data);
                var kendallCriteria = CalculateKendallCriteria(data);
                var correlationRatio = CalculateCorrelationRatio(data);
            }
            public double GetStudentQuantile(int N, double alpha, int freedom)
            {
                return MathNet.Numerics.Distributions.StudentT.InvCDF(0.0, 1.0, (double)N - freedom, 1 - alpha / 2);
            }
            private double GetFisherQuantile(double v1, double v2, double alpha)
            {
                return MathNet.Numerics.Distributions.FisherSnedecor.InvCDF(v1, v2, 1 - alpha);
            }
            private List<Tuple<double, double>> CalculateRanks(IEnumerable<double> sample)
            {
                var orderedSample = sample.OrderBy(x => x).ToList();
                List<Tuple<double, double>> serialNumbers = new List<Tuple<double, double>>();
                List<Tuple<double, double>> ranks = new List<Tuple<double, double>>();
                for (int i = 0; i < orderedSample.Count; i++)
                {
                    serialNumbers.Add(new Tuple<double, double>(orderedSample[i], i + 1));
                }

                foreach (var X in serialNumbers.GroupBy(t => t.Item1))
                {
                    foreach (var x in X)
                    {
                        var key = X.Key;
                        var values = X.Select(t => t.Item2).ToList();
                        if (values.Count == 1)
                        {
                            ranks.Add(new Tuple<double, double>(key, values[0]));
                        }
                        else
                        {
                            ranks.Add(new Tuple<double, double>(key, values.Average()));
                        }
                    }
                    
                }
                return ranks;
            }
        }
        
    }
}
