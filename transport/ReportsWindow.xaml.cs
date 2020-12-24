using Microsoft.Maps.MapControl.WPF;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;


namespace transport
{
    /// <summary>
    /// Interaction logic for ReportsWindow.xaml
    /// </summary>
    public partial class ReportsWindow : Window
    {
        Boolean isplaying = false;
        Boolean isStopped = true;
        DataTable cargaTable,notaTable,prodTable,notaTable2;
        int selectedNota=-1;
        int counter = 0;
        MediaPlayer mediaPlayer;
        DispatcherTimer timer;
        bool pendencia_user_check = false;
        string numcar = "";
        public ReportsWindow()
        {
            InitializeComponent();
            perfeitoCheckBox.Checked += setCheckBoxesEnable;
            devolucaoTotalCheckBox.Checked += setCheckBoxesEnable;
            reentregaCheckBox.Checked += setCheckBoxesEnable;
            devolucaoParcialCheckBox.Checked += setCheckBoxesEnable;
            perfeitoCheckBox.Unchecked += setCheckBoxesEnable;
            devolucaoTotalCheckBox.Unchecked += setCheckBoxesEnable;
            reentregaCheckBox.Unchecked += setCheckBoxesEnable;
            devolucaoParcialCheckBox.Unchecked += setCheckBoxesEnable;
        }


