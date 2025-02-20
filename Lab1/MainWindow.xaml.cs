using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            Data.Stats_.InitializeStats();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
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
            Data.Data_.Item1.Clear();
            Data.Data_.Item2.Clear();
            Data.DataView_.Clear();
            Data.XStatsView.Clear();
            Data.YStatsView.Clear();
            Data.CriteriaViews.Clear();
            XStats.ItemsSource = null;
            YStats.ItemsSource = null;
            DataGrid.ItemsSource = null;
            CriteriaGrid.ItemsSource = null;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if(string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[0], out double value1) &&
                    double.TryParse(parts[1], out double value2))
                {
                    Data.Data_.Item1.Add(value1);
                    Data.Data_.Item2.Add(value2);
                    Data.DataView_.Add(new Data.DataView{X = value1, Y = value2});
                    CorrelationField.Plot.Add.Scatter(value1, value2);
                }
            }
            
            DataGrid.ItemsSource = Data.DataView_;
            CorrelationField.Plot.Axes.Margins(0, 0);
            CorrelationField.Refresh();
            Data.Stats_.XStats.CalculateStats(Data.Data_.Item1);
            Data.Stats_.YStats.CalculateStats(Data.Data_.Item2);
            Data.Stats_.XStats.ShowStats(Data.XStatsView);
            Data.Stats_.YStats.ShowStats(Data.YStatsView);
            XStats.ItemsSource = XStatsView;
            YStats.ItemsSource = YStatsView;
            Data.Stats_.CalculatePearsonCriteria(Data.Data_);
            Data.Stats_.CalculateSpearmanCriteria(Data.Data_);
            Data.Stats_.CalculateKendallCriteria(Data.Data_);
            Data.Stats_.CalculateCorrelationRatio(Data.Data_);
            CriteriaGrid.ItemsSource = CriteriaViews;
        }
    }
}