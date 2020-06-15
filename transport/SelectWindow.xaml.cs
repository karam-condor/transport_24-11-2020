using System;
using System.Collections.Generic;
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
    /// Interaction logic for SelectWindow.xaml
    /// </summary>
    public partial class SelectWindow : Window
    {        
        public SelectWindow()
        {
            InitializeComponent();
        }

        private void button_monitor_Click(object sender, RoutedEventArgs e)
        {
            MonitorWindow monitorWindow = new MonitorWindow();
            monitorWindow.Show();
        }

        private void button_report_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow reportsWindow = new ReportsWindow();
            reportsWindow.Show();
        }

    

        private void button_config_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationWindow configurationWindow = new ConfigurationWindow();
            configurationWindow.Show();
        }
    }
}