        private void textJustNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            Methods.acceptJustNumbers(sender, e);
        }

        


  

        
        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if(cargaTextBox.Text != string.Empty)
            {
                DBConnection connection = new DBConnection();
                cargaTable = connection.readByAdapter("select A.NUMCAR,A.CODMOTORISTA,B.NOME MOTORISTA, A.KM,A.DTSAIDA,A.DTFINAL from "+tabelas.cargaTable+" A, PCEMPR@WINT B, PCVEICUL@WINT C where A.codmotorista = B.matricula AND NUMCAR = :NUMCAR GROUP BY  A.NUMCAR,A.CODMOTORISTA,B.NOME,A.KM,A.DTSAIDA,A.DTFINAL", new string[] { ":NUMCAR" }, new string[] { cargaTextBox.Text });
                connection = new DBConnection();
                notaTable = connection.readByAdapter("select CODPROCESS,case when status = 0 then ''  when status = 1  then 'Perfeito' when status = 2  then 'Pendencia ou devolução parcial' when status = 3 then 'Devolução' when status = 4 then 'Re-entrega' end as status,NUMNOTA,LAT,LONGT,DTENTREGA DT_ENTREGA,OBSENT OBS_ENTREGA,case when status = 1 then '' else  to_char(CODMOTIVO) end COD_MOTIVO, case when status = 1 then '' else B.MOTIVO end MOTIVO,DTSTATUS1 DT_EMISSAO,DTSTATUS2,DTSTATUS3,DTSTATUS4,CARGAVINCUL,CASE WHEN  STCRED=1  THEN 'Gerar crédito' ELSE '' END AS CREDITO  FROM "+tabelas.processTable+" A, PCTABDEV@WINT B WHERE A.NUMCAR = :NUMCAR AND A.CODMOTIVO = B.CODDEVOL order by DTENTREGA", new string[] { ":NUMCAR" },new string[] { cargaTextBox.Text});
                
                if(cargaTable != null && cargaTable.Rows.Count > 0)
                {
                    numcar = cargaTextBox.Text;
                    codMotoTextBox.Text = Convert.ToString(cargaTable.Rows[0]["CODMOTORISTA"]);
                    motoTextBox.Text = Convert.ToString(cargaTable.Rows[0]["MOTORISTA"]);
                    kmTextBox.Text = Convert.ToString(cargaTable.Rows[0]["KM"]);
                    dtIncialTextBox.Text = Convert.ToString(cargaTable.Rows[0]["DTSAIDA"]);
                    dtFinalTextBox.Text = Convert.ToString(cargaTable.Rows[0]["DTFINAL"]);
                }
                else
                {
                    codMotoTextBox.Text = "";
                    motoTextBox.Text = "";
                    kmTextBox.Text = "";
                    dtIncialTextBox.Text = "";
                    dtFinalTextBox.Text = "";
                }
                
                if(notaTable != null && notaTable.Rows.Count > 0)
                {                    
                    DataTable dt = notaTable.DefaultView.ToTable(false, new string[] { "NUMNOTA", "STATUS", "DT_ENTREGA", "OBS_ENTREGA", "COD_MOTIVO" , "MOTIVO" , "DT_EMISSAO","CREDITO"});
                    notaDataGrid.ItemsSource = dt.DefaultView;
                    ControlTemplate template = (ControlTemplate)this.FindResource("CutomPushpinTemplate2");
                    for(int i = 0; i< notaTable.Rows.Count; i++)
                    {
                        Location location = new Location(Methods.doubleParser(Convert.ToString(notaTable.Rows[i]["LAT"])), Methods.doubleParser(Convert.ToString(notaTable.Rows[i]["LONGT"])));
                        drawPushPin(location, template,(i+1).ToString());
                    }
                    setBounds(notaTable);
                }
                else
                {
                    clearMap();
                    notaDataGrid.ItemsSource = new DataTable().DefaultView;
                }
            }            
        }

        private void drawPushPin(Location location, ControlTemplate template, String dtentrega)
        {
            Pushpin pin = new Pushpin();
            pin.Location = location;
            pin.Template = template;
            pin.PositionOrigin = PositionOrigin.Center;
            pin.Content = dtentrega;
            mapView.Children.Add(pin);
        }

        private void obsMotoTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (obsMotoTextBox.Text != "")
            {
                MessageBox.Show(obsMotoTextBox.Text, "", MessageBoxButton.OK, MessageBoxImage.Information);
            }            
        }

        private void obsCliTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (obsCliTextBox.Text != "")
            {
                MessageBox.Show(obsCliTextBox.Text, "", MessageBoxButton.OK, MessageBoxImage.Information);
            }            
        }



        private void searchButtonNotas_Click(object sender, RoutedEventArgs e)
        {
            if(notaTextBox.Text != "" || processTextBox.Text != "" || dtIncialPicker.SelectedDate.Value != null || dtFinalPicker.SelectedDate.Value != null)
            {
                stopAudio();
                DBConnection connection = new DBConnection();                
                notaTable2 = connection.readByAdapter("select a.USUEMISSAO Usuario_Emissao_Mapa,a.USUVINCUL Usuario_Vinculacao_Carga , a.NUMTRANSVENDA,a.CODPROCESS Cod_Entrega,case when a.status = 0 then ''  when a.status = 1  then 'Perfeito' when a.status = 2  then 'Pendencia ou devolução parcial' when a.status = 3 then 'Devolução' when a.status = 4 then 'Re-entrega' end as status,a.NUMCAR N_CARREGAMENTO, a.NUMNOTA NF,a.LAT,a.LONGT,a.DTENTREGA DT_ENTREGA,a.EMAIL_CLIENTE, a.OBSENT OBS_ENTREGA,a.AVALICAO,a.CLICOMENT,case when a.status = 1 then '' when a.status = 2 then '' else to_char(CODMOTIVO) end COD_MOTIVO,case when a.status = 1 then '' when a.status = 2 then '' else (select b.motivo from pctabdev@wint b where b.coddevol = a.codmotivo) end MOTIVO,a.DTSTATUS1 DT_EMISSAO, a.DTSTATUS2 DT_VINCULACAO, (select a.dtstatus2 from " + tabelas.processTable+" a where  a.codprocess IN (SELECT codprocess FROM (SELECT a.codprocess, ROWNUM num FROM "+tabelas.processTable+ " a WHERE a.numtransvenda = NUMTRANSVENDA ORDER BY a.codprocess DESC) WHERE num = 2))  DT_Entrega_Pendencia, a.DTSTATUS4, a.CARGAVINCUL Carregamento_Vinculado,CASE WHEN  a.STCRED=1  THEN 'Gerar crédito' ELSE '' END AS CREDITO ,b.codcli,b.cliente,b.fantasia,b.estent uf,b.municent cidade,b.enderent endereco,b.complementoent complemento,b.bairroent bairro,b.cepent cep,b.obsentrega1 obs1,b.obsentrega2 obs2,b.obsentrega3 obs3,c.numped,d.nome rca FROM " + tabelas.processTable+ " A,pcclient@wint b,pcnfsaid@wint c,pcusuari@wint d WHERE A.NUMNOTA LIKE CASE WHEN :NUMNOTA IS NULL THEN '%' ELSE :NUMNOTA END AND  A.NUMCAR LIKE CASE WHEN :NUMCAR IS NULL THEN '%' ELSE :NUMCAR END   AND A.CODPROCESS LIKE CASE WHEN :CODPROCESS IS NULL THEN '%' ELSE: CODPROCESS END AND A.CODPROCESS LIKE CASE WHEN :CODPROCESS IS NULL THEN '%' ELSE: CODPROCESS END  AND A.DTENTREGA >= TO_DATE(:DTINCIAL, 'YYYY/MM/DD HH24:MI:SS') AND A.DTENTREGA <= TO_DATE(:DTFINAL,'YYYY/MM/DD HH24:MI:SS') AND A.STATUS IN(" + getStatusConditions() + ") "+ getNOtaEmitionConditions() +getNotaLinkedStatus()+ " AND a.numtransvenda = c.numtransvenda AND b.codcli = c.codcli AND d.codusur = c.codusur order by A.DTENTREGA", new string[] {":NUMCAR",":NUMNOTA" , ":CODPROCESS" , ":DTINCIAL", ":DTFINAL" }, new string[] {carga2TextBox.Text, notaTextBox.Text, processTextBox.Text,dtIncialPicker.SelectedDate.Value.ToString("yyyy/MM/dd"), dtFinalPicker.SelectedDate.Value.AddDays(1).ToString("yyyy/MM/dd")});
                if(notaTable2 != null && notaTable2.Rows.Count > 0)
                {
                    prodDataGrid.ItemsSource = new DataTable().DefaultView;
                    DataTable dt = notaTable2.DefaultView.ToTable(false, new string[] { "Cod_Entrega", "N_CARREGAMENTO", "NF", "STATUS", "DT_ENTREGA", "OBS_ENTREGA", "COD_MOTIVO", "MOTIVO", "DT_EMISSAO", "Usuario_Emissao_Mapa", "Carregamento_Vinculado", "DT_VINCULACAO", "Usuario_Vinculacao_Carga", "CREDITO", "DT_Entrega_Pendencia" });
                    notaDataGrid2.ItemsSource = dt.DefaultView;
                    setCountNotas(notaTable2);
                    setLayoutValues(Convert.ToString(notaTable2.Rows[0]["OBS_ENTREGA"]), Convert.ToString(notaTable2.Rows[0]["CLICOMENT"]), Convert.ToString(notaTable2.Rows[0]["EMAIL_CLIENTE"]), notaTable2.Rows[0]["AVALICAO"] == DBNull.Value ? 0 : Convert.ToInt32(notaTable2.Rows[0]["AVALICAO"]));
                }
                else
                {
                    notaDataGrid2.ItemsSource = new DataTable().DefaultView;
                    setCountNotas(notaTable2);
                    setLayoutValues("", "", "", 0);
                }
                               
            }            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Configure layout
            dtIncialPicker.SelectedDate = DateTime.Now;
            dtFinalPicker.SelectedDate = DateTime.Now;
            buttonPlay.Content = FindResource("play");
            //Configure permissions
            switch (Methods.loginType)
            {
                case "TR":
                    buttonVincularCarga.IsEnabled = true;
                    break;
                case "PD":
                    pendencia_user_check = true;
                    break;
                case "AD":
                    buttonVincularCarga.IsEnabled = true;
                    pendencia_user_check = true;
                    break;
            }
        }

        
       

        private void clearMap()
        {
            mapView.Children.Clear();
        }

        private void notaDataGrid2_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {                        
            clearMap();
            if (notaDataGrid2.Items.Count > 0 && notaDataGrid2.SelectedIndex > -1)
            {
                stopAudio();
                selectedNota = notaDataGrid2.SelectedIndex;                
                setLayoutValues(Convert.ToString(notaTable2.Rows[selectedNota]["OBS_ENTREGA"]), Convert.ToString(notaTable2.Rows[selectedNota]["CLICOMENT"]), Convert.ToString(notaTable2.Rows[selectedNota]["EMAIL_CLIENTE"]), notaTable2.Rows[selectedNota]["AVALICAO"] == DBNull.Value ? 0 : Convert.ToInt32(notaTable2.Rows[selectedNota]["AVALICAO"]));
                try
                {
                    ControlTemplate template = (ControlTemplate)this.FindResource("CutomPushpinTemplate2");
                    Location location = new Location(Methods.doubleParser(Convert.ToString(notaTable2.Rows[selectedNota]["LAT"])), Methods.doubleParser(Convert.ToString(notaTable2.Rows[selectedNota]["LONGT"])));
                    drawPushPin(location, template, Convert.ToString(notaTable2.Rows[selectedNota]["NF"]));
                    setBoundsOneNota(location);
                } catch (Exception ex){}                
                DBConnection connection = new DBConnection();
                prodTable = connection.readByAdapter("SELECT A.CODPROD , B.DESCRICAO , A.QT , A.CODMOTIVO , C.MOTIVO ,CASE WHEN A.STDEV = 1 THEN 'PENDENCIA' ELSE  'DEVOLUÇÃO' END AS STATUS  FROM "+tabelas.prodTable+" A,PCPRODUT@WINT B,PCTABDEV@WINT C  WHERE A.CODPROCESS = :CODPROCESS AND A.CODPROD = B.CODPROD AND A.CODMOTIVO = C.CODDEVOL", new string[] { ":CODPROCESS" }, new string[] { Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"])});
                //here                
                if (prodTable != null && prodTable.Rows.Count > 0)
                {                    
                    prodDataGrid.ItemsSource = prodTable.DefaultView;
                    setCountProds(prodTable);
                }
                else
                {
                    prodDataGrid.ItemsSource = new DataTable().DefaultView;
                    setCountProds(prodTable);
                }
            }
        }

        private void buttonVincularCarga_Click(object sender, RoutedEventArgs e)
        {
            if(cargaVinculTextBox.Text != "")
            {
                if (selectedNota > -1)
                {
                    MessageBoxResult result= MessageBox.Show("Confirma Vincular a nota fiscal: " + Convert.ToString(notaTable2.Rows[selectedNota]["NF"]) + " com o carregamento N° " + cargaVinculTextBox.Text, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if(result == MessageBoxResult.Yes)
                    {
                        DBConnection connection = new DBConnection();
                        DataTable dt = connection.readByAdapter("SELECT * FROM "+tabelas.prodTable+" WHERE CODPROCESS = :CODPROCESS AND STDEV = 1", new string[] {":CODPROCESS"}, new string[] { Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]) });
                        if (dt.Rows.Count > 0)
                        {
                            connection = new DBConnection();
                            DataTable dt2 =  connection.readByAdapter("SELECT * FROM "+tabelas.processTable+" WHERE CODPROCESS = :CODPROCESS AND DTSTATUS1 IS NOT NULL", new string[] { ":CODPROCESS" }, new string[] {  Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]) });
                            if(dt2 != null && dt2.Rows.Count > 0)
                            {
                                connection = new DBConnection();
                                connection.write("UPDATE "+tabelas.processTable+" SET CARGAVINCUL = :NUMCAR,DTSTATUS2 = (SELECT SYSDATE FROM DUAL),USUVINCUL = :USERNAME WHERE CODPROCESS=:CODPROCESS", new string[] { ":NUMCAR", ":USERNAME", ":CODPROCESS" }, new string[] { cargaVinculTextBox.Text,Methods.username, Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]) });
                            }
                            else
                            {
                                MessageBox.Show("Não foi emitida o mapa de separação desta pendencia, vôcê pode vincular carregamento só quando após a emissão");
                            }                            
                        }
                        else
                        {
                            MessageBox.Show("Nota selcionada não teve pendencia", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }                    
                }
                else
                {
                    MessageBox.Show("Seleciona uma nota", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }                
            }
            else
            {
                MessageBox.Show("Insere o N°Carregamento", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void setBounds(DataTable dt)
        {
            try
            {
                //var numLat = Math.Abs((Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Max())) - Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Min()))) /
                //Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Max())));
                //var numLongt = Math.Abs((Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Max())) - Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Min()))) /
                //Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Max())));
                //var box = new LocationRect(Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Max())) + numLat,
                //Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Min())) - numLongt, Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Min())) - numLat,
                //Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Max()) + numLongt));
                //Console.WriteLine("karam1313 " +box.East); ;
                //mapView.SetView(box);
                float numLat = 0, numLongt = 0;
                var box = new LocationRect(Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Max())) + numLat,
                        Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Min())) - numLongt, Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LAT"]).Min())) - numLat,
                        Methods.doubleParser(Convert.ToString(dt.AsEnumerable().Select(x => x["LONGT"]).Max()) + numLongt));
                mapView.SetView(box);
            }
            catch(Exception ex)
            {
                mapView.Center = new Location(-15.7785, -47.9287);
                mapView.ZoomLevel = 9;
            }            
        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            if(notaTable2 != null && notaTable2.Rows.Count>0 && selectedNota > -1)
            {
                string numcar = Convert.ToString(notaTable2.Rows[selectedNota]["N_CARREGAMENTO"]);
                string numnota = Convert.ToString(notaTable2.Rows[selectedNota]["NF"]);
                string filename = numcar + "-" + numnota + ".3gp";
                Uri uri = new Uri(tabelas.audioUrl + filename);// 192.168.0.203
                if (Methods.isAlive(uri.ToString()))
                {
                    playAudio(uri);
                    playButtonChangeContent();                    
                }
                else
                {
                    MessageBox.Show("Não há gravação relacionada com esta entrega", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }                        
        }

        private void setDataUserEmissao(string codprocess)
        {
            DBConnection connection = new DBConnection();
            DataTable dt = connection.readByAdapter("SELECT * FROM "+tabelas.processTable+" WHERE CODPROCESS = :CODPROCESS AND DTSTATUS1 IS NULL", new string[] { ":CODPROCESS" }, new string[] { codprocess });
            if(dt!=null && dt.Rows.Count > 0)
            {
                connection = new DBConnection();
                connection.write("UPDATE "+tabelas.processTable+" SET DTSTATUS1 = (SELECT SYSDATE FROM DUAL) , USUEMISSAO = :USERNAME WHERE CODPROCESS=:CODPROCESS", new string[] { ":USERNAME", ":CODPROCESS" }, new string[] {Methods.username , codprocess });
            }            
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            stopAudio();
        }

        private void setBoundsOneNota(Location location)
        {
            mapView.Center = location;
            mapView.ZoomLevel=15;
        }


        private string getStatusConditions()
        {
            IList<string> str = new List<string>();
            if (perfeitoCheckBox.IsChecked == true)
            {
                str.Add("1");
            }
            if (devolucaoParcialCheckBox.IsChecked == true)
            {
                str.Add("2");
            }
            if (reentregaCheckBox.IsChecked == true)
            {
                str.Add("4");
            }
            if (devolucaoTotalCheckBox.IsChecked == true)
            {
                str.Add("3");
            }
            
            string statusStr = string.Join(",", str);
            if (perfeitoCheckBox.IsChecked == false && devolucaoParcialCheckBox.IsChecked == false &&
                reentregaCheckBox.IsChecked == false && devolucaoTotalCheckBox.IsChecked == false)
            {
                statusStr = "1,2,3,4";
            }            
            return statusStr;
        }


        private string getNOtaEmitionConditions()
        {
            if(dtEmissaoCheckBox.IsEnabled == true && dtEmissaoCheckBox.IsChecked == true)
            {
                return " AND DTSTATUS1 IS NULL";
            }
            return "";
        }


        private string getNotaLinkedStatus()
        {
            if (dtVincularCheckBox.IsEnabled==true && dtVincularCheckBox.IsChecked == true)
            {
                return " AND DTSTATUS2 IS NULL";
            }
            return "";
        }



        private void setLayoutValues(string obsmoto,string obscli,string emailcli ,int avalicao)
        {
            obsMotoTextBox.Text = obsmoto;
            obsCliTextBox.Text = obscli;
            emailCliTextBox.Text = emailcli;
            switch (avalicao)
            {
                case 0:
                    star1.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star2.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star3.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star4.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star5.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    break;
                case 1:
                    star1.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star2.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star3.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star4.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star5.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    break;
                case 2:
                    star1.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star2.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star3.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star4.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star5.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    break;
                case 3:
                    star1.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star2.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star3.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star4.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    star5.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    break;
                case 4:
                    star1.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star2.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star3.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star4.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star5.Fill = new SolidColorBrush(Color.FromRgb(211, 211, 211));
                    break;
                case 5:
                    star1.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star2.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star3.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star4.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    star5.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 0));
                    break;
            }           
        }

        private void playAudio(Uri uri)
        {
            if (isStopped)
            {
                mediaPlayer = new MediaPlayer();
                mediaPlayer.Open(uri);
                mediaPlayer.Play();
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                isplaying = true;
                isStopped = false;
                timer = new DispatcherTimer();
                timer.Tick += Timer_Tick;
                timer.Interval = new TimeSpan(0, 0, 1);
                timer.Start();
            }
            else
            {
                if (isplaying)
                {
                    pauseAudio();
                    isStopped = false;
                }
                else
                {                    
                    mediaPlayer.Play();
                    isplaying = true;
                    isStopped = false;
                }                
            }
            
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            try
            {
                stopAudio();
            }catch(Exception ex)
            {

            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {            
            if (isplaying)
            {                
                counter++;
                int minutes = counter / 60;
                int seconds = counter % 60;
                timerText.Text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }

        private void notaDataGrid2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {            
            if(sender != null)
            {
                selectedNota = notaDataGrid2.SelectedIndex;
                if(notaTable2.Rows.Count > 0 && selectedNota > -1)
                {
                    if(pendencia_user_check == true)
                    {
                        MessageBoxResult result = MessageBox.Show("Deseja emtir o mapa de separação dessa nota?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                DBConnection connection = new DBConnection();
                                DataTable dt = connection.readByAdapter("SELECT PROD.CODPROD,PROD.QT, PRODUT.MODULO, PRODUT.RUA, PRODUT.NUMERO, PRODUT.APTO,PRODUT.DESCRICAO,PRODUT.CODFAB,PRODUT.UNIDADE,PRODUT.EMBALAGEM ,(select marca from pcmarca@wint where codmarca = produt.codmarca) marca,(select fornecedor from pcfornec@wint where codfornec = produt.codfornec) fornecedor FROM " + tabelas.prodTable+" PROD, PCPRODUT@WINT PRODUT WHERE PROD.CODPROCESS = :CODPROCESS AND PROD.STDEV = 1 AND PROD.CODPROD = PRODUT.CODPROD order by produt.modulo,produt.rua,produt.numero,produt.apto", new string[] { ":CODPROCESS" }, new string[] { Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]) });
                                if (dt.Rows.Count > 0)
                                {                                                                        
                                    IList<object> listReport = new List<object>();
                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        listReport.Add(new Produto
                                        {
                                            CODPROD = Convert.ToString(dr["CODPROD"]),
                                            QT = Convert.ToString(dr["QT"]),
                                            MODULO = Convert.ToString(dr["MODULO"]),
                                            RUA = Convert.ToString(dr["RUA"]),
                                            NUMERO = Convert.ToString(dr["NUMERO"]),
                                            APTO = Convert.ToString(dr["APTO"]),
                                            CODFAB = Convert.ToString(dr["CODFAB"]),
                                            DESCRICAO = Convert.ToString(dr["DESCRICAO"]),
                                            EMBALAGEM = Convert.ToString(dr["EMBALAGEM"]),
                                            UNIDADE = Convert.ToString(dr["UNIDADE"]),                                            
                                            MARCA = Convert.ToString(dr["MARCA"])
                                        });
                                    }
                                    Dictionary<string, string> parameteres = new Dictionary<string, string>();                                    
                                    parameteres.Add("nf", Convert.ToString(notaTable2.Rows[selectedNota]["NF"]));
                                    parameteres.Add("cod_pendencia", Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]));
                                    parameteres.Add("pedido", Convert.ToString(notaTable2.Rows[selectedNota]["numped"]));
                                    parameteres.Add("rca",Convert.ToString(notaTable2.Rows[selectedNota]["rca"]));
                                    parameteres.Add("uf",Convert.ToString(notaTable2.Rows[selectedNota]["uf"]));
                                    parameteres.Add("cidade",Convert.ToString(notaTable2.Rows[selectedNota]["cidade"]));
                                    parameteres.Add("endereco",Convert.ToString(notaTable2.Rows[selectedNota]["endereco"]));
                                    parameteres.Add("complemento",Convert.ToString(notaTable2.Rows[selectedNota]["complemento"]));
                                    parameteres.Add("bairro",Convert.ToString(notaTable2.Rows[selectedNota]["bairro"]));
                                    parameteres.Add("obs1",Convert.ToString(notaTable2.Rows[selectedNota]["obs1"]));
                                    parameteres.Add("obs2",Convert.ToString(notaTable2.Rows[selectedNota]["obs2"]));
                                    parameteres.Add("obs3",Convert.ToString(notaTable2.Rows[selectedNota]["obs3"]));
                                    parameteres.Add("cep",Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]));
                                    parameteres.Add("cod_cliente",Convert.ToString(notaTable2.Rows[selectedNota]["cep"]));
                                    parameteres.Add("cliente",Convert.ToString(notaTable2.Rows[selectedNota]["cliente"]));
                                    parameteres.Add("fantasia",Convert.ToString(notaTable2.Rows[selectedNota]["fantasia"]));                                    
                                    PendenciaReport pendenciaReport = new PendenciaReport(listReport,parameteres, "transport.ReportMapaSeparacao.rdlc", "DataSetSeparacao");                                    
                                    pendenciaReport.setReportWindowInstance(this);
                                    this.IsEnabled = false;
                                    this.Focusable = false;
                                    this.Focusable = false;
                                    pendenciaReport.Show();
                                    pendenciaReport.Activate();
                                    //Cadstrar a data de impressão da mapa de separação da pendencia
                                    setDataUserEmissao(Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]));
                                }
                                else
                                {
                                    MessageBox.Show("Está nota não teve pendencia", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Sem permissão", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }                                           
                }
            }            
        }

        private void buttonEXportExcel_Click(object sender, RoutedEventArgs e)
        {            
            if (notaTable2!=null && notaTable2.Rows.Count > 0)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = "Arquivos de excel|*.xls";
                dialog.ShowDialog();
                String fileName = dialog.FileName;
                if (fileName != null && fileName != "")
                {
                    GenerateExcel(notaTable2, fileName);
                }                
            }
            else
            {
                MessageBox.Show("Não há dados para exportar", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }


        public static void GenerateExcel(DataTable dataTable, string path)
        {

            DataSet dataSet = new DataSet();
            dataSet.Tables.Add(dataTable);

            // create a excel app along side with workbook and worksheet and give a name to it             
            Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel.Workbook excelWorkBook = excelApp.Workbooks.Add();
            Microsoft.Office.Interop.Excel._Worksheet xlWorksheet = excelWorkBook.Sheets[1];
            Microsoft.Office.Interop.Excel.Range xlRange = xlWorksheet.UsedRange;
            foreach (DataTable table in dataSet.Tables)
            {
                //Add a new worksheet to workbook with the Datatable name  
                Microsoft.Office.Interop.Excel.Worksheet excelWorkSheet = excelWorkBook.Sheets.Add();
                excelWorkSheet.Name = table.TableName;

                // add all the columns  
                for (int i = 1; i < table.Columns.Count + 1; i++)
                {
                    excelWorkSheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
                }

                // add all the rows  
                for (int j = 0; j < table.Rows.Count; j++)
                {
                    for (int k = 0; k < table.Columns.Count; k++)
                    {
                        excelWorkSheet.Cells[j + 2, k + 1] = table.Rows[j].ItemArray[k].ToString();
                    }
                }
            }
            excelWorkBook.SaveAs(path); // -> this will do the custom  
            excelWorkBook.Close();
            excelApp.Quit();
        }

        private void buttonLoadImages_Click(object sender, RoutedEventArgs e)
        {
            if (notaTable2 != null && notaTable2.Rows.Count > 0 && selectedNota > -1)
            {
                string numcar = Convert.ToString(notaTable2.Rows[selectedNota]["N_CARREGAMENTO"]);
                string numnota = Convert.ToString(notaTable2.Rows[selectedNota]["NF"]);
                string filename = tabelas.imgUrl + numcar + "-" + numnota+ "-1.jpg";
                Uri uri = new Uri(filename);
                if (Methods.isAlive(uri.ToString()))
                {
                    ImagesWindow imagesWindow = new ImagesWindow();
                    imagesWindow.setNumCarNumNota(numcar, numnota);
                    imagesWindow.Show();
                }
                else
                {
                    MessageBox.Show("Não há anexos relacionados com esta entrega", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }                
            }
        }

        private void pauseAudio()
        {            
           mediaPlayer.Pause();            
           isplaying = false;
        }

     

        private void stopAudio()
        {
            if(mediaPlayer != null && isStopped == false)
            {
                mediaPlayer.Stop();
                mediaPlayer.Close();
                mediaPlayer = null;
                isStopped = true;
                timer.Stop();
                timer = null;
                counter = 0;
                timerText.Text = "00:00";
                buttonPlay.Content = FindResource("play");
            }            
        }

        private void playButtonChangeContent()
        {
            if(buttonPlay.Content ==FindResource("play"))
            {
                buttonPlay.Content = FindResource("pause");
            }
            else
            {
                buttonPlay.Content = FindResource("play");
            }            
        }

        private void finalizeTravelButton_Click(object sender, RoutedEventArgs e)
        {
            if(Methods.loginType == "AD")
            {
                if (numcar != string.Empty)
                {
                    DBConnection connection = new DBConnection();
                    DataTable dt = connection.readByAdapter($"select a.numcar from {tabelas.cargaTable} a,pccarreg@wint b where a.numcar = b.numcar and  a.numcar = :numcar and a.dtfinal is null and b.dtretorno is not null", new string[] { ":numcar" }, new string[] { numcar });
                    if (dt.Rows.Count > 0)
                    {
                        connection = new DBConnection();
                        connection.write($"update {tabelas.cargaTable} set dtfinal = sysdate where numcar = :numcar", new string[] { ":numcar" }, new string[] { numcar });
                        MessageBox.Show($"O carregamento {numcar} está finalizado com sucesso", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Não pode finalizar este carrecamento", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Sem permissão", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }            
        }

        private void setCheckBoxesEnable(object sender, RoutedEventArgs e)
        {
            if(devolucaoParcialCheckBox.IsChecked == true && perfeitoCheckBox.IsChecked == false && devolucaoTotalCheckBox.IsChecked== false && reentregaCheckBox.IsChecked == false)
            {
                dtEmissaoCheckBox.IsEnabled = true;
                dtVincularCheckBox.IsEnabled = true;
            }
            else
            {
                dtEmissaoCheckBox.IsEnabled = false;
                dtVincularCheckBox.IsEnabled = false;
            }
        }


        private void setCountNotas(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                int countDev = dt.Select("status = 'Devolução'").Count();
                int countPer = dt.Select("status = 'Perfeito'").Count();
                int countPend = dt.Select("status = 'Pendencia ou devolução parcial'").Count();
                int countRen = dt.Select("status = 'Re-entrega'").Count();                
                countNotasTextBlock.Text = $"Notas: Total ({ dt.Rows.Count}) , Perfeito ({countPer}) , Devolução({countDev}) , Re-entrega ({countRen}) , Pendencia ou devolução parcial ({countPend})";
            }
            else
            {
                countNotasTextBlock.Text = "Notas: ";
            }
        }

        private void setCountProds(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                int countDev = dt.Select("status = 'Devolução'").Count();                
                int countPend = dt.Select("status = 'Pendencia'").Count();                
                countProdsTextBlock.Text = $"Produtos : Total ({ dt.Rows.Count}) Devolução ({countDev}) , Pendencia ({countPend})";
            }
            else
            {
                countProdsTextBlock.Text = "Produtos: ";
            }
        }

    }
}