using Microsoft.Maps.MapControl.WPF;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;

namespace transport
{

    public partial class routing2 : Window
    {
        public Carga carga { get; set; }
        public RoutingWindow routingWindow { get; set; }
        DataTable dtConsultaNotas, dtConsultaGeo, dtConsultaPendencia;
        public Pedido[] pedidos;
        List<Pushpin> pushPinList;        
        int pedido_index;
        int selectedRowIndex;
        bool isGeocoded = false, isRouted = false, tspCanceled = false;



        loadingWindow loading_window;
        //Define map layers
        MapLayer pushPinLayer, polylineLayer, routeLayer, pushPinGeocodingLayer;
        //_______________________Define Threads 
        Thread geoCodingThread, routingThread, revertThread,timerThread;




        private static int timeLimit = 1000;
        int timeInSeconds = 0;
        private string numcar, startingTime = null;
        double lastdist, actualdist;
        DispatcherTimer timer;
        //Define the lock objects
        private readonly object _algorithmLock = new object(), _geocodingThreadLock = new object(), _revertThreadLock = new object(),_timerThreadLock  = new object();
        
        //Genetic algorithm parameters
        private int _destinationCount = 0;
        private const int _populationCount = 300;
        private readonly Pedido _startLocation = new Pedido(-15.804799, -47.967552, 0);

        private readonly object _lock = new object();
        private TravellingSalesmanAlgorithm _algorithm;
        //private Pedido[]  _entregasLocations;        
        //____________________
        private Pedido[] _bestSolutionSoFar;
        private volatile bool _mutateFailedCrossovers = true;
        private volatile bool _mutateDuplicates = true;
        private volatile bool _mustDoCrossovers = true;
        private bool _closing = false;        


        public routing2()
        {
            InitializeComponent();
        }

        //ok
        private void setGeneralValues()
        {
            if (carga != null)
            {
                carregamentoTextBlock.Text = carga.carregamento;
                destinoTextBlock.Text = carga.Destino;
                motoristaTextBlock.Text = carga.Motorista;
                veiculoTextBlock.Text = carga.Veiculo;
                placaTextBlock.Text = carga.Placa;
                entregasTextBlock.Text = carga.entregas;
                itensTextBlock.Text = carga.Itens;
                pesoTextBlock.Text = carga.peso_total;
                VolumeTextBlock.Text = carga.volume_total;
                pedidosTextBlock.Text = carga.pedidos;
                valorTextBlock.Text = carga.vltotal;
            }
        }

        //ok
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (routingWindow != null)
            {
                routingWindow.IsEnabled = true;
                routingWindow.Focus();
            }
        }
        
        


        //ok
        private bool searchNotas()
        {
            DBConnection connection = new DBConnection();
            if (isGeocoded)
            {                
                dtConsultaNotas = connection.readByAdapter(@"SELECT a.numnota,
                                                           a.codcli,
                                                           a.numped,                                                           
                                                           b.cliente,
                                                           (SELECT estado
                                                            FROM pcestado@wint
                                                            WHERE uf = b.estent) uf,
                                                           b.municent cidade,
                                                           b.bairroent bairro,
                                                           b.complementoent,
                                                           b.enderent endereco,
                                                           REGEXP_REPLACE (b.cepent, '[^0-9]', '') cep,
                                                           a.obsentrega1 obs1,
                                                           a.obsentrega2 obs2,
                                                           a.obsentrega3 obs3,
                                                           u.nome rca,
                                                           g.latitude lat,
                                                           g.longitude longt,
                                                           g.cor
                                                    FROM pcpedc@wint a,     
                                                         pcclient@wint b,
                                                         pcusuari@wint u,
                                                         " + tabelas.geoTable + @" g
                                                    WHERE     a.codcli = b.codcli
                                                          AND a.dtcancel IS NULL
                                                          AND a.codusur = u.codusur
                                                          AND g.numped = a.numped
                                                          AND a.condvenda NOT IN(10)
                                                          AND a.origemped IN('T', 'F', 'W')
                                                          AND a.numcar = :numcar
                                                    ORDER BY g.seq", new string[] { ":NUMCAR" }, new string[] { carga.carregamento });
            }
            else
            {
              dtConsultaNotas = connection.readByAdapter(@"SELECT a.numnota,
                                                          a.codcli,
                                                          a.numped,                                                          
                                                          b.cliente,
                                                          (SELECT estado
                                                           FROM pcestado@wint
                                                           WHERE uf = b.estent) uf,
                                                          b.municent cidade,
                                                          b.bairroent bairro,
                                                          b.complementoent,
                                                          b.enderent endereco,
                                                          REGEXP_REPLACE (b.cepent, '[^0-9]', '') cep,
                                                          a.obsentrega1 obs1,
                                                          a.obsentrega2 obs2,
                                                          a.obsentrega3 obs3,
                                                          u.nome rca,
                                                          '-1000' lat,
                                                          '-1000' longt,
                                                          '#FFFFFF' cor
                                                   FROM pcpedc@wint a,
                                                        pcclient@wint b,
                                                        pcusuari@wint u
                                                   WHERE     a.codcli = b.codcli
                                                         AND a.dtcancel IS NULL      
                                                         AND a.codusur = u.codusur
                                                         AND a.condvenda NOT IN (10)
                                                         AND a.origemped IN ('T', 'F', 'W')
                                                         AND a.numcar = :numcar", new string[] { ":NUMCAR" }, new string[] { carga.carregamento });
            }
            
            if (dtConsultaNotas != null && dtConsultaNotas.Rows.Count > 0)
            {
                Methods.columnEncodeUtf8(ref dtConsultaNotas, new string[] { "endereco", "complementoent", "bairro", "cidade", "bairro", "obs1", "obs2", "obs3" });
                dtConsultaPendencia = connection.readByAdapter(@"SELECT a.numnota,                                                                        
                                                                        a.codcli,
                                                                        b.cliente,
                                                                        a.numped,
                                                                        b.estent uf,
                                                                        b.municent cidade,
                                                                        b.bairroent bairro,
                                                                        b.complementoent,
                                                                        b.enderent endereco,
                                                                        b.cepent cep,
                                                                        a.obsentrega1 obs1,
                                                                        a.obsentrega2 obs2,
                                                                        a.obsentrega3 obs3,
                                                                        (SELECT nome
                                                                         FROM pcusuari@wint
                                                                         WHERE codusur = a.codusur) rca,
                                                                        c.codprocess,
                                                                        c.lat,
                                                                        c.longt,
                                                                        c.dtentrega,
                                                                        c.obsent
                                                                 FROM pcpedc@wint a,
                                                                      pcclient@wint b,
                                                                      logtransprocess c     
                                                                 WHERE     a.codcli = b.codcli
                                                                       AND a.numcar = c.numcar
                                                                       AND a.numnota = c.numnota
                                                                       AND a.dtcancel IS NULL      
                                                                       AND a.condvenda NOT IN (10)
                                                                       AND a.origemped IN ('T', 'F', 'W')
                                                                       AND a.numcar IN
                                                                               (SELECT DISTINCT (numcar)
                                                                                FROM logtransprocess
                                                                                WHERE numtransvenda IN
                                                                                          (SELECT numtransvenda
                                                                                           FROM logtransprocess
                                                                                           WHERE cargavincul = :numcar AND status = 2))
                                                                       AND a.numnota IN (SELECT numnota
                                                                                         FROM logtransprocess a
                                                                                         WHERE a.cargavincul = :numcar AND a.status = 2)", new string[] { ":NUMCAR" }, new string[] { carga.carregamento });
                return true;
            }
            else
            {
                MessageBox.Show("Numero de carregamento Invalido", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            return false;
        }

        //ok
        private void notaDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                var ped = (Pedido)e.AddedCells[0].Item;
                Location location = new Location(ped.X, ped.Y);
                setBoundsOneNota(location);
            }
            catch
            {

            }
        }

        //ok
        public void setBoundsOneNota(Location location)
        {
            mapView.Center = location;
            mapView.ZoomLevel = 15;
        }


        

        //ok
        private void geocode(Pedido ped)
        {
            string obs = string.Join(" ", new string[] { ped.obs1, ped.obs2, ped.obs3 });
            if (obs.Trim() == string.Empty)
            {
                ped.cor = "#FFFFFF";
                GeocodeAdress geocodeAdress = new GeocodeAdress(ped.cep, $"{ped.endereco} {ped.bairro} {ped.cidade} {ped.uf}",ped.codcli);
                if (geocodeAdress.getLatitude() != "-1000" && geocodeAdress.getLongitude() != "-1000")
                {
                    ped.X = Convert.ToDouble(geocodeAdress.getLatitude());
                    ped.Y = Convert.ToDouble(geocodeAdress.getLongitude());
                }
            }
            else
            {
                ped.cor = "#F7B501";
                GeocodeCep geocodeCep = new GeocodeCep(obs);
                if (geocodeCep.getLatitude() != "-1000" && geocodeCep.getLongitude() != "-1000")
                {
                    ped.X = Convert.ToDouble(geocodeCep.getLatitude());
                    ped.Y = Convert.ToDouble(geocodeCep.getLongitude());
                    ped.cor = "#2e8b57";
                }
                else
                {
                    GeocodeAdress geocodeAdress = new GeocodeAdress(ped.cep, $"{ped.endereco} {ped.bairro} {ped.cidade} {ped.uf}", ped.codcli) ;
                    if (geocodeAdress.getLatitude() != "-1000" && geocodeAdress.getLongitude() != "-1000")
                    {
                        ped.X = Convert.ToDouble(geocodeAdress.getLatitude());
                        ped.Y = Convert.ToDouble(geocodeAdress.getLongitude());
                    }
                }
            }
            if (ped.X == -1000 || ped.Y == -1000)
            {
                ped.cor = "#d63031";
            }
        }




        //ok
        private void notaDataGrid_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Methods.selectRowByRightClick(ref notaDataGrid, e);
            if (notaDataGrid.SelectedItems != null)
            {
                var ped = (Pedido)notaDataGrid.SelectedItems[0];
                if (ped != null)
                {
                    if (selectedRowIndex > -1)
                    {
                        showGeocodingMenu(ped.index-1);
                    }
                }
            }
        }


        //ok
        public void drawMarkers(Pedido[] pedidos, bool showContent)
        {
            try
            {
                mapView.Children.Remove(pushPinLayer);
            }
            catch
            {

            }
            pushPinLayer = new MapLayer();
            pushPinList = new List<Pushpin>();
            Pushpin pin;
            ControlTemplate template1 = (ControlTemplate)this.FindResource("CondorPushPin");
            ControlTemplate template2 = (ControlTemplate)this.FindResource("customePushpinblack");
            for (int i = -1; i < pedidos.Count(); i++)
            {
                pin = new Pushpin();
                pin.PositionOrigin = PositionOrigin.BottomLeft;

                if (i == -1)
                {
                    pin.Template = template1;
                    pushPinLayer.AddChild(pin, new Location(_startLocation.X, _startLocation.Y));
                }
                else
                {
                    if (pedidos[i].X != -1000 && pedidos[i].Y != -1000)
                    {
                        pin.Template = template2;
                        if(pedidos[i].cor != null)
                        {
                            pin.Background = convertToBrushFromCode(pedidos[i].cor);
                        }
                        else
                        {
                            pin.Background = convertToBrushFromCode("#FFFFFF");
                        }                        
                        if (showContent)
                        {
                            pin.Content = pedidos[i].index;
                        }
                        setPushpinTooltip_index(pedidos[i], pin, pedidos[i].index-1);
                        pushPinLayer.AddChild(pin, new Location(pedidos[i].X, pedidos[i].Y));
                        pushPinList.Add(pin);
                    }
                }
            }
            mapView.Children.Add(pushPinLayer);
        }
        //ok
        public void setPushpinsMouseClick()
        {
            if (pushPinList != null)
            {
                foreach (var pin in pushPinList)
                {
                    pin.MouseRightButtonDown += (sender, e) => Pin_MouseRightButtonDown(sender, e, pin.index);
                }
            }
        }
        //ok
        private void Pin_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e, int index)
        {
            showGeocodingMenu(index);
        }


        //ok
        private void showGeocodingMenu(int index)
        {
            ContextMenu pushpinMenu = new ContextMenu();
            MenuItem geocodeByForm = new MenuItem
            {
                Header = "Goecodificar pelo cep/endereço"
            };
            MenuItem geocodeByMap = new MenuItem
            {
                Header = "Escolher endereço no mapa"
            };
            pushpinMenu.Items.Add(geocodeByForm);
            pushpinMenu.Items.Add(geocodeByMap);
            pushpinMenu.IsOpen = true;

            //set menu item click event
            geocodeByForm.Click += (sender, e) => GeocodeByForm_Click(sender, e, index);
            geocodeByMap.Click += (sender, e) => GeocodeByMap_Click(sender, e, index);
        }


        //ok
        private void GeocodeByMap_Click(object sender, RoutedEventArgs e, int index)
        {
            if (pushPinGeocodingLayer != null && pushPinGeocodingLayer.Children != null)
            {
                pushPinGeocodingLayer.Children.Clear();
            }

            geodcodeByMapGrid.Visibility = Visibility.Visible;
            geocodingMapView.Center = new Location(pedidos[index].X, pedidos[index].Y);
            geocodingMapView.ZoomLevel = 15;
            pedido_index = index;
        }
        //ok
        private void routingZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (pushPinLayer != null && pushPinLayer.Children.Count > 0)
            {
                setBounds(pedidos);
            }
        }
        
        //ok
        private void geocodingMapView_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            geocodingMapView.ZoomLevel = 40;
            Location loc = geocodingMapView.ViewportPointToLocation(e.GetPosition(this));
            //update the register in database
            DBConnection connection = new DBConnection();
            connection.write($"update {tabelas.geoTable} set latitude = :latitude , longitude = :longitude , cor = '#FFFFFF' where numcar = :numcar and numped = :numped", new string[] { ":latitude", ":longitude", ":numcar" , ":numped" }, new string[] { Convert.ToString(loc.Latitude), Convert.ToString(loc.Longitude), carga.carregamento, pedidos[pedido_index].numped });            
            pedidos[pedido_index].X = loc.Latitude;
            pedidos[pedido_index].Y = loc.Longitude;
            pedidos[pedido_index].cor = "#FFFFFF";
            updateDataGridItemSource();
            if (pushPinLayer != null && pushPinLayer.Children != null)
            {
                pushPinLayer.Children.Clear();
            }
            drawMarkers(pedidos, true);
            setPushpinsMouseClick();
            setBounds(pedidos);
            geodcodeByMapGrid.Visibility = Visibility.Hidden;            
            if (isRouted)
            {
                routingThread = new Thread(_RoutingThread);
                routingThread.Start();
            }            
        }


        //ok
        private void cancelGeocodeButton_Click(object sender, RoutedEventArgs e)
        {
            geodcodeByMapGrid.Visibility = Visibility.Hidden;

        }
        //ok
        private void GeocodeByForm_Click(object sender, RoutedEventArgs e, int index)
        {
            GeocodingWindow geocodingWindow = new GeocodingWindow(0, this, pedidos[index], index, pushPinLayer,isRouted, carga.carregamento);
            this.IsEnabled = false;
            geocodingWindow.Show();
            geocodingWindow.Focus();
        }
        //ok
        private void setPushpinTooltip_index(Pedido ped, Pushpin pin, int index)
        {
            pin.index = index;
            pin.X = ped.X;
            pin.Y = ped.Y;
            pin.Title = ped.numped;
            pin.bgColor = ped.cor;
            pin.Description = "Numnota: " + ped.numnota + Environment.NewLine +
                "Cod cliente: " + ped.codcli + Environment.NewLine
                + "Cliente" + ped.cliente + Environment.NewLine + "Estado: " + ped.uf
                + Environment.NewLine + "Cidade: " + ped.cidade + Environment.NewLine +
                "Bairro: " + ped.bairro + Environment.NewLine + "Cep: " + ped.cep
                + Environment.NewLine + "Obs1: " + ped.obs1 + Environment.NewLine
                + "Obs2: " + ped.obs2 + Environment.NewLine + "Obs3: " + ped.obs3 + Environment.NewLine;

            ToolTipService.SetToolTip(pin, new ToolTip()
            {
                DataContext = pin,
                Style = Application.Current.Resources["CustomInfoboxStyle"] as Style
            });
        }
        //ok
        private Brush convertToBrushFromCode(string code)
        {
            var converter = new BrushConverter();
            if (code != null && code != string.Empty)
            {                
                return (Brush)converter.ConvertFromString(code);
            }
            return (Brush)converter.ConvertFromString("#FFFFFF");
        }
        //ok
        public void setBounds(Pedido[] pedidos)
        {
            try
            {
                double min;
                var pedidosX = pedidos.AsEnumerable().Select(x => x.X).ToList();
                var pedidosY = pedidos.AsEnumerable().Select(x => x.Y).ToList();
                foreach (var item in pedidosX)
                {
                    if (Convert.ToString(item) == "-1000")
                    {
                        pedidosX.Remove(item);
                    }
                }
                foreach (var item in pedidosY)
                {
                    if (Convert.ToString(item) == "-1000")
                    {
                        pedidosX.Remove(item);
                    }
                }
                var maxX = Methods.doubleParser(Convert.ToString(pedidos.AsEnumerable().Select(x => x.X).Max()));
                var minX = Methods.doubleParser(Convert.ToString(pedidos.AsEnumerable().Select(x => x.X).Min()));
                var maxY = Methods.doubleParser(Convert.ToString(pedidos.AsEnumerable().Select(x => x.Y).Max()));
                var minY = Methods.doubleParser(Convert.ToString(pedidos.AsEnumerable().Select(x => x.Y).Min()));

                var numLat = Math.Abs(maxX - minX) / maxX;
                var numLongt = Math.Abs(maxY - minY) / maxY;
                var box = new LocationRect(maxX, minY, minX, maxY);
                mapView.SetView(box);
            }
            catch (Exception ex)
            {
                mapView.Center = new Location(-15.7785, -47.9287);
                mapView.ZoomLevel = 9;
            }
        }
        //ok
        private void drawPolyLine(Pedido[] locs)
        {
            try
            {
                mapView.Children.Remove(polylineLayer);
            }
            catch
            {

            }
            polylineLayer = new MapLayer();
            MapPolyline polyline;
            LocationCollection collection;
            Color color;
            int red, blue, c = locs.Count() - 1;
            for (int i = -1; i < locs.Count(); i++)
            {
                red = 255 * (i + 1) / locs.Length;
                blue = 255 - red;
                polyline = new MapPolyline();
                collection = new LocationCollection();
                color = System.Windows.Media.Color.FromRgb((byte)red, 0, (byte)blue);
                polyline.Stroke = new SolidColorBrush(color);
                polyline.StrokeThickness = 2;
                polyline.Opacity = 0.7;
                if (i == -1)
                {
                    collection.Add(new Location(_startLocation.X, _startLocation.Y));
                    collection.Add(new Location(locs[0].X, locs[0].Y));
                }
                else if (i == c)
                {
                    collection.Add(new Location(locs[c].X, locs[c].Y));
                    collection.Add(new Location(_startLocation.X, _startLocation.Y));
                }
                else
                {
                    collection.Add(new Location(locs[i].X, locs[i].Y));
                    collection.Add(new Location(locs[i + 1].X, locs[i + 1].Y));
                }
                polyline.Locations = collection;
                polylineLayer.Children.Add(polyline);
            }
            mapView.Children.Add(polylineLayer);
        }


        //ok
        private void drawRouteLines(JArray arr)
        {
            if (arr != null && arr.Count > 0)
            {
                if(routeLayer != null)
                {
                    routeLayer.Children.Clear();
                }
                routeLayer = new MapLayer();
                MapPolyline mp = new MapPolyline();
                mp.Stroke = Brushes.Black;
                mp.Opacity = 0.65;
                mp.StrokeThickness = 5;
                LocationCollection collection = new LocationCollection();
                Location loc;
                for (int i = 0; i < arr.Count; i++)
                {
                    var ob = JObject.Parse(Convert.ToString(arr[i]));
                    var lineArr = JArray.Parse(Convert.ToString(ob.SelectToken("resourceSets[0].resources[0].routePath.line.coordinates")));
                    foreach (var obj in lineArr)
                    {
                        loc = new Location((double)obj[0], (double)obj[1]);
                        collection.Add(loc);
                    }
                }
                mp.Locations = collection;
                routeLayer.Children.Add(mp);
                mapView.Children.Add(routeLayer);
            }
        }
        //ok
        public void updateDataGridItemSource()
        {
            notaDataGrid.ItemsSource = null;
            notaDataGrid.ItemsSource = pedidos;
            notaDataGrid.Columns[17].Visibility = Visibility.Hidden;
            notaDataGrid.Columns[18].Visibility = Visibility.Hidden;            
        }

        //ok
        private bool checkBackGroundWorkInGoing()
        {
            bool check1 = false, check2 = false, check3 = false;
            if (geoCodingThread != null)
            {
                check1 = geoCodingThread.IsAlive;
            }
            if (routingThread != null)
            {
                check2 = routingThread.IsAlive;
            }
            if (revertThread != null)
            {
                check3 = revertThread.IsAlive;
            }
            if (check1 == true || check2 == true || check3 == true)
            {
                return true;
            }

            return false;
        }
        //ok
        private void disableEnableMapManipulation(bool check)
        {
            if (check)
            {
                mapView.SupportedManipulations = System.Windows.Input.Manipulations.Manipulations2D.None;
            }
            else
            {
                mapView.SupportedManipulations = System.Windows.Input.Manipulations.Manipulations2D.All;
            }
        }

        //show the total distance on the map
        //ok
        private void showDistance(double distance)
        {
            distanceText.Text = $"Distancia total: {Math.Round(distance,3)} km";
        }


        private void setTimer()
        {
            timeInSeconds = 0;            
            timerThread = new Thread(timer_tick);
            timerThread.Start();
            //timer = new DispatcherTimer();
            //timer.Tick += new EventHandler(timer_tick);
            //timer.Interval = new TimeSpan(0, 0, 1);
            //timer.Start();
        }
        private void timer_tick()
        {
            lock (_timerThreadLock)
            {
                while(timeInSeconds < timeLimit && routingThread != null && routingThread.IsAlive)
                {
                    Thread.Sleep(1000);
                    timeInSeconds++;                    
                }                                
            }            
        }

        
        //ok
        private void generateDistanceMatrix()
        {
            if (pedidos.Count() < 50)
            {
                Pedido.distanceMatrix = MapsRequest.generateDistanceMatrix(_startLocation, pedidos);
                if (Pedido.distanceMatrix == null)
                {
                    Methods.useDistanceMatrix = false;
                }
                else
                {
                    Methods.useDistanceMatrix = true;
                }
            }
        }      

        //ok
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _closing = true;

            lock (_lock)
                Monitor.Pulse(_lock);
            loading_window.Close();
        }

        //ok
        private void showLoadingWindow(bool showCancelButton)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                loading_window = new loadingWindow(showCancelButton, this);
                //this.IsEnabled = false;
                loading_window.Show();
                loading_window.Focus();
            }), DispatcherPriority.ApplicationIdle);
        }

        //ok
        private void hideLoadWindow()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (loading_window != null)
                    {
                        this.IsEnabled = true;
                        loading_window.Close();
                    }
                }), DispatcherPriority.ApplicationIdle);
            }
            catch
            {

            }            
        }

        private void routingRoutingButton_Click(object sender, RoutedEventArgs e)
        {
            if(checkBackGroundWorkInGoing() == false)
            {
                if(timer != null)
                {
                    timer.Stop();
                    timer = null;
                }
                routingThread = new Thread(_RoutingThread);
                routingThread.Start();
            }
        }

        private void regocodingSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if(checkBackGroundWorkInGoing() == false)
            {
                isGeocoded = false;
                if(polylineLayer != null)
                {
                    mapView.Children.Remove(polylineLayer);
                }
                if (routeLayer != null)
                {
                    mapView.Children.Remove(routeLayer);
                }
                geoCodingThread = new Thread(_GoecodedThread);
                geoCodingThread.Start();
                isRouted = false;
                DBConnection connection = new DBConnection();
                connection.write($"delete from {tabelas.geoTable}  where numcar = :numcar", new string[] { ":numcar" }, new string[] { carga.carregamento });
            }
        }

        private void routingReverseButton_Click(object sender, RoutedEventArgs e)
        {
            if (checkBackGroundWorkInGoing()== false && isGeocoded && isRouted)
            {
                revertThread = new Thread(_RevertThread);
                revertThread.Start();
            }            
        }

        //ok
        private bool checkAllPedidosGeocoded()
        {
            foreach (var item in pedidos)
            {
                if (item.X == -1000 || item.Y == -1000)
                {
                    return false;                    
                }
            }
            return true;
        }
        //ok
        private void checkCargaGeocoded()
        {
            DBConnection connection = new DBConnection();
            dtConsultaGeo = connection.readByAdapter($"select * from {tabelas.geoTable} where numcar = :numcar", new string[] { ":numcar" }, new string[] { carga.carregamento });
            if (dtConsultaGeo != null && dtConsultaGeo.Rows.Count > 0)
            {
                isGeocoded = true;
            }
            else
            {
                isGeocoded = false;
            }            
        }

        private void checkCargaRouted() {
            isRouted = true;
            if (dtConsultaGeo != null && dtConsultaGeo.Rows.Count > 0)
            {
                foreach(DataRow dr in dtConsultaPendencia.Rows)
                {
                    if (Convert.ToString(dr["seq"]).Trim() == string.Empty)
                    {
                        isRouted = false;
                    }
                }
            }
            else
            {
                isRouted = false;
            }
        }

        //ok
        public void cancelTSP()
        {
            tspCanceled = true;
        }

        //ok
        public void saveRoutingResult()
        {
            DBConnection connection = new DBConnection();
            for (int i = 1; i <= pedidos.Length; i++)
            {
                connection.write($"update {tabelas.geoTable} set seq = :seq where numcar = :numcar and numped = :numped", new string[] { ":seq", ":numcar" , ":numped"},new string[] { i.ToString(), carga.carregamento, pedidos[i-1].numped});
            }
        }



        public partial class Pushpin : Microsoft.Maps.MapControl.WPF.Pushpin
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public int index { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public string bgColor { get; set; }
        }




        //_________________________________________________________________________________________
        //not ok
        private void generateLocations()
        {
            int count1 = dtConsultaNotas.Rows.Count;
            int count2 = dtConsultaPendencia.Rows.Count;
            string obs = string.Empty;            
            //should see this part            
            int c = 0;
            _destinationCount = count1 + count2;            
            pedidos = new Pedido[_destinationCount];
            for (int i = 0; i < count1; i++)
            {
                pedidos[i] = new Pedido(Convert.ToString(dtConsultaNotas.Rows[i]["NUMNOTA"]), Convert.ToString(dtConsultaNotas.Rows[i]["CODCLI"]),
                    Convert.ToString(dtConsultaNotas.Rows[i]["NUMPED"]), Convert.ToString(dtConsultaNotas.Rows[i]["CLIENTE"]),
                    Convert.ToString(dtConsultaNotas.Rows[i]["UF"]), Convert.ToString(dtConsultaNotas.Rows[i]["CIDADE"]), Convert.ToString(dtConsultaNotas.Rows[i]["BAIRRO"]),
                    Convert.ToString(dtConsultaNotas.Rows[i]["ENDERECO"]), Convert.ToString(dtConsultaNotas.Rows[i]["CEP"]), Convert.ToString(dtConsultaNotas.Rows[i]["OBS1"]),
                    Convert.ToString(dtConsultaNotas.Rows[i]["OBS2"]), Convert.ToString(dtConsultaNotas.Rows[i]["OBS3"]), Convert.ToString(dtConsultaNotas.Rows[i]["RCA"]), Methods.doubleParser(Convert.ToString(dtConsultaNotas.Rows[i]["LAT"])),
                    Methods.doubleParser(Convert.ToString(dtConsultaNotas.Rows[i]["LONGT"])), 1, i + 1,
                    Convert.ToString(dtConsultaNotas.Rows[i]["complementoent"]), Convert.ToString(dtConsultaNotas.Rows[i]["COR"]));
            }
            //for (int i = 0; i < count2; i++)
            //{
            //    c = i + count1;
            //    pedidos[c] = new Pedido(Convert.ToString(dtConsultaPendencia.Rows[i]["NUMNOTA"]), Convert.ToString(dtConsultaPendencia.Rows[i]["CODCLI"]),
            //        Convert.ToString(dtConsultaPendencia.Rows[i]["NUMPED"]), Convert.ToString(dtConsultaPendencia.Rows[i]["CLIENTE"]),
            //        Convert.ToString(dtConsultaPendencia.Rows[i]["UF"]), Convert.ToString(dtConsultaPendencia.Rows[i]["CIDADE"]), Convert.ToString(dtConsultaPendencia.Rows[i]["BAIRRO"]),
            //        Convert.ToString(dtConsultaPendencia.Rows[i]["ENDERECO"]), Convert.ToString(dtConsultaPendencia.Rows[i]["CEP"]), Convert.ToString(dtConsultaPendencia.Rows[i]["OBS1"]),
            //        Convert.ToString(dtConsultaPendencia.Rows[i]["OBS2"]), Convert.ToString(dtConsultaPendencia.Rows[i]["OBS3"]), Convert.ToString(dtConsultaPendencia.Rows[i]["RCA"]), Methods.doubleParser(Convert.ToString(dtConsultaPendencia.Rows[i]["LAT"])),
            //        Methods.doubleParser(Convert.ToString(dtConsultaPendencia.Rows[i]["LONGT"])), 2, c + 1, Convert.ToString(dtConsultaNotas.Rows[i]["complementoent"]));
            //}

            //Geocoding orders
            //chcek if is already geocoded then wont call the map
            if(isGeocoded == false)
            {
                foreach (Pedido ped in pedidos)
                {
                    geocode(ped);
                }
            }            

            //drawing the markers
            Dispatcher.BeginInvoke(new Action(() => drawMarkers(pedidos, true)), DispatcherPriority.ApplicationIdle);
            //add pushpins mouse click events
            Dispatcher.BeginInvoke(new Action(() => setPushpinsMouseClick()), DispatcherPriority.ApplicationIdle);
            //Zooming
            Dispatcher.BeginInvoke(new Action(() => setBounds(pedidos)), DispatcherPriority.ApplicationIdle);

            //save pedidos
            //chcek if is already geocoded then wont save nothing
            if(isGeocoded == false)
            {
                DBConnection connection = new DBConnection();
                int x = 0;
                foreach (var ped in pedidos)
                {
                    connection.write($"insert into {tabelas.geoTable} (id,numcar,numnota,numped,codcli,latitude,longitude,tiponota,cor) values ({tabelas.seqIdGeo}.nextval,:numcar,:numnota,:numped,:codcli,:latitude,:longitude,:tipo,:cor)", new string[] { ":numcar", ":numnota", ":numped" , ":codcli", ":latitude", ":longitude" ,":tipo",":cor"}, new string[] { carga.carregamento, ped.numnota, ped.numped, ped.codcli, Convert.ToString(ped.X), Convert.ToString(ped.Y) , Convert.ToString(ped.tipo) , Convert.ToString(ped.cor) });
                }
            }            
            Dispatcher.BeginInvoke(new Action(() => {
                notaDataGrid.ItemsSource = pedidos;
                notaDataGrid.Columns[17].Visibility = Visibility.Hidden;
                notaDataGrid.Columns[18].Visibility = Visibility.Hidden;                
            }), DispatcherPriority.ApplicationIdle);
        }

        private void drawRouteReverse()
        {
            JArray PathArray = MapsRequest.generateRoute(_startLocation, pedidos, startingTime, carga.carregamento);
            drawRouteLines(PathArray);
        }

        //not ok
        private void drawRoute()
        {
            try
            {
                mapView.Children.Remove(routeLayer);
            }
            catch
            {

            }
            if (isRouted)
            {
                try
                {
                    string jsonStr = System.IO.File.ReadAllText($"json/map_data/{carga.carregamento}.json");
                    JArray arr = JArray.Parse(jsonStr);
                    drawRouteLines(arr);
                }
                catch
                {

                }                
            }
            else
            {
                JArray PathArray = MapsRequest.generateRoute(_startLocation, _bestSolutionSoFar, startingTime, carga.carregamento);
                drawRouteLines(PathArray);                
            }
            isRouted = true;
        }
               
        //not ok
        private void _RoutingThread()
        {
            if (checkAllPedidosGeocoded())
            {                 
                if(routeLayer != null)
                {                    
                    Dispatcher.BeginInvoke(new Action(() => routeLayer.Children.Clear()), DispatcherPriority.ApplicationIdle);
                }
                setTimer();
                showLoadingWindow(true);
                generateDistanceMatrix();
                _algorithm = new TravellingSalesmanAlgorithm(_startLocation, pedidos, _populationCount,isRouted);
                if (isRouted)
                {
                    isRouted = false;                    
                }
                _bestSolutionSoFar = _algorithm.GetBestSolutionSoFar().ToArray();
                Dispatcher.BeginInvoke(new Action(() => drawPolyLine(_bestSolutionSoFar)), DispatcherPriority.ApplicationIdle);
                Dispatcher.BeginInvoke(new Action(() => drawMarkers(_bestSolutionSoFar, true)), DispatcherPriority.ApplicationIdle);
                Dispatcher.BeginInvoke(new Action(() => setBounds(_bestSolutionSoFar)), DispatcherPriority.ApplicationIdle);
                int counter = 0;
                int limit = _bestSolutionSoFar.Count() * 30;
                timeLimit = _bestSolutionSoFar.Count() * 5;
                if (limit < 200)
                {
                    limit = 400;
                }                
                while ((counter < limit) && timeInSeconds < timeLimit && !_closing && tspCanceled == false)
                {                    
                    lock (_algorithmLock)
                    {                                                                       
                        _algorithm.MustMutateFailedCrossovers = _mutateFailedCrossovers;
                        _algorithm.MustDoCrossovers = _mustDoCrossovers;
                        _algorithm.Reproduce();
                        if (_mutateDuplicates)
                            _algorithm.MutateDuplicates();

                        var newSolution = _algorithm.GetBestSolutionSoFar().ToArray();
                        if (!newSolution.SequenceEqual(_bestSolutionSoFar))
                        {
                            lock (_lock)
                            {
                                _bestSolutionSoFar = newSolution;                                
                                pedidos = _bestSolutionSoFar;
                                actualdist = (Pedido.GetTotalDistanceKM(_startLocation, _bestSolutionSoFar));
                                if (Math.Round(actualdist,3) == Math.Round(lastdist,3))
                                {                                    
                                    counter = counter + 2;
                                }
                                else
                                {
                                    counter++;
                                }
                                updatePedidosIndex();
                                Dispatcher.BeginInvoke(new Action(() => drawPolyLine(pedidos)), DispatcherPriority.ApplicationIdle);
                                Dispatcher.BeginInvoke(new Action(() => drawMarkers(pedidos, true)), DispatcherPriority.ApplicationIdle);
                                Dispatcher.BeginInvoke(new Action(() => showDistance(actualdist)), DispatcherPriority.ApplicationIdle);
                            }
                            //Thread.Sleep(200);                            
                        }
                        lastdist = actualdist;
                    }
                }
                saveRoutingResult();
                Dispatcher.BeginInvoke(new Action(() => drawRoute()), DispatcherPriority.ApplicationIdle);
                Dispatcher.BeginInvoke(new Action(() => setPushpinsMouseClick()), DispatcherPriority.ApplicationIdle);
                Dispatcher.BeginInvoke(new Action(() => updateDataGridItemSource()), DispatcherPriority.ApplicationIdle);
                tspCanceled = false;
                hideLoadWindow();
            }
            else
            {
                MessageBox.Show("Não todos os pedidos são geo-localizados", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        private void updatePedidosIndex()
        {
            for (int i = 0; i < pedidos.Length; i++)
            {
                pedidos[i].index = i + 1;
            }
        }

        private void _RoutingThreadRouted()
        {
            showLoadingWindow(false);
            //Dispatcher.BeginInvoke(new Action(() => drawPolyLine(pedidos)), DispatcherPriority.ApplicationIdle);
            Dispatcher.BeginInvoke(new Action(() => drawRoute()), DispatcherPriority.ApplicationIdle);
            Dispatcher.BeginInvoke(new Action(() => setPushpinsMouseClick()), DispatcherPriority.ApplicationIdle);
            hideLoadWindow();
        }

        //not ok
        private void _RevertThread()
        {
            lock (_revertThreadLock)
            {                                
                Array.Reverse(pedidos);
                updatePedidosIndex();
                Dispatcher.BeginInvoke(new Action(() => drawMarkers(pedidos,true)), DispatcherPriority.ApplicationIdle);
                Dispatcher.BeginInvoke(new Action(() => drawPolyLine(pedidos)), DispatcherPriority.ApplicationIdle);
                Dispatcher.BeginInvoke(new Action(() => setPushpinsMouseClick()), DispatcherPriority.ApplicationIdle);
                saveRoutingResult();
                Dispatcher.BeginInvoke(new Action(() => drawRouteReverse()), DispatcherPriority.ApplicationIdle);
                hideLoadWindow();     
            }
        }

        private void _GoecodedThread()
        {
            lock (_geocodingThreadLock)
            {
                showLoadingWindow(false);
                if (carga != null)
                {
                    if (searchNotas())
                    {
                        generateLocations();                        
                    }
                    //set the carga as geocoded in the runtime condition
                    isGeocoded = true;
                    //check if the carga is routed
                    checkCargaRouted();
                    if (isRouted)
                    {
                        routingThread = new Thread(_RoutingThreadRouted);
                        routingThread.Start();
                    }
                }
                else
                {
                    this.Close();
                }
                hideLoadWindow();
            }
        }


        public void callRoutingThread()
        {
            if(checkBackGroundWorkInGoing() == false)
            routingThread = new Thread(_RoutingThread);
            routingThread.Start();
        }

        //________________________________________________________________


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //set carga
            setGeneralValues();
            //check if the carga is geocoded
            checkCargaGeocoded();
            geoCodingThread = new Thread(_GoecodedThread);
            geoCodingThread.Start();
        }
    }
}
