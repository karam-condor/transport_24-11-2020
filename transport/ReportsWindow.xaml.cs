using Microsoft.Maps.MapControl.WPF;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public ReportsWindow()
        {
            InitializeComponent();
        }


        private void textJustNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            acceptJustNumbers(sender, e);
        }

        


  

        public void acceptJustNumbers(Object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int iValue = -1;

            if (Int32.TryParse(textBox.Text, out iValue) == false)
            {
                TextChange textChange = e.Changes.ElementAt<TextChange>(0);
                int iAddedLength = textChange.AddedLength;
                int iOffset = textChange.Offset;
                textBox.Text = textBox.Text.Remove(iOffset, iAddedLength);
                textBox.Select(textBox.Text.Length, textBox.Text.Length);
            }
        }
        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if(cargaTextBox.Text != string.Empty)
            {
                DBConnection connection = new DBConnection();
                cargaTable = connection.readByAdapter("select A.NUMCAR,A.CODMOTORISTA,B.NOME MOTORISTA, A.KM,A.DTSAIDA,A.DTFINAL from LOGTRANSCAR A, PCEMPR@WINT B, PCVEICUL@WINT C where A.codmotorista = B.matricula AND NUMCAR = :NUMCAR GROUP BY  A.NUMCAR,A.CODMOTORISTA,B.NOME,A.KM,A.DTSAIDA,A.DTFINAL", new string[] { ":NUMCAR" }, new string[] { cargaTextBox.Text });
                connection = new DBConnection();
                notaTable = connection.readByAdapter("select CODPROCESS,case when status = 0 then ''  when status = 1  then 'Perfeito' when status = 2  then 'Pendencia ou devolução parcial' when status = 3 then 'Devolução' when status = 4 then 'Re-entrega' end as status,NUMNOTA,LAT,LONGT,DTENTREGA DT_ENTREGA,OBSENT OBS_ENTREGA,case when status = 1 then '' else  to_char(CODMOTIVO) end COD_MOTIVO, case when status = 1 then '' else B.MOTIVO end MOTIVO,DTSTATUS1 DT_EMISSAO,DTSTATUS2,DTSTATUS3,DTSTATUS4,CARGAVINCUL,CASE WHEN  STCRED=1  THEN 'Gerar crédito' ELSE '' END AS CREDITO  FROM LOGTRANSPROCESS A, PCTABDEV@WINT B WHERE A.NUMCAR = :NUMCAR AND A.CODMOTIVO = B.CODDEVOL order by DTENTREGA", new string[] { ":NUMCAR" },new string[] { cargaTextBox.Text});
                
                if(cargaTable != null && cargaTable.Rows.Count > 0)
                {                    
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
                        Location location = new Location(Convert.ToDouble(notaTable.Rows[i]["LAT"]), Convert.ToDouble(notaTable.Rows[i]["LONGT"]));
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
                notaTable2 = connection.readByAdapter("select CODPROCESS Cod_Entrega,case when status = 0 then ''  when status = 1  then 'Perfeito' when status = 2  then 'Pendencia ou devolução parcial' when status = 3 then 'Devolução' when status = 4 then 'Re-entrega' end as status,NUMCAR N_CARREGAMENTO, NUMNOTA NF,LAT,LONGT,DTENTREGA DT_ENTREGA,EMAIL_CLIENTE, OBSENT OBS_ENTREGA,AVALICAO,CLICOMENT,case when status = 1 then '' else to_char(CODMOTIVO) end COD_MOTIVO, case when status = 1 then '' else B.MOTIVO end MOTIVO,DTSTATUS1 DT_EMISSAO, DTSTATUS2, DTSTATUS3, DTSTATUS4, CARGAVINCUL Carregamento_Vinculado,CASE WHEN  STCRED=1  THEN 'Gerar crédito' ELSE '' END AS CREDITO FROM LOGTRANSPROCESS A, PCTABDEV @WINT B WHERE A.NUMNOTA LIKE CASE WHEN :NUMNOTA IS NULL THEN '%' ELSE: NUMNOTA END AND A.CODPROCESS LIKE CASE WHEN :CODPROCESS IS NULL THEN '%' ELSE: CODPROCESS END  AND A.CODMOTIVO = B.CODDEVOL AND A.DTENTREGA >= TO_DATE(:DTINCIAL, 'YYYY/MM/DD HH24:MI:SS') AND A.DTENTREGA <= TO_DATE(:DTFINAL,'YYYY/MM/DD HH24:MI:SS') AND A.STATUS IN(" + getStatusConditions() + ") order by A.DTENTREGA", new string[] {":NUMNOTA" , ":CODPROCESS" , ":DTINCIAL", ":DTFINAL" }, new string[] { notaTextBox.Text, processTextBox.Text,dtIncialPicker.SelectedDate.Value.ToString("yyyy/MM/dd"), dtFinalPicker.SelectedDate.Value.AddDays(1).ToString("yyyy/MM/dd")});
                if(notaTable2 != null && notaTable2.Rows.Count > 0)
                {
                    prodDataGrid.ItemsSource = new DataTable().DefaultView;
                    DataTable dt = notaTable2.DefaultView.ToTable(false, new string[] { "Cod_Entrega", "N_CARREGAMENTO", "NF", "STATUS", "DT_ENTREGA", "OBS_ENTREGA", "COD_MOTIVO", "MOTIVO", "Carregamento_Vinculado", "DT_EMISSAO" ,"CREDITO"});
                    notaDataGrid2.ItemsSource = dt.DefaultView;
                    setLayoutValues(Convert.ToString(notaTable2.Rows[0]["OBS_ENTREGA"]), Convert.ToString(notaTable2.Rows[0]["CLICOMENT"]), Convert.ToString(notaTable2.Rows[0]["EMAIL_CLIENTE"]), notaTable2.Rows[0]["AVALICAO"] == DBNull.Value ? 0 : Convert.ToInt32(notaTable2.Rows[0]["AVALICAO"]));
                }
                else
                {
                    notaDataGrid2.ItemsSource = new DataTable().DefaultView;
                    setLayoutValues("", "", "", 0);
                }
                
                
            }            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dtIncialPicker.SelectedDate=DateTime.Now;
            dtFinalPicker.SelectedDate = DateTime.Now;
            buttonPlay.Content = FindResource("play");
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
                    Location location = new Location(Convert.ToDouble(notaTable2.Rows[selectedNota]["LAT"]), Convert.ToDouble(notaTable2.Rows[selectedNota]["LONGT"]));
                    drawPushPin(location, template, Convert.ToString(notaTable2.Rows[selectedNota]["NF"]));
                    setBoundsOneNota(location);
                } catch (Exception ex){}                
                DBConnection connection = new DBConnection();
                prodTable = connection.readByAdapter("SELECT A.CODPROD , B.DESCRICAO , A.QT , A.CODMOTIVO , C.MOTIVO ,CASE WHEN A.STDEV = 1 THEN 'PENDENCIA' ELSE  'DEVOLUÇÃO' END AS STATUS  FROM LOGTRANSPROD A,PCPRODUT@WINT B,PCTABDEV@WINT C  WHERE A.CODPROCESS = :CODPROCESS AND A.CODPROD = B.CODPROD AND A.CODMOTIVO = C.CODDEVOL", new string[] { ":CODPROCESS" }, new string[] { Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"])});
                //here                
                if (prodTable != null && prodTable.Rows.Count > 0)
                {                    
                    prodDataGrid.ItemsSource = prodTable.DefaultView;
                }
                else
                {
                    prodDataGrid.ItemsSource = new DataTable().DefaultView;
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
                        DataTable dt = connection.readByAdapter("SELECT * FROM LOGTRANSPROD WHERE CODPROCESS = :CODPROCESS AND STDEV = 1", new string[] {":CODPROCESS"}, new string[] { Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]) });
                        if (dt.Rows.Count > 0)
                        {
                            connection = new DBConnection();
                            connection.write("UPDATE LOGTRANSPROCESS SET CARGAVINCUL = :NUMCAR WHERE CODPROCESS=:CODPROCESS", new string[] { ":NUMCAR", ":CODPROCESS" }, new string[] { cargaVinculTextBox.Text, Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]) });
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
            var numLat = Math.Abs((Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Max()) - Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Min())) /
            Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Max()));
            var numLongt = Math.Abs((Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Max()) - Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Min())) /
            Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Max()));
            var box = new LocationRect(Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Max()) + numLat,
            Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Min()) - numLongt, Convert.ToDouble(dt.AsEnumerable().Select(x => x["LAT"]).Min()) - numLat,
            Convert.ToDouble(dt.AsEnumerable().Select(x => x["LONGT"]).Max()) + numLongt);
            mapView.SetView(box);
        }

        private void buttonPlay_Click(object sender, RoutedEventArgs e)
        {
            if(notaTable2 != null && notaTable2.Rows.Count>0 && selectedNota > -1)
            {
                string numcar = Convert.ToString(notaTable2.Rows[selectedNota]["N_CARREGAMENTO"]);
                string numnota = Convert.ToString(notaTable2.Rows[selectedNota]["NF"]);
                string filename = numcar + "-" + numnota + ".3gp";
                Uri uri = new Uri("http://192.168.0.203/transporte/records/" + filename);// 192.168.0.203
                if (Methods.isURLExist(uri.ToString()))
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

        private void setDataEmissao(string codprocess)
        {
            DBConnection connection = new DBConnection();
            DataTable dt = connection.readByAdapter("SELECT * FROM LOGTRANSPROCESS WHERE CODPROCESS = :CODPROCESS AND DTSTATUS1 IS NULL", new string[] { ":CODPROCESS" }, new string[] { codprocess });
            if(dt!=null && dt.Rows.Count > 0)
            {
                connection = new DBConnection();
                connection.write("UPDATE LOGTRANSPROCESS SET DTSTATUS1 = (SELECT SYSDATE FROM DUAL) WHERE CODPROCESS=:CODPROCESS", new string[] { ":CODPROCESS" }, new string[] { codprocess });
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
                    MessageBoxResult result = MessageBox.Show("Deseja emtir o mapa de separação dessa nota?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            DBConnection connection = new DBConnection();
                            DataTable dt = connection.readByAdapter("SELECT PROD.CODPROD,PROD.QT, PRODUT.MODULO, PRODUT.RUA, PRODUT.NUMERO, PRODUT.APTO,PRODUT.DESCRICAO,PRODUT.CODFAB,PRODUT.UNIDADE,PRODUT.EMBALAGEM  FROM LOGTRANSPROD PROD, PCPRODUT@WINT PRODUT WHERE PROD.CODPROCESS = :CODPROCESS AND PROD.STDEV = 1 AND PROD.CODPROD = PRODUT.CODPROD order by produt.modulo,produt.rua,produt.numero,produt.apto", new string[] { ":CODPROCESS" }, new string[] { Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]) });
                            if (dt.Rows.Count > 0)
                            {                                
                                PendenciaReport pendenciaReport = new PendenciaReport();
                                pendenciaReport.setReportWindowInstance(this);
                                IList<Produto> listReport = new List<Produto>();
                                foreach(DataRow dr in dt.Rows)
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
                                        UNIDADE = Convert.ToString(dr["UNIDADE"])
                                    });
                                }                                
                                pendenciaReport.setListReport(listReport);
                                pendenciaReport.setDate(DateTime.Now.ToString());
                                pendenciaReport.setNF(Convert.ToString(notaTable2.Rows[selectedNota]["NF"]));
                                pendenciaReport.setCod_pendencia(Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]));
                                this.IsEnabled = false;
                                this.Focusable = false;
                                this.Focusable = false;
                                pendenciaReport.Show();
                                pendenciaReport.Activate();
                                //Cadstrar a data de impressão da mapa de separação da pendencia
                                setDataEmissao(Convert.ToString(notaTable2.Rows[selectedNota]["Cod_Entrega"]));
                            }
                            else
                            {
                                MessageBox.Show("Está nota não teve pendencia","Erro",MessageBoxButton.OK,MessageBoxImage.Error);
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }                        
                    }                        
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
    }
}