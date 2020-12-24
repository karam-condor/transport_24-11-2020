using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// Interaction logic for ImagesWindow.xaml
    /// </summary>
    public partial class ImagesWindow : Window
    {
        private string numcar, numnota;
        BitmapImage bitmap;
        public ImagesWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            showImage(1);            
        }

        private void showImage(int order)
        {
            try
            {
                string filename = tabelas.imgUrl + numcar + "-" + numnota + "-" + order + ".jpg";
                Uri uri = new Uri(filename);
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.EndInit();
                img.Source = bitmap;
            }
            catch(Exception ex)
            {

            }
        }

        private void btn_img_1_Click(object sender, RoutedEventArgs e)
        {
            showImage(1);
        }

        private void btn_img_2_Click(object sender, RoutedEventArgs e)
        {
            showImage(2);
        }

        private void btn_img_3_Click(object sender, RoutedEventArgs e)
        {
            showImage(3);
        }

        public void setNumCarNumNota(string numcar, string numnota)
        {
            this.numcar = numcar;
            this.numnota = numnota;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            saveImage();
        }

        private void saveImage()
        {
            if(bitmap != null)
            {
                try
                {
                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.Filter = "JPEG|*.jpg";
                    dialog.ShowDialog();
                    String fileName = dialog.FileName;
                    Methods.Save(bitmap, fileName);
                }
                catch(Exception ex)
                {

                }                
            }
        }
    }    
}
