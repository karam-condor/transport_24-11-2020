using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace transport
{
    /// <summary>
    /// Interaction logic for MonitorWindow.xaml
    /// </summary>
    public partial class MonitorWindow : Window
    {
        DBConnection dBConnection;
        DataTable cargaTable, notaTable;
        List<chartItem> chartList;        
        

        public MonitorWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cargas_refrech_checkBox.SetCurrentValue(CheckBox.IsCheckedProperty, true);
            loadCargas();
            setTimer();
            setCharts();
        }

        private void setTimer()
        {
            var timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 1, 0);
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (cargas_refrech_checkBox.IsChecked == true)
            {
                try
                {
                    clearMap();
                    loadCargas();
                    setCharts();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void loadCargas()
        {
            ControlTemplate template = (ControlTemplate)this.FindResource("CutomPushpinTemplate");
            dBConnection = new DBConnection();
            cargaTable = dBConnection.readByAdapter("SELECT * FROM LOGTRANSCAR WHERE DTFINAL IS NULL", null, null);
            if (cargaTable != null && cargaTable.Rows.Count > 0)
            {
                foreach (DataRow dr in cargaTable.Rows)
                {                    
                    Location location = new Location(Convert.ToDouble(dr["LAT"]),Convert.ToDouble(dr["LONGT"]));
                    drawPushPin(location, template, Convert.ToString(dr["NUMCAR"]));
                }
                setBounds(cargaTable);
            }            
        }

        private void setCharts()
        {
            String cargas = String.Join(",", cargaTable.AsEnumerable().Select(x => x["NUMCAR"]).ToArray());
            chartList = new List<chartItem>();
            notaTable = dBConnection.readByAdapter("SELECT * FROM(SELECT NUMCAR, COUNT(CASE STATUS WHEN 1 THEN 1 END) QTPER, COUNT(CASE STATUS WHEN 2 THEN 1 END) QTPEND, COUNT(CASE STATUS WHEN 3 THEN 1 END) QTDEV, COUNT(CASE STATUS WHEN 4 THEN 1 END) QTREN FROM  LOGTRANSPROCESS WHERE NUMCAR IN(SELECT NUMCAR FROM LOGTRANSCAR WHERE DTFINAL IS NULL) GROUP BY NUMCAR) A LEFT JOIN (SELECT NUMCAR, COUNT(NUMCAR) QTTOT FROM PCPEDC@WINT WHERE NUMCAR IN (SELECT NUMCAR FROM LOGTRANSCAR WHERE DTFINAL IS NULL) GROUP BY NUMCAR) B ON A.NUMCAR = B.NUMCAR", null, null);
            if (notaTable != null && notaTable.Rows.Count > 0)
            {                
                foreach (DataRow dr in notaTable.Rows)
                {
                    int qtEmTransit = Convert.ToInt32(dr["QTTOT"]) - (Convert.ToInt32(dr["QTPER"]) + Convert.ToInt32(dr["QTPEND"]) + Convert.ToInt32(dr["QTDEV"]) + Convert.ToInt32(dr["QTREN"]));                                                           
                    chartList.Add(new chartItem
                    {
                        numCarga = Convert.ToInt32(dr["NUMCAR"]),
                        Perfeito = Convert.ToInt32(dr["QTPER"]),
                        Pendencia = Convert.ToInt32(dr["QTPEND"]),
                        Devoluçao = Convert.ToInt32(dr["QTDEV"]),
                        Rentrega = Convert.ToInt32(dr["QTREN"]),
                        emTransito = qtEmTransit
                    });;                                        
                }
                chartListView.ItemsSource = chartList;
            }
        }
        private void drawPushPin(Location location, ControlTemplate template, String numcar)
        {
            Pushpin pin = new Pushpin();
            pin.Location = location;
            pin.Template = template;
            pin.PositionOrigin = PositionOrigin.BottomLeft;
            pin.Content = numcar;
            mapView.Children.Add(pin);
        }

        private void clearMap()
        {
            mapView.Children.Clear();
        }

        private void setBounds(DataTable dt)
        {
            var numLat = Math.Abs((Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Max()) - Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Min())) /
                Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Max()));
            var numLongt = Math.Abs((Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Max()) - Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Min())) /
                Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Max()));
            var box = new LocationRect(Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Max()) + numLat,
                    Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Min()) - numLongt, Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Min()) - numLat,
                    Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Max()) + numLongt);
            mapView.SetView(box);            
        }


        class chartItem
        {
            public int numCarga { set; get; }
            public int emTransito { set; get; }
            public int Perfeito { set; get; }
            public int Pendencia { set; get; }
            public int Devoluçao { set; get; }
            public int Rentrega { set; get; }
        }
    }
}