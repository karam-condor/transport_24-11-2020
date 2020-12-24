using Microsoft.Maps.MapControl.WPF;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace transport
{
    /// <summary>
    /// Interaction logic for GeocodingWindow.xaml
    /// </summary>
    public partial class GeocodingWindow : Window
    {
        bool isGoecoded = false,isRouted = false;
        Object parentWindow;
        int selector;
        Pedido pedido;
        string adress = string.Empty,resultAdress,latitude,longitude;
        int pedido_index;
        List<Point> brazil_polygon;
        MapLayer pushPinLayer;
        string numcar;

        public GeocodingWindow(int selector,Object parentWindow,Pedido pedido,int pedido_index,MapLayer layer,bool isRouted,string numcar)
        {
            InitializeComponent();
            this.parentWindow = parentWindow;
            this.pedido = pedido;
            this.selector = selector;
            this.pedido_index = pedido_index;
            this.pushPinLayer = layer;
            this.isRouted = isRouted;
            this.numcar = numcar;
            if(selector != 0 && selector != 1)
            {
                throw new Exception("Selector take values {0 ,1}");
            }
            if(selector == 0)
            {
                tabControl.SelectedIndex = 0;
            }
            else
            {
                tabControl.SelectedIndex = 1;
            }

        }



        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (isGoecoded == true)
            {
                //update the register in database
                routing2 parentWin = (routing2)parentWindow;
                DBConnection connection = new DBConnection();
                connection.write($"update {tabelas.geoTable} set latitude = :latitude , longitude = :longitude , cor = '#FFFFFF' where numcar = :numcar and numped = :numped", new string[] { ":latitude", ":longitude", ":numcar" , ":numped"}, new string[] { Convert.ToString(parentWin.pedidos[pedido_index].X), Convert.ToString(parentWin.pedidos[pedido_index].Y),numcar ,parentWin.pedidos[pedido_index].numped });
                parentWin.pedidos[pedido_index].cor = "#FFFFFF";
                parentWin.pedidos[pedido_index].X = Methods.doubleParser(latitude);
                parentWin.pedidos[pedido_index].Y = Methods.doubleParser(longitude);
                parentWin.updateDataGridItemSource();
                updatePushpinOnParent();                
                if (isRouted)
                {
                    parentWin.callRoutingThread();
                }                
                onClose();
            }
            else
            {
                MessageBox.Show("Não existe dados para gravar", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GecodeButton_Click(object sender, RoutedEventArgs e)
        {
            callGoogleMap();
        }


        private void updatePushpinOnParent()
        {
            routing2 parentWin = (routing2)parentWindow;
            pushPinLayer.Children.Clear();
            parentWin.drawMarkers(parentWin.pedidos,true);
            parentWin.setPushpinsMouseClick();
            parentWin.setBounds(parentWin.pedidos);
        }

        private void setAdress()
        {
            List<string> strs = new List<string>();
            if(estadoCheckBox.IsChecked == true)
            {
                strs.Add(estadoTextBox.Text);
            }
            if (cidadeCheckBox.IsChecked == true)
            {
                strs.Add(cidadeTextBox.Text);
            }
            if (bairroCheckBox.IsChecked == true)
            {
                strs.Add(bairroTextBox.Text);
            }
            if (complementoCheckBox.IsChecked == true)
            {
                strs.Add(complementoTextBox.Text);
            }
            if (enderecoCheckBox.IsChecked == true)
            {
                strs.Add(enderecoTextBox.Text);
            }
            if (cepCheckBox.IsChecked == true)
            {
                strs.Add(cepTextBox.Text);
            }
            if (obs1CheckBox.IsChecked == true)
            {
                strs.Add(obs1TextBox.Text);
            }
            if (obs2CheckBox.IsChecked == true)
            {
                strs.Add(obs2TextBox.Text);
            }
            if (obs3CheckBox.IsChecked == true)
            {
                strs.Add(obs3TextBox.Text);
            }
            adress = string.Join(" ", strs).Trim();
            if (adress != string.Empty) adress += " Brasil";

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(selector == 0)
            {
                if (pedido != null)
                {
                    setTextBlocks();
                }
                else
                {
                    this.Close();
                }
            }
            setBrazilPolygon();
        }

        private void setBrazilPolygon()
        {
            try
            {
                brazil_polygon = new List<Point>();
                string txt = System.IO.File.ReadAllText("json/country_polygons/brazil.json");
                JObject jObject = JObject.Parse(txt);
                JArray array = (JArray) jObject.SelectToken("geometry.coordinates[0][0]");                
                foreach(var item in array)
                {
                    brazil_polygon.Add(new Point((double)item[0], (double)item[1]));
                }                
            }
            catch
            {

            }            
        }
        private void callBingMap()
        {
            setAdress();
            if(adress != null && adress != string.Empty)
            {
                string link = GeocodeAdress.takeWhiteSpacesUrl("http://dev.virtualearth.net/REST/v1/Locations?q=" + adress + "&key=WfxFWb3dXmy4JnWw1kpG~FX_KaKXZ4wVgHduN5YpQPA~AjA8yf6Z3F4887_kgWPSipd2CFpPUlcyQRROzDqa2bPx0iK9T3BqHL0odYJ80aMD");
                try
                {
                    string jsonStr = new System.Net.WebClient().DownloadString(link);
                    JObject obj = JObject.Parse(jsonStr);
                    if (Convert.ToString(obj.SelectToken("statusCode")) == "200")
                    {
                        latitude = Convert.ToString(obj.SelectToken("resourceSets[0].resources[0].point.coordinates[0]"));
                        longitude = Convert.ToString(obj.SelectToken("resourceSets[0].resources[0].point.coordinates[1]"));                        
                        resultAdress = Methods.encodeUTF8(Convert.ToString(obj.SelectToken("resourceSets[0].resources[0].address.formattedAddress")));
                        resultTextBlock.Text = resultAdress;
                        isGoecoded = true;
                        drawMarker();                        
                    }
                    else
                    {
                        setNoResult();
                    }
                }
                catch
                {
                    setNoResult();
                }
            }
            else
            {
                setNoResult();
            }            
        }


        private void callGoogleMap()
        {
            setAdress();
            if (adress != null && adress != string.Empty)
            {
                string link = GeocodeAdress.takeWhiteSpacesUrl("https://maps.googleapis.com/maps/api/geocode/json?address="+adress+"&key=AIzaSyCMdqUXkGNkpiU0bVc6flJ2QKyDY9FiyyY");
                try
                {
                    string jsonStr = new System.Net.WebClient().DownloadString(link);
                    JObject obj = JObject.Parse(jsonStr);
                    if (Convert.ToString(obj.SelectToken("status")) == "OK")
                    {
                        latitude = Convert.ToString(obj.SelectToken("results[0].geometry.location.lat"));
                        longitude = Convert.ToString(obj.SelectToken("results[0].geometry.location.lng"));
                        resultAdress = Methods.encodeUTF8(Convert.ToString(obj.SelectToken("results[0].formatted_address")));
                        resultTextBlock.Text = resultAdress;
                        isGoecoded = true;
                        drawMarker();
                    }
                    else
                    {
                        setNoResult();
                    }
                }
                catch
                {
                    setNoResult();
                }
            }
            else
            {
                setNoResult();
            }
        }



        private void setNoResult()
        {
            isGoecoded = false;
            latitude = "-1000";
            longitude = "-1000";
            resultAdress = "";
            MessageBox.Show("Endereço não localizado", "", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void drawMarker()
        {
            try
            {
                mapView.Children.Clear();
                Pushpin pin = new Pushpin();
                pin.Background = Brushes.Black;
                Location location = new Location(Methods.doubleParser(latitude), Methods.doubleParser(longitude));
                pin.Location = location;
                mapView.Children.Add(pin);
                mapView.Center = location;
                mapView.ZoomLevel = 15;
            }
            catch
            {

            }                                           
        }

        private void setTextBlocks()
        {
            infoTextBlock.Text = $"Pedido: {pedido.numped} | NF: {pedido.numnota} | Cliente: {pedido.cliente}";
            estadoTextBox.Text = pedido.uf;
            cidadeTextBox.Text = pedido.cidade;
            cepTextBox.Text = pedido.cep;
            bairroTextBox.Text = pedido.bairro;
            complementoTextBox.Text = pedido.complemento;
            enderecoTextBox.Text = pedido.endereco;
            obs1TextBox.Text = pedido.obs1;
            obs2TextBox.Text = pedido.obs2;
            obs3TextBox.Text = pedido.obs3;
        }


        private void onClose()
        {
            this.Close();
            if (this.parentWindow != null) {
                ((Window)parentWindow).IsEnabled = true;
                ((Window)parentWindow).Focus();
            }                                
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            onClose();
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e, ref TextBox textBox)
        {
            textBox.IsEnabled = true;
        }

        private void checkBox_UnChecked(object sender, RoutedEventArgs e, ref TextBox textBox)
        {
            textBox.IsEnabled = false;
        }

        private void estadoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref estadoTextBox);
        }

        

        private void cidadeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref cidadeTextBox);
        }

        private void bairroCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref bairroTextBox);
        }

        private void complementoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref complementoTextBox);
        }

        private void obs1CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref obs1TextBox);
        }

        private void obs2CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref obs2TextBox);
        }

        private void obs3CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref obs3TextBox);
        }

        private void cepCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref cepTextBox);
        }

        private void enderecoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_Checked(sender, e,ref enderecoTextBox);
        }

        private void estadoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e,ref estadoTextBox);
        }

        private void cidadeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref cidadeTextBox);
        }

        private void enderecoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref enderecoTextBox);
        }

        private void bairroCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref bairroTextBox);
        }

        private void complementoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref complementoTextBox);
        }

        private void cepCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref cepTextBox);
        }

        private void mapView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Location loc = mapView.ViewportPointToLocation(e.GetPosition(this));
            if(loc != null)
            {
                latitude = Convert.ToString(loc.Latitude);
                longitude = Convert.ToString(loc.Longitude);
            }
            drawMarker();
            resultTextBlock.Text = resultAdress;
        }

        private void obs1CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref obs1TextBox);
        }

        private void obs2CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref obs2TextBox);
        }

        private void obs3CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBox_UnChecked(sender, e, ref obs3TextBox);
        }
    }
}
