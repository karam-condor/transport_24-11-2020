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
        private Object parentWindow;        
        private IList<object> listReport;
        private Dictionary<string,string> propList;
        private string reportName,reportDataSetName;

        
        public PendenciaReport()
        {
            InitializeComponent();
            _reportViewer.Load += ReportViewer_Load;
        }

        public PendenciaReport(IList<object> listReport, Dictionary<string,string> props,string reportName,string reportDataSetName)
        {
            InitializeComponent();
            this.propList = props;
            this.listReport = listReport;
            this.reportName = reportName;
            this.reportDataSetName = reportDataSetName;
            _reportViewer.Load += ReportViewer_Load;
        }

        private void ReportViewer_Load(object sender, EventArgs e)
        {
            if (this.listReport.Count > 0)
            {
                var dataSource = new Microsoft.Reporting.WinForms.ReportDataSource(reportDataSetName, listReport);
                _reportViewer.LocalReport.DataSources.Add(dataSource);
                _reportViewer.LocalReport.ReportEmbeddedResource = reportName;
                Microsoft.Reporting.WinForms.ReportParameter[] repParams = new Microsoft.Reporting.WinForms.ReportParameter[propList.Count];
                int counter = 0;
                foreach (KeyValuePair<string, string> entry in propList)
                {                   
                    repParams[counter] = new Microsoft.Reporting.WinForms.ReportParameter(entry.Key, entry.Value);
                    counter++;
                }                
                _reportViewer.LocalReport.SetParameters(repParams);
                _reportViewer.SetDisplayMode(Microsoft.Reporting.WinForms.DisplayMode.PrintLayout);
                _reportViewer.RefreshReport();                
            }
        }
        

        public void setReportWindowInstance(Object parentWindow)
        {
            this.parentWindow = parentWindow;
        }

        

        

        

        private void Window_Closed(object sender, EventArgs e)
        {
            if(parentWindow != null)
            {
                ((Window)parentWindow).IsEnabled = true;
                ((Window)parentWindow).Activate();
                ((Window)parentWindow).Focusable = true;
            }
        }
    }
}



