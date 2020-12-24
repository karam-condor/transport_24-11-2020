using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees;
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
        DataTable cargaTable, notaTable,notaTable2,notaTable3;
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
            cargaTable = dBConnection.readByAdapter("SELECT * FROM "+tabelas.cargaTable+" WHERE DTFINAL IS NULL", null, null);
            if (cargaTable != null && cargaTable.Rows.Count > 0)
            {
                foreach (DataRow dr in cargaTable.Rows)
                {
                    try
                    {
                        Location location = new Location(Methods.doubleParser(Convert.ToString(dr["LAT"])), Methods.doubleParser(Convert.ToString(dr["LONGT"])));
                        drawPushPin(location, template, Convert.ToString(dr["NUMCAR"]));
                    }
                    catch(Exception ex)
                    {

                    }
                    
                }
                setBounds(cargaTable);
            }            
        }

        private void setCharts()
        {
            String cargas = String.Join(",", cargaTable.AsEnumerable().Select(x => x["NUMCAR"]).ToArray());
            chartList = new List<chartItem>();
            notaTable = dBConnection.readByAdapter("SELECT * FROM(SELECT NUMCAR, COUNT(CASE STATUS WHEN 1 THEN 1 END) QTPER, COUNT(CASE STATUS WHEN 2 THEN 1 END) QTPEND, COUNT(CASE STATUS WHEN 3 THEN 1 END) QTDEV, COUNT(CASE STATUS WHEN 4 THEN 1 END) QTREN FROM  "+tabelas.processTable+" WHERE NUMCAR IN(SELECT NUMCAR FROM "+tabelas.cargaTable+" WHERE DTFINAL IS NULL) GROUP BY NUMCAR) A LEFT JOIN (SELECT NUMCAR, COUNT(NUMCAR) QTTOT FROM pcnfsaid@WINT WHERE NUMCAR IN (SELECT NUMCAR FROM "+tabelas.cargaTable+" WHERE DTFINAL IS NULL) GROUP BY NUMCAR) B ON A.NUMCAR = B.NUMCAR", null, null);
            notaTable2 = dBConnection.readByAdapter("select numcar,count(numcar) count from pcnfsaid@wint where numcar in (select numcar from "+tabelas.cargaTable+" where dtfinal is null) group by numcar", null, null);
            notaTable3 = dBConnection.readByAdapter("select a.numcar,b.placa from pccarreg@wint a,pcveicul@wint b where a.codveiculo = b.codveiculo and a.numcar in (select numcar from " + tabelas.cargaTable + " where dtfinal is null)", null, null);
        
            if (notaTable2 != null && notaTable2.Rows.Count > 0)
            {
                if(notaTable != null && notaTable.Rows.Count > 0)
                {
                    foreach (DataRow dr in notaTable.Rows)
                    {
                        int qtEmTransit = Convert.ToInt32(dr["QTTOT"]) - (Convert.ToInt32(dr["QTPER"]) + Convert.ToInt32(dr["QTPEND"]) + Convert.ToInt32(dr["QTDEV"]) + Convert.ToInt32(dr["QTREN"]));
                        if(qtEmTransit < 0)
                        {
                            qtEmTransit = 0;
                        }
                        chartList.Add(new chartItem
                        {
                            numCarga = Convert.ToInt32(dr["NUMCAR"]),
                            Perfeito = Convert.ToInt32(dr["QTPER"]),
                            Pendencia = Convert.ToInt32(dr["QTPEND"]),
                            Devoluçao = Convert.ToInt32(dr["QTDEV"]),
                            Rentrega = Convert.ToInt32(dr["QTREN"]),
                            emTransito = qtEmTransit
                        });
                    }
                }                
                foreach (DataRow dr in notaTable2.Rows)
                {
                    if(notaTable.Select("numcar = '" + Convert.ToInt32(dr["NUMCAR"]) + "'").Length == 0)
                    {
                        chartList.Add(new chartItem
                        {
                            numCarga = Convert.ToInt32(dr["NUMCAR"]),
                            Perfeito = 0,
                            Pendencia = 0,
                            Devoluçao = 0,
                            Rentrega = 0,
                            emTransito = Convert.ToInt32(dr["COUNT"])
                        });
                    }                    
                }                
                if(notaTable3.Rows != null && notaTable3.Rows.Count > 0)
                {
                    foreach(var item in chartList)
                    {
                        foreach(DataRow dr in notaTable3.Rows)
                        {
                            if(Convert.ToString(dr["NUMCAR"]) == Convert.ToString(item.numCarga))
                            {
                                item.placa = Convert.ToString(dr["PLACA"]);
                                break;
                            }
                        }
                    }
                }
                setTotalCounts();
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
            try
            {
                float numLat = 0, numLongt = 0;
                var box = new LocationRect(Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Max())) + numLat,
                        Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Min())) - numLongt, Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Min())) - numLat,
                        Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Max()) + numLongt));
                mapView.SetView(box);
            }
            catch
            {
                mapView.Center = new Location(-15.7785, -47.9287);
                mapView.ZoomLevel = 9;
            }
        }

        private void setTotalCounts()
        {
            if(chartList != null && chartList.Count > 0)
            {
                countViagensTextBlock.Text = chartList.Count.ToString(); ;
                countEmTransitoTextBlock.Text = chartList.AsEnumerable().Sum(x=>x.emTransito).ToString();
                countPerfeitoTextBlock.Text = chartList.AsEnumerable().Sum(x => x.Perfeito).ToString();
                countPendenciaTextBlock.Text = chartList.AsEnumerable().Sum(x=>x.Pendencia).ToString();
                countReEntregaTextBlock.Text = chartList.AsEnumerable().Sum(x=>x.Rentrega).ToString();
                countDevolucaoTextBlock.Text = chartList.AsEnumerable().Sum(x=>x.Devoluçao).ToString();
            }
            else
            {
                countViagensTextBlock.Text = "0";
                countEmTransitoTextBlock.Text = "0";
                countPerfeitoTextBlock.Text = "0";
                countPendenciaTextBlock.Text = "0";
                countReEntregaTextBlock.Text = "0";
                countDevolucaoTextBlock.Text = "0";
            }            
        }
        class chartItem
        {
            public int numCarga { set; get; }
            public string placa { set; get; }
            public int emTransito { set; get; }
            public int Perfeito { set; get; }
            public int Pendencia { set; get; }
            public int Devoluçao { set; get; }
            public int Rentrega { set; get; }
        }
    }
}