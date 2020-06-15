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

namespace transport
{
    /// <summary>
    /// Interaction logic for PendenciaReport.xaml
    /// </summary>
    public partial class PendenciaReport : Window
    {
        private ReportsWindow reportWindInstance;        
        private IList<Produto> listReport;
        private string date, cod_pendencia, nf;
        public PendenciaReport()
        {
            InitializeComponent();
            _reportViewer.Load += ReportViewer_Load;
        }


        private void ReportViewer_Load(object sender, EventArgs e)
        {
            if (this.listReport.Count > 0)
            {                
                var dataSource = new Microsoft.Reporting.WinForms.ReportDataSource("DataSetSeparacao", listReport);
                _reportViewer.LocalReport.DataSources.Add(dataSource);
                _reportViewer.LocalReport.ReportEmbeddedResource = "transport.ReportMapaSeparacao.rdlc";                
                Microsoft.Reporting.WinForms.ReportParameter nfParam = new Microsoft.Reporting.WinForms.ReportParameter("nf", this.nf);
                Microsoft.Reporting.WinForms.ReportParameter pendParam = new Microsoft.Reporting.WinForms.ReportParameter("cod_pendencia", this.cod_pendencia);
                _reportViewer.LocalReport.SetParameters(new Microsoft.Reporting.WinForms.ReportParameter[] { nfParam,pendParam});
                _reportViewer.RefreshReport();                
            }
        }
        public void setListReport(IList<Produto> list)
        {
            this.listReport = list;
        }

        public void setReportWindowInstance(ReportsWindow reportsWindow)
        {
            this.reportWindInstance = reportsWindow;
        }

        public void setDate(string date)
        {
            this.date = date;
        }

        public void setNF(string nf)
        {
            this.nf = nf;
        }

        public void setCod_pendencia(string cod_pendencia)
        {
            this.cod_pendencia = cod_pendencia;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(reportWindInstance != null)
            {
                reportWindInstance.IsEnabled = true;
                reportWindInstance.Activate();
                reportWindInstance.Focusable = true;
            }
        }
    }
}