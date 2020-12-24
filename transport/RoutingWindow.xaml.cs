using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;


namespace transport
{
    //1132104 numcar where case not all pedidos geocogificados    
    public partial class RoutingWindow : Window
    {
        DataTable cargasTable;
        int selectedIndex;
        DataRowView selectedRow;                        
        public RoutingWindow()
        {
            InitializeComponent();
            setDatePickerDefaultValue();
        }

        private void setDatePickerDefaultValue()
        {
            dtmonfin.SelectedDate = DateTime.Now.AddDays(1);
            dtsaidfin.SelectedDate = DateTime.Now.AddDays(1);
            dtmonini.SelectedDate = DateTime.Now.AddDays(-7);
            dtsaidini.SelectedDate = DateTime.Now.AddDays(-7);
        }

        private void routingSearchButton_Click(object sender, RoutedEventArgs e)
        {
            loadCarga();
        }

        private void routingNumcarTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Methods.acceptJustNumbers(sender,e);
        }

        private void routingCargasDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            selectedIndex = routingCargasDataGrid.SelectedIndex;
            selectedRow = (DataRowView)routingCargasDataGrid.SelectedItems[0];
            try
            {
                if (routingRadioButton.IsChecked == true)
                {
                    Carga carga = new Carga(Convert.ToString(selectedRow["Carregamento"]), Convert.ToString(selectedRow["Roteirizado"]), Convert.ToString(selectedRow["Destino"]), Convert.ToString(selectedRow["codveiculo"]), Convert.ToString(selectedRow["Placa"]), Convert.ToString(selectedRow["Veiculo"])
                    , Convert.ToString(selectedRow["Motorista"]), Convert.ToString(selectedRow["datamon"]), Convert.ToString(selectedRow["dtsaida"]), Convert.ToString(selectedRow["dtretorno"]), Convert.ToString(selectedRow["dias"]), Convert.ToString(selectedRow["Entregas"]
                    ), Convert.ToString(selectedRow["Itens"]), Convert.ToString(selectedRow["km_rodado"]), Convert.ToString(selectedRow["Volume_Total"]), Convert.ToString(selectedRow["Peso_Total"]), Convert.ToString(selectedRow["Pedidos"]), Convert.ToString(selectedRow["Vltotal"]));
                    if (Methods.intParser(carga.pedidos) > 1)
                    {
                        routing2 routing2 = new routing2();
                        routing2.carga = carga;
                        routing2.routingWindow = this;
                        routing2.Show();
                        routing2.Focus();
                        this.IsEnabled = false;
                    }
                    else
                    {
                        MessageBox.Show("É necessario ter no mínimo dois pedidos para roteirização");
                    }
                }
                else if (reportRadioButton.IsChecked == true)
                {
                    DBConnection connection = new DBConnection();
                    DataTable dt = connection.readByAdapter(@"SELECT geo.numcar,
                                                                     geo.numped,
                                                                     (SELECT numnota
                                                                      FROM pcnfsaid@wint
                                                                      WHERE numcar = geo.numcar AND numped = geo.numped) numnota,
                                                                     pedc.numvolume,
                                                                     carreg.codrotaprinc,
                                                                     pedc.vlatend vltotal,
                                                                     pedc.totpeso pesototal,
                                                                     vei.placa,
                                                                     cli.cliente,
                                                                     cli.estent uf,
                                                                     cli.municent cidade,
                                                                     geo.seq
                                                              FROM " + tabelas.geoTable + @" geo,
                                                                   pcpedc@wint pedc,
                                                                   pccarreg@wint carreg,
                                                                   pcveicul@wint vei,
                                                                   pcclient@wint cli
                                                              WHERE     geo.numcar = :numcar
                                                                    AND geo.numped = pedc.numped
                                                                    AND geo.codcli = cli.codcli
                                                                    AND geo.numcar = carreg.numcar
                                                                    AND carreg.codveiculo = vei.codveiculo
                                                              ORDER BY geo.seq DESC, geo.numped", new string[] { ":numcar" }, new string[] { Convert.ToString(selectedRow["Carregamento"]) });
                    if (dt.Rows.Count > 0)
                    {
                        Console.WriteLine("reached");
                        IList<Object> listReport = new List<Object>();
                        foreach (DataRow dr in dt.Rows)
                        {
                            listReport.Add(new Entrega
                            {
                                numcar = Convert.ToString(dr["numcar"]),
                                numped = Convert.ToString(dr["numped"]),
                                numnota = Convert.ToString(dr["numnota"]),
                                numvolume = Convert.ToString(dr["numvolume"]),
                                cidade = Convert.ToString(dr["cidade"]),
                                cliente = Convert.ToString(dr["cliente"]),
                                codrotaprinc = Convert.ToString(dr["codrotaprinc"]),
                                uf = Convert.ToString(dr["uf"]),
                                pesototal= Convert.ToString(dr["pesototal"]),
                                vltotal = Convert.ToString(dr["vltotal"]),
                                placa = Convert.ToString(dr["placa"]),
                                seq = Convert.ToString(dr["seq"])

                            });
                        }
                        Dictionary<string, string> parameteres = new Dictionary<string, string>();                        
                        PendenciaReport pendenciaReport = new PendenciaReport(listReport, parameteres, "transport.ReportEntregaSeq.rdlc", "entregaDataSet");
                        pendenciaReport.setReportWindowInstance(this);
                        this.IsEnabled = false;
                        this.Focusable = false;
                        this.Focusable = false;
                        pendenciaReport.Show();
                        pendenciaReport.Activate();
                    }
                }
            }
            catch
            {

            }                        
        }


        private void loadCarga()
        {
            try
            {
                routingCargasDataGrid.ItemsSource = new DataView();
                DBConnection connection = new DBConnection();
                cargasTable = connection.readByAdapter($"SELECT carreg.numcar Carregamento,(select case when count(numcar) = 0 then 'Não' else 'Sim' end from {tabelas.geoTable} where numcar = carreg.numcar)  Roteirizado, carreg.destino Destino,carreg.codveiculo,veic.placa Placa,veic.descricao Veiculo, empr.nome Motorista,carreg.datamon,carreg.dtsaida, carreg.dtretorno,(trunc(carreg.dtretorno) - trunc(carreg.dtsaida)) dias,count(distinct(pedc.numped)) Pedidos,count(distinct(pedc.codcli)) Entregas,sum(pedi.qt) Itens,carreg.kmfinal - carreg.kminicial km_rodado,carreg.totpeso Peso_Total ,carreg.totvolume Volume_Total ,carreg.vltotal Vltotal FROM pccarreg@wint carreg, pcveicul@wint veic, pcempr@wint empr,pcpedc@wint pedc,pcpedi@wint pedi where empr.matricula = carreg.codmotorista and veic.codveiculo = carreg.codveiculo and carreg.numcar = pedc.numcar and pedc.numped = pedi.numped and carreg.dt_cancel is null and carreg.destino <> 'retira' and carreg.destino <> 'VENDA BALCAO' and carreg.datamon between TO_DATE(:dtmonini,'YYYY-MM-DD HH24:MI:SS') and TO_DATE(:dtmonfin,'YYYY-MM-DD HH24:MI:SS') and carreg.dtsaida between TO_DATE(:dtsaidini,'YYYY-MM-DD HH24:MI:SS') and TO_DATE(:dtsaidfin,'YYYY-MM-DD HH24:MI:SS') and carreg.numcar like case when :numcar is null then '%' else :numcar end group by carreg.numcar,carreg.destino,carreg.codveiculo,veic.placa,veic.descricao,    empr.nome,carreg.datamon,carreg.dtsaida,   carreg.dtretorno,trunc(carreg.dtretorno) - trunc(carreg.dtsaida),carreg.kmfinal - carreg.kminicial,carreg.totpeso,carreg.totvolume,carreg.vltotal",
                    new string[] { ":dtmonini", ":dtmonfin", ":dtsaidini", ":dtsaidfin", "numcar" },
                    new string[] { dtmonini.SelectedDate.Value.ToString("yyyy-MM-dd"),dtmonfin.SelectedDate.Value.ToString("yyyy-MM-dd"),dtsaidini.SelectedDate.Value.ToString("yyyy-MM-dd")
                    ,dtsaidfin.SelectedDate.Value.ToString("yyyy-MM-dd"),Convert.ToString(routingNumcarTextBox.Text).Trim()
                });
                if (cargasTable != null && cargasTable.Rows.Count > 0)
                {
                    routingCargasDataGrid.ItemsSource = cargasTable.DefaultView;
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);                
            }
        }                      

    }
}