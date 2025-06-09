using System.IO;
using System.Windows;
using System.Windows.Controls;
using OpenTK.Graphics.OpenGL;
using ScottPlot;
using ScottPlot.Plottables;
using static Lab1.Data;

namespace Lab1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            Stats_.InitializeStats();
        }

        private async void LoadData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Text File",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = false
            };

            bool? result = openFileDialog.ShowDialog();
            if (result != true)
            {
                return;
            }
            var filePath = openFileDialog.FileName;
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);
            CorrelationField.Plot.Clear();
            Data_.Item1.Clear();
            Data_.Item2.Clear();
            OldXs.Clear();
            DataView_.Clear();
            XStatsView.Clear();
            YStatsView.Clear();
            CriteriaViews.Clear();
            ComparingCriteria.Clear();
            XStats.ItemsSource = null;
            YStats.ItemsSource = null;
            DataGrid.ItemsSource = null;
            CriteriaGrid.ItemsSource = null;
            ComparingGrid.ItemsSource = null;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if(string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[0], out double value1) &&
                    double.TryParse(parts[1], out double value2))
                {
                    OldXs.Add(value1);
                    Data_.Item1.Add(value1);
                    Data_.Item2.Add(value2);
                    DataView_.Add(new DataView{X = value1, Y = value2});
                    CorrelationField.Plot.Add.Scatter(value1, value2);
                }
            }
            
            DataGrid.ItemsSource = DataView_;
            CorrelationField.Plot.Axes.Margins(0, 0);
            CorrelationField.Plot.YLabel("y");
            CorrelationField.Plot.XLabel("x");
            CorrelationField.Refresh();
            Stats_.XStats.CalculateStats(Data_.Item1);
            Stats_.YStats.CalculateStats(Data_.Item2);
            Stats_.XStats.ShowStats(XStatsView);
            Stats_.YStats.ShowStats(YStatsView);
            XStats.ItemsSource = XStatsView;
            YStats.ItemsSource = YStatsView;
            Stats_.CalculateCriteria(Data_);
            CriteriaGrid.ItemsSource = CriteriaViews;
            ComparingGrid.ItemsSource = ComparingCriteria;
        }

        private void RecoveryLinearRegression(List<double> xs)
        {
            RegressionView_.Clear();
            FTest.Clear();
            ParametersGrid.ItemsSource = null;
            FTestGrid.ItemsSource = null;
            CalculateValueView.Clear();
            RegressionGrid.ItemsSource = null;
            var pearson = Stats_.CalculatePearsonCriteria(Data_);
            Regression_.SetValues(Data_, Stats_.XStats, Stats_.YStats, pearson.Estimate);
            var result = Regression_.EstimateParameters();
            RegressionView_.Add(result.Item1);
            RegressionView_.Add(result.Item2);
            ParametersGrid.ItemsSource = RegressionView_;
            var fTest = Regression_.FTest();
            var rSquared = Regression_.RSquaredView();
            var residualsDispersion = Regression_.ResidualsDispersionView();
            FTest.Add(fTest);
            FTest.Add(rSquared);
            FTest.Add(residualsDispersion);
            FTestGrid.ItemsSource = FTest;
            ShowRegression(xs);
            ShowRegressionIntervals(xs);
            ShowPredictiveValueIntervals(xs);
            CorrelationField.Plot.Axes.Margins(0, 0);
            CorrelationField.Refresh();
        }

        private void ShowRegression(List<double> xs)
        {
            var ys = Data_.Item1.Select(x => Regression_.CalculateValue(x)).ToList();
            var list = Data_.Item1.Zip(Data_.Item2, (x, y) => new { x, y })
                .OrderBy(pair => pair.x)
                .ToList();
            bool isDescending = list.First().y > list.Last().y;
            var sortedYs = ys.OrderBy(p => isDescending ? -p : p).ToList();
            CorrelationField.Plot.Add.ScatterLine(xs.OrderBy(x => x).ToList(), sortedYs);
        }
        private void ShowPredictiveValueIntervals(List<double> xs)
        {
            var low = Data_.Item1.Select(x => Regression_.Low(Regression_.CalculateValue(x), Regression_.PredictiveValueDeviation(x))).ToList();
            var high = Data_.Item1.Select(x => Regression_.High(Regression_.CalculateValue(x), Regression_.PredictiveValueDeviation(x))).ToList();
            var list = Data_.Item1.Zip(Data_.Item2, (x, y) => new { x, y })
                .OrderBy(pair => pair.x)
                .ToList();
            bool isDescending = list.First().y > list.Last().y;
            var sortedLow = low.OrderBy(p => isDescending ? -p : p).ToList();
            var x = CorrelationField.Plot.Add.ScatterLine(xs.OrderBy(x => x).ToList(), sortedLow);
            x.LinePattern = ScottPlot.LinePattern.Dotted;
            x.Color = Colors.Black;
            x.MarkerSize = 0;
            var sortedHigh = high.OrderBy(p => isDescending ? -p : p).ToList();
            var y = CorrelationField.Plot.Add.ScatterLine(xs.OrderBy(x => x).ToList(), sortedHigh);
            y.LinePattern = ScottPlot.LinePattern.Dotted;
            y.Color = Colors.Black;
            y.MarkerSize = 0;
            y.LegendText = "Інтервал на прогнозне значення";
        }

        private void ShowRegressionIntervals(List<double> xs)
        {
            var low = Data_.Item1.Select(x => Regression_.Low(Regression_.CalculateValue(x), Regression_.RegressionDeviation(x))).ToList();
            var high = Data_.Item1.Select(x => Regression_.High(Regression_.CalculateValue(x), Regression_.RegressionDeviation(x))).ToList();
            var list = Data_.Item1.Zip(Data_.Item2, (x, y) => new { x, y })
                .OrderBy(pair => pair.x)
                .ToList();
            bool isDescending = list.First().y > list.Last().y;
            var sortedLow = low.OrderBy(p => isDescending ? -p : p).ToList();
            var x = CorrelationField.Plot.Add.ScatterLine(xs.OrderBy(x => x).ToList(), sortedLow);
            x.LinePattern = ScottPlot.LinePattern.Dashed;
            x.LineWidth = 2;
            x.Color = Colors.Red;
            x.MarkerSize = 0;
            var sortedHigh = high.OrderBy(p => isDescending ? -p : p).ToList();
            var y = CorrelationField.Plot.Add.ScatterLine(xs.OrderBy(x => x).ToList(), sortedHigh);
            y.LinePattern = ScottPlot.LinePattern.Dashed;
            y.LineWidth = 2;
            y.Color = Colors.Red;
            y.MarkerSize = 0;
            y.LegendText = "Інтервал на регресію";
        }
        private void SelectK_OnClick(object sender, RoutedEventArgs e)
        {
            int k = int.Parse(Classes.Text);
            Stats_.k = k;
            CriteriaGrid.ItemsSource = null;
            ComparingGrid.ItemsSource = null;
            CriteriaViews.Clear();
            ComparingCriteria.Clear();
            Stats_.CalculateCriteria(Data_);
            CriteriaGrid.ItemsSource = CriteriaViews;
            ComparingGrid.ItemsSource = ComparingCriteria;
        }

        private void CalculateValue_Click(object sender, RoutedEventArgs e)
        {
            CalculateValueView.Clear();
            RegressionGrid.ItemsSource = null;
            var x = double.Parse(X.Text);
            var y = Regression_.CalculateValue(x);
            var d = Regression_.PredictiveValueDeviation(x);
            var low = Regression_.Low(y, d);
            var high = Regression_.High(y, d);
            var view = new RegressionView
            {
                Estimate = y,
                LeftInterval = low,
                RightInterval = high
            };
            CalculateValueView.Add(view);
            d = Regression_.RegressionDeviation(x);
            low = Regression_.Low(y, d);
            high = Regression_.High(y, d);
            view = new RegressionView
            {
                LeftInterval = low,
                RightInterval = high
            };
            CalculateValueView.Add(view);
            RegressionGrid.ItemsSource = CalculateValueView;
        }

        private void RecoveryRegression_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button.Name == "Linear")
            {
                Data_ = new Tuple<List<double>, List<double>>(new List<double>(OldXs),
                    new List<double>(Data_.Item2));
                Stats_.InitializeStats();
                Stats_.XStats.CalculateStats(Data_.Item1);
                Stats_.YStats.CalculateStats(Data_.Item2);
                DataGrid.ItemsSource = null;
                DataView_ = Data_.Item1
                    .Zip(Data_.Item2, (x, y) => new DataView { X = x, Y = y })
                    .ToList();
                DataGrid.ItemsSource = DataView_;
                CorrelationField.Plot.Clear();
                CorrelationField.Plot.Add.ScatterPoints(Data_.Item1, Data_.Item2);
                CorrelationField.Plot.XLabel("x");
                RecoveryLinearRegression(Data_.Item1);
            }
            if (button.Name == "Nonlinear")
            {
                CorrelationField.Plot.Clear();
                Data_ = new Tuple<List<double>, List<double>>(OldXs.Select(x => Math.Log(x)).ToList(),
                    Data_.Item2);
                Stats_.InitializeStats();
                Stats_.XStats.CalculateStats(Data_.Item1);
                Stats_.YStats.CalculateStats(Data_.Item2);
                DataGrid.ItemsSource = null;
                DataView_ = Data_.Item1
                    .Zip(Data_.Item2, (x, y) => new DataView { X = x, Y = y })
                    .ToList();
                DataGrid.ItemsSource = DataView_;
                CorrelationField.Plot.Add.ScatterPoints(Data_.Item1, Data_.Item2);
                CorrelationField.Plot.XLabel("t=lnx");
                RecoveryLinearRegression(Data_.Item1);
            }

            if (button.Name == "Return")
            {
                CorrelationField.Plot.Clear();
                CorrelationField.Plot.Add.ScatterPoints(OldXs, Data_.Item2);
                CorrelationField.Plot.XLabel("x");
                RecoveryLinearRegression(OldXs);
            }
                
        }
    }
}