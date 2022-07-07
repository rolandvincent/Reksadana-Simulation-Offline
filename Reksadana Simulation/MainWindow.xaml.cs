using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.Win32;

namespace Reksadana_Simulation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataJSON modul = JsonConvert.DeserializeObject<DataJSON>(Properties.Resources.JSONData);

        public MainWindow()
        {
            InitializeComponent();
            FutureDatePicker.BlackoutDates.Add(new CalendarDateRange(DateTime.Now, DateTime.MaxValue));

            ptitle.Content = modul.pname;
        }

        struct InvestasiData
        {
            public string Tanggal { get; set; }
            public string Investasi { get => $"Rp{NilaiInvestasi.ToString("###,##0.00", cultureInfo)}"; }
            public double NilaiInvestasi { get; set; }
        }

        struct GraphJSON
        {
            public string id;
            public string date;
            public string value;
        }

        struct DataJSON
        {
            public string pid;
            public string ptype;
            public string idate;
            public string inav;
            public string pname;
            public GraphJSON[] nav;
        }

        public static CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("eu-ES");

        struct InvestasiDetail
        {
            public string Tanggal { get; set; }
            public string Investasi { get => $"Rp{NilaiInvestasi.ToString("###,##0.00", cultureInfo)}"; }
            public double NilaiInvestasi { get; set; }
            public double NABValue { get; set; }
            public string NAB { get => $"{NABValue.ToString("#,###,##0.####", cultureInfo)}"; }
            public string Unit { get => $"{UnitValue.ToString("#,###,##0.#####", cultureInfo)}"; }
            public double UnitValue { get; set; }
            public double JumlahUnitValue { get; set; }
            public string JumlahUnit { get => $"{JumlahUnitValue.ToString("#,###,##0.######", cultureInfo)}"; }
            public string TotalInvestasi { get => $"Rp{TotalInvestasiValue.ToString("###,##0.00", cultureInfo)}"; }
            public double TotalInvestasiValue { get; set; }
            public string TotalInvestasiReturn { get => $"Rp{TotalInvestasiReturnValue.ToString("###,##0.00", cultureInfo)}"; }
            public double TotalInvestasiReturnValue { get; set; }
            public string Return { get => $"Rp{ReturnValue.ToString("###,##0.00", cultureInfo)}"; }
            public double ReturnValue { get; set; }
            public double PertumbuhanValue { get; set; }
            public string Pertumbuhan { get => $"{Math.Round(PertumbuhanValue, 2) + "%"}"; }
            public string Untung { get => $"Rp{UntungValue.ToString("###,##0.00", cultureInfo)}"; }
            public double UntungValue { get; set; }
        }

        List<InvestasiData> DataInvestasi = new List<InvestasiData>();
        List<InvestasiDetail> DetailInvest = new List<InvestasiDetail>();

        public static string DateToStringDMY(DateTime date)
        {
            return date.Day.ToString("00") + "-" + date.Month.ToString("00") + "-" + date.Year.ToString("0000");
        }
        public static string DateToStringYMD(DateTime date)
        {
            return date.Year.ToString("0000") + "-" + date.Month.ToString("00") + "-" + date.Day.ToString("00");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(Nominal.Text, out double nominal) && TanggalInvest.SelectedDate != null)
            {
                InvestasiData investasiData = new InvestasiData();
                investasiData.Tanggal = TanggalInvest.SelectedDate.Value.Day.ToString("00") + "-" + TanggalInvest.SelectedDate.Value.Month.ToString("00") + "-" + TanggalInvest.SelectedDate.Value.Year.ToString("0000");
                investasiData.NilaiInvestasi = nominal;
                DataInvestasi.Add(investasiData);
                InvestasiList.ItemsSource = null;
                InvestasiList.ItemsSource = DataInvestasi;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (InvestasiList.SelectedItems.Count > 0)
            {
                InvestasiData investasiData = (InvestasiData)InvestasiList.SelectedItem;
                DataInvestasi.Remove(investasiData);
                InvestasiList.ItemsSource = null;
                InvestasiList.ItemsSource = DataInvestasi;
            }
        }

        private void FutureDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                TanggalInvest.BlackoutDates.Clear();
                TanggalInvest.BlackoutDates.Add(new CalendarDateRange(DateTime.MinValue, FutureDatePicker.SelectedDate?.AddDays(-1) ?? DateTime.Now.AddDays(-1)));
                TanggalInvest.BlackoutDates.Add(new CalendarDateRange(DateTime.Now, DateTime.MaxValue));

            }
            catch { }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            x1.IsEnabled = false;
            x2.IsEnabled = false;
            x3.IsEnabled = false;
            if (Reksadana.Series.Count == 0)
            {
                Reksadana.Series.Add(new LineSeries()
                {
                    Title = "NAB",
                    LineSmoothness = 0,
                    Values = new ChartValues<ObservableValue>(),
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(Colors.Black),
                    PointGeometrySize = 4,
                    PointForeground = new SolidColorBrush(Colors.Blue),
                    Fill = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0))
                });
            }
            Reksadana.Series[0].Values.Clear();
            DateTime? dateStart = null;
            Task.Factory.StartNew(() =>
            {
                DateTime current = DateTime.Now;
                List<string> YLabel = new List<string>();
                Dispatcher.Invoke(() => current = FutureDatePicker.SelectedDate.Value);

                GraphJSON lastJson = new GraphJSON();

                while (current < DateTime.Now.AddDays(-1))
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            bool found = false;
                            foreach (GraphJSON graph in modul.nav)
                            {
                                if (graph.date == DateToStringYMD(current))
                                {
                                    double NAB = double.Parse(graph.value);
                                    this.Title = "Read... " + DateToStringDMY(current);
                                    Reksadana.Series[0].Values.Add(new ObservableValue(NAB));
                                    YLabel.Add(DateToStringDMY(current));

                                    lastJson = graph;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                if (double.TryParse(lastJson.value, out double NAB))
                                    Reksadana.Series[0].Values.Add(new ObservableValue(double.NaN));
                                else
                                    Reksadana.Series[0].Values.Add(new ObservableValue(double.NaN));
                                this.Title = "Read... " + DateToStringDMY(current);
                                YLabel.Add(DateToStringDMY(current));
                            }
                        });

                    }
                    catch
                    {
                    }
                    current = current.AddDays(1);
                }
                Dispatcher.Invoke(() =>
                {
                    Reksadana.AxisX[0].Title = "Tanggal";
                    Reksadana.AxisX[0].Labels = YLabel.ToArray();
                });
                DateTime datefrom = DateTime.Now.AddDays(-1);
                Dispatcher.Invoke(() => datefrom = FutureDatePicker.SelectedDate.Value);
                DetailInvest.Clear();
                lastJson = new GraphJSON();
                InvestasiDetail lastData = new InvestasiDetail()
                {
                    JumlahUnitValue = 0,
                    NABValue = 0,
                    NilaiInvestasi = 0,
                    PertumbuhanValue = 0,
                    ReturnValue = 0,
                    UntungValue = 0,
                    Tanggal = "",
                    TotalInvestasiReturnValue = 0,
                    TotalInvestasiValue = 0,
                    UnitValue = 0
                };
                dateStart = null;
                for (; datefrom < DateTime.Now; datefrom = datefrom.AddDays(1))
                {
                    try
                    {
                        bool found = false;
                        foreach (GraphJSON graph in modul.nav)
                        {
                            if (graph.date == DateToStringYMD(datefrom))
                            {
                                InvestasiDetail investasiDetail = new InvestasiDetail();
                                investasiDetail.Tanggal = DateToStringDMY(datefrom);
                                investasiDetail.NABValue = double.Parse(graph.value);
                                investasiDetail.NilaiInvestasi = DataInvestasi.Find(_ => _.Tanggal == DateToStringDMY(datefrom)).NilaiInvestasi;
                                investasiDetail.UnitValue = (double)investasiDetail.NilaiInvestasi / investasiDetail.NABValue;
                                investasiDetail.JumlahUnitValue = (double)investasiDetail.UnitValue + lastData.JumlahUnitValue;
                                investasiDetail.TotalInvestasiValue = (double)investasiDetail.NilaiInvestasi + lastData.TotalInvestasiValue;
                                investasiDetail.TotalInvestasiReturnValue = (double)investasiDetail.NABValue * investasiDetail.JumlahUnitValue;
                                investasiDetail.ReturnValue = (double)investasiDetail.TotalInvestasiReturnValue - investasiDetail.TotalInvestasiValue;
                                investasiDetail.PertumbuhanValue = (double)investasiDetail.ReturnValue / investasiDetail.TotalInvestasiValue * 100d;
                                investasiDetail.UntungValue = (double)investasiDetail.ReturnValue - lastData.ReturnValue;

                                if (dateStart == null && investasiDetail.NilaiInvestasi != 0) dateStart = datefrom;

                                DetailInvest.Add(investasiDetail);

                                lastJson = graph;
                                lastData = investasiDetail;
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            InvestasiDetail investasiDetail = new InvestasiDetail();
                            investasiDetail.Tanggal = DateToStringDMY(datefrom);
                            investasiDetail.NABValue = double.Parse(lastJson.value == null ? "0" : lastJson.value);
                            investasiDetail.NilaiInvestasi = DataInvestasi.Find(_ => _.Tanggal == DateToStringDMY(datefrom)).NilaiInvestasi;
                            investasiDetail.UnitValue = (double)investasiDetail.NilaiInvestasi / investasiDetail.NABValue;
                            investasiDetail.JumlahUnitValue = (double)investasiDetail.UnitValue + lastData.JumlahUnitValue;
                            investasiDetail.TotalInvestasiValue = (double)investasiDetail.NilaiInvestasi + lastData.TotalInvestasiValue;
                            investasiDetail.TotalInvestasiReturnValue = (double)investasiDetail.NABValue * investasiDetail.JumlahUnitValue;
                            investasiDetail.ReturnValue = (double)investasiDetail.TotalInvestasiReturnValue - investasiDetail.TotalInvestasiValue;
                            investasiDetail.PertumbuhanValue = (double)investasiDetail.ReturnValue / investasiDetail.TotalInvestasiValue * 100d;
                            investasiDetail.UntungValue = (double)investasiDetail.ReturnValue - lastData.ReturnValue;

                            DetailInvest.Add(investasiDetail);

                            lastData = investasiDetail;
                        }

                        //InvestasiDetail investasiDetail = new InvestasiDetail();
                        //investasiDetail.Tanggal = DateToStringDMY(datefrom);
                        //investasiDetail.NAB = DataNAB.Find(_ => _.Tanggal == DateToStringDMY(datefrom)).NAB;
                        //investasiDetail.NilaiInvestasi = DataInvestasi.Find(_ => _.Tanggal == DateToStringDMY(datefrom)).NilaiInvestasi;
                        //investasiDetail.UnitValue = (double)investasiDetail.NilaiInvestasi / investasiDetail.NAB;
                        //investasiDetail.JumlahUnit = (double)investasiDetail.UnitValue + lastData.JumlahUnit;
                        //investasiDetail.TotalInvestasiValue = (double)investasiDetail.NilaiInvestasi + lastData.TotalInvestasiValue;
                        //investasiDetail.TotalInvestasiReturnValue = (double)investasiDetail.NAB * investasiDetail.JumlahUnit;
                        //investasiDetail.ReturnValue = (double)investasiDetail.TotalInvestasiReturnValue - investasiDetail.TotalInvestasiValue;
                        //investasiDetail.PertumbuhanValue = (double)investasiDetail.ReturnValue / investasiDetail.TotalInvestasiValue * 100d;
                        //investasiDetail.UntungValue = (double)investasiDetail.ReturnValue - lastData.ReturnValue;

                        //DetailInvest.Add(investasiDetail);

                        //lastData = investasiDetail;
                    }
                    catch { }
                }
            }).ContinueWith(task =>
            {
                Dispatcher.Invoke(() =>
                {
                    InvestDetailList.ItemsSource = null;
                    InvestDetailList.ItemsSource = DetailInvest;
                    this.Title = "Simulasi Reksadana";
                    x1.IsEnabled = true;
                    x2.IsEnabled = true;
                    x3.IsEnabled = true;
                    if (DetailInvest.Count > 0)
                    {
                        totalKeuntungan.Content = DetailInvest.Last().Return;
                        totalPersen.Content = DetailInvest.Last().Pertumbuhan;
                        totalInvest.Content = DetailInvest.Last().TotalInvestasiReturn;
                        totalTabung.Content = DetailInvest.Last().TotalInvestasi;
                        totalHari.Content = DateTime.Now.Subtract(dateStart.Value).Days + " Hari";
                    }
                    else
                    {
                        totalKeuntungan.Content = "";
                        totalPersen.Content = "";
                        totalInvest.Content = "";
                        totalTabung.Content = "";
                        totalHari.Content = "";
                    }
                });
            });

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oplg = new OpenFileDialog();
            oplg.Filter = "JSON|*.json";
            oplg.Title = "Buka data harga json";
            if (oplg.ShowDialog() == true)
            {
                modul = JsonConvert.DeserializeObject<DataJSON>(File.ReadAllText(oplg.FileName));
                ptitle.Content = modul.pname;

                if (Reksadana.Series.Count > 0)
                    Reksadana.Series[0].Values.Clear();
            }
        }
    }


}
