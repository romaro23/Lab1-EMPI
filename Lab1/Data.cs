using System.Diagnostics.SymbolStore;

namespace Lab1
{
    public class Data
    {
        public static Tuple<List<double>, List<double>> Data_ { get; set; } = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
        public static List<double> OldXs { get; set; } = new List<double>(); 
        public static List<DataView> DataView_ { get; set; } = new List<DataView>();
        public static List<StatsView> XStatsView { get; set; } = new List<StatsView>();
        public static List<StatsView> YStatsView { get; set; } = new List<StatsView>();
        public static List<CriteriaView> CriteriaViews { get; set; } = new List<CriteriaView>();
        public static List<CriteriaView> ComparingCriteria { get; set; } = new List<CriteriaView>();
        public static List<RegressionView> RegressionView_ { get; set; } = new List<RegressionView>();
        public static List<FTestView> FTest { get; set; } = new List<FTestView>();

        public static List<RegressionView> CalculateValueView { get; set; } = new List<RegressionView>(); 
        public static Stats Stats_ { get; set; } = new Stats();
        public static Regression Regression_ { get; set; } = new Regression();

        public class DataView
        {
            public double X { get; set; }
            public double Y { get; set; }
        }

        public class StatsView
        {
            public string? Name { get; set; }
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

        public class RegressionView
        {
            public string? Name { get; set; }
            public double Estimate { get; set; }
            public double Deviation { get; set; }
            public double LeftInterval { get; set; }
            public double RightInterval { get; set; }
            public double Statistics { get; set; }
            public double Quantile { get; set; }
            public string? Conclusion { get; set; }
        }

        public class FTestView
        {
            public string? Name { get; set; }
            public double Statistics { get; set; }
            public double Quantile { get; set; }
            public string? Conclusion { get; set; }
        }
        public class Regression
        {
            private Tuple<List<double>, List<double>> _data;
            private Stats _xStats, _yStats;
            private double _a, _b, _pearson;
            public Tuple<List<double>, List<double>> Data
            {
                get => _data;
                set => _data = value ?? throw new ArgumentNullException(nameof(value));
            }

            public Stats XStats
            {
                get => _xStats;
                set => _xStats = value ?? throw new ArgumentNullException(nameof(value));
            }

            public Stats YStats
            {
                get => _yStats;
                set => _yStats = value ?? throw new ArgumentNullException(nameof(value));
            }

            public double A
            {
                get => _a;
                set => _a = value;
            }

            public double B
            {
                get => _b;
                set => _b = value;
            }

            public double Pearson
            {
                get => _pearson;
                set => _pearson = value;
            }

            private int _n;

            public FTestView RSquaredView()
            {
                return new FTestView
                {
                    Name = "R\u00B2",
                    Statistics = RSquared(),
                    Conclusion = "",
                    Quantile = 0
                };
            }

            public FTestView ResidualsDispersionView()
            {
                return new FTestView
                {
                    Name = "S\u00B2\u2091",
                    Statistics = ResidualsDispersion(),
                    Conclusion = "",
                    Quantile = 0
                };
            }
            public double RSquared()
            {
                var s = _data.GetType().GenericTypeArguments.Length;
                var value = 1 - ResidualsDispersion() * (_n - s) / (Math.Pow(_yStats.S, 2) * _n);
                return value * 100;
            }
            public FTestView FTest()
            {
                var fValue = FValue();
                var fisher = Stats_.GetFisherQuantile(1, _n - 2, 0.05);
                string conclusion = fValue > fisher ? "Регресія значуща" : "Регресія не значуща";
                return new FTestView
                {
                    Name = "F",
                    Statistics = fValue,
                    Quantile = fisher,
                    Conclusion = conclusion
                };
            }
            public double FValue()
            {
                var ys = _data.Item1.Select(CalculateValue).ToList();
                var sum = ys.Select(y => Math.Pow(y - _yStats.Mean, 2)).Sum();
                return sum / ResidualsDispersion();
            }
            public void SetValues(Tuple<List<double>, List<double>> data, Stats xStats, Stats yStats, double pearson)
            {
                _data = data;
                _xStats = xStats;
                _yStats = yStats;
                _pearson = pearson;
                _n = _data.Item1.Count;
                CalculateBParameter();
                CalculateAParameter();
            }

            public double PredictiveValueDeviation(double x)
            {
                return Math.Sqrt(Math.Pow(RegressionDeviation(x), 2) + ResidualsDispersion());
            }
            public double RegressionDeviation(double x)
            {
                var dispersion = ResidualsDispersion();
                var bDeviation = BDeviation(dispersion);
                var value = dispersion / _n + Math.Pow(bDeviation * (x - _xStats.Mean), 2);
                return Math.Sqrt(value);
            }
            public (RegressionView, RegressionView) EstimateParameters()
            {
                var dispersion = ResidualsDispersion();
                var aDeviation = ADeviation(dispersion);
                var bDeviation = BDeviation(dispersion);
                var aLow = Low(_a, aDeviation);
                var aHigh = High(_a, aDeviation);
                var bLow = Low(_b, bDeviation);
                var bHigh = High(_b, bDeviation);
                var aStats = Stats(_a, aDeviation);
                var bStats = Stats(_b, bDeviation);
                var student = Stats_.GetStudentQuantile(_n, 0.05, 2);
                string aResult = "Не значущий";
                if (Math.Abs(aStats) > student)
                {
                    aResult = "Значущий";
                }
                string bResult = "Не значущий";
                if (Math.Abs(bStats) > student)
                {
                    bResult = "Значущий";
                }

                RegressionView aView = new RegressionView
                {
                    Name = "a",
                    Estimate = _a,
                    Deviation = aDeviation,
                    LeftInterval = aLow,
                    RightInterval = aHigh,
                    Statistics = aStats,
                    Quantile = student,
                    Conclusion = aResult
                };
                RegressionView bView = new RegressionView
                {
                    Name = "b",
                    Estimate = _b,
                    Deviation = bDeviation,
                    LeftInterval = bLow,
                    RightInterval = bHigh,
                    Statistics = bStats,
                    Quantile = student,
                    Conclusion = bResult
                };
                return (aView, bView);
            }

            public double Stats(double value, double deviation)
            {
                return value / deviation;
            }   
            public double High(double value, double deviation)
            {
                var t = Stats_.GetStudentQuantile(_n, 0.05, 2);
                return value + t * deviation;
            }

            public double Low(double value, double deviation)
            {
                var t = Stats_.GetStudentQuantile(_n, 0.05, 2);
                return value - t * deviation;
            }
            public double BDeviation(double dispersion)
            {
                var value = dispersion / (_n * Math.Pow(_xStats.S, 2));
                return Math.Sqrt(value);
            }
            public double ADeviation(double dispersion)
            {
                var value = dispersion / _n * (1 + Math.Pow(_xStats.Mean, 2) / Math.Pow(_xStats.S, 2));
                return Math.Sqrt(value);
            }
            public double ResidualsDispersion()
            {
                var xs = _data.Item1;
                var ys = _data.Item2;
                var ysRecovered = xs.Select(x => CalculateValue(x)).ToList();
                var residuals = ys.Zip(ysRecovered, (y, yRecovered) => y - yRecovered).ToList();
                var residualsSquared = residuals.Select(r => Math.Pow(r, 2)).ToList();
                var sum = residualsSquared.Sum();
                return 1.0 / (_n  - 2) * sum;
            }
            public double CalculateValue(double x)
            {
                return _a + _b * x;
            }
            public void CalculateBParameter()
            {
                _b = _pearson * (_yStats.S / _xStats.S);
            }

            public void CalculateAParameter()
            {
                _a = _yStats.Mean - _xStats.Mean * _b;
            }
        }
        public class Stats
        {
            public int N;
            public int k = 0;
            public double Mean, Med, S_, S, A_, A, E_, E, MeanDeviation, S_Deviation, A_Deviation, E_Deviation, T, MeanLow, MeanHigh, MedLow, MedHigh, S_Low, S_High, A_Low, A_High, E_Low, E_High;
            public Stats? XStats { get; set; }
            public Stats? YStats { get; set; }

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
                string conclusion = "Не значущий";
                string relationConclusion = "Монотонного зв'язку немає";
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

            public (CriteriaView, CriteriaView) CalculateCorrelationRatio(Tuple<List<double>, List<double>> data)
            {
                if (k == 0)
                {
                    k = (int)(1 + 1.44 * Math.Log(data.Item1.Count));
                }
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
                        if (i == k + 1)
                        {
                            classes.Add(new Tuple<double, double>(temp, data.Item1.Max()));
                        }
                        else
                        {
                            classes.Add(new Tuple<double, double>(temp, g));
                        }
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
                    if (list.Value.Count > 0)
                    {
                        var diff = list.Value.Average() - yAvg;
                        groupVariance += Math.Pow(diff, 2) * list.Value.Count;
                    }
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
                var correlationRatioSquared = Math.Pow(correlationRatio, 2);
                var f = (correlationRatioSquared / (k - 1)) / ((1 - correlationRatioSquared) / (n - k));
                var fisher = GetFisherQuantile(k - 1, n, 0.05);
                string conclusion = "Не значущий";
                string relationConclusion = "Зв'язку немає";
                CriteriaView secondResult = new CriteriaView
                {
                    Name = "",
                    Estimate = 0,
                    LeftInterval = 0,
                    RightInterval = 0,
                    Statistics = 0,
                    Quantile = 0,
                    Conclusion = "",
                    RelationConclusion = ""
                };
                if (f > fisher)
                {
                    conclusion = "Значущий";
                    relationConclusion = "Є зв'язок";
                    Tuple<List<double>, List<double>> oldData = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
                    foreach (var pair in newData)
                    {
                        foreach (var y in pair.Value)
                        {
                            oldData.Item1.Add(pair.Key);
                            oldData.Item2.Add(y);
                        }
                    }
                    XStats.CalculateStats(oldData.Item1);
                    var pearsonCriteria = CalculatePearsonCriteria(oldData);
                    var pearson = pearsonCriteria.Estimate;
                    var pearsonSquared = Math.Pow(pearsonCriteria.Estimate, 2);
                    var f_ = ((correlationRatioSquared - pearsonSquared) / (k - 2)) / ((1 - correlationRatioSquared) / (n - k));
                    var fisher_ = GetFisherQuantile(k - 2, n - k, 0.05);
                    string conclusion_;
                    string relationConclusion_;
                    conclusion_ = "Не рівні";
                    relationConclusion_ = "Лінійного зв'язку немає";
                    if (f_ <= fisher_)
                    {
                        conclusion_ = "Рівні";
                        relationConclusion_ = "Є лінійний зв'язок";
                    }
                    secondResult = new CriteriaView
                    {
                        Name = "",
                        Estimate = pearson,
                        LeftInterval = correlationRatio,
                        RightInterval = 0,
                        Statistics = f_,
                        Quantile = fisher_,
                        Conclusion = conclusion_,
                        RelationConclusion = relationConclusion_
                    };
                }
                CriteriaView firstResult = new CriteriaView
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
                return (firstResult, secondResult);

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
                string conclusion = "Не значущий";
                string relationConclusion = "Монотонного зв'язку немає";
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
            public void ShowStats(List<StatsView> statsView)
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
                CriteriaViews.Add(pearsonCriteria);
                CriteriaViews.Add(spearmanCriteria);
                CriteriaViews.Add(kendallCriteria);
                CriteriaViews.Add(correlationRatio.Item1);
                ComparingCriteria.Add(correlationRatio.Item2);
            }
            public double GetStudentQuantile(int N, double alpha, int freedom)
            {
                return MathNet.Numerics.Distributions.StudentT.InvCDF(0.0, 1.0, (double)N - freedom, 1 - alpha / 2);
            }
            public double GetFisherQuantile(double v1, double v2, double alpha)
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
