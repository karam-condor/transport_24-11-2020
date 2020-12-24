using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public partial class loadingWindow : Window
    {
        Object parent;
        public loadingWindow(bool showCancelButton,object parent)
        {
            InitializeComponent();
            this.parent = parent;
            if (showCancelButton)
            {
                cancelButton.Visibility = Visibility.Visible;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width -20;
            this.Top = desktopWorkingArea.Bottom - this.Height -20;
            this.Topmost = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            cancelButton.IsEnabled = false;
            routing2 parentWin = (routing2)parent;
            parentWin.cancelTSP();
        }
    }
}
