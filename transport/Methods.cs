using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Data;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace transport
{
    class Methods
    {
        public static string loginType= "";
        public static string username = "";
        public static bool useDistanceMatrix = false;        


        public static int intParser(string number)
        {            
            try
            {
                return int.Parse(number);
            }catch(Exception ex)
            {
                return 0;
            }
        }


        public static float floatParser(string number)
        {
            try
            {
                return float.Parse(number);
            }
            catch (Exception ex)
            {
                return 0f;
            }
        }


        public static bool IsAllDigits(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }


        public static bool isURLExist(string url)
        {
            try
            {
                WebRequest req = WebRequest.Create(url);

                WebResponse res = req.GetResponse();

                return true;
            }
            catch (WebException ex)
            {
                return false;
            }
        }

        public static Double doubleParser(String numStr)
        {
            try
            {
                return Convert.ToDouble(numStr);
            }
            catch(Exception ex)
            {
                return -1000d;
            }
        }
        public static void Save(BitmapImage image, string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }


        public static void acceptJustNumbers(Object sender, TextChangedEventArgs e)
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

        public static void acceptJustFloatingNumbers(Object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            float iValue = -1;            
            if (float.TryParse(textBox.Text, out iValue) == false)
            {
                TextChange textChange = e.Changes.ElementAt<TextChange>(0);
                int iAddedLength = textChange.AddedLength;
                int iOffset = textChange.Offset;
                textBox.Text = textBox.Text.Remove(iOffset, iAddedLength);
                textBox.Select(textBox.Text.Length, textBox.Text.Length);
            }
        }

        public static void saveSetting(string settingName,string settingValue)
        {
            Properties.Settings.Default[settingName] = settingValue;
            Properties.Settings.Default.Save();
        }

        public static DataTable ConvertTo<T>(List<T> list)
        {
            DataTable table = CreateTable<T>();
            Type entityType = typeof(T);
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);
            foreach (T item in list)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item);
                }
                table.Rows.Add(row);
            }
            return table;
        }

        public static DataTable CreateTable<T>()
        {
            Type entityType = typeof(T);
            DataTable table = new DataTable(entityType.Name);
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(entityType);
            foreach (PropertyDescriptor prop in properties)
            {
                table.Columns.Add(prop.Name, prop.PropertyType);
            }
            return table;
        }





        public static Color getRandomColor(Random rnd)
        {
            return Color.FromRgb((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));
        }


        public static bool isAlive(string url)
        {            
            WebRequest request = WebRequest.Create(url);
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    //return Convert.ToString((int)response.StatusCode);
                    return true;
                }

            }
            catch (WebException ex)
            {
                using (HttpWebResponse res = (HttpWebResponse)ex.Response)
                {
                    return false;
                }
            }
        }

        /*By default, when you click inside a DataGridRow, it will be selected automatically. 
         * However if in DataGrid there is an element which handles the mouse event, then the automatic selection will not work.
         * You could use the code what you have tried to find exactly which row should be selected, and set the SelectedItem property with it.*/
        public static void selectRowByRightClick(ref DataGrid grid, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null) return;

            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                cell.Focus();

                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                DataGridRow row = dep as DataGridRow;
                grid.SelectedItem = row.DataContext;
            }
        }

        public static void columnEncodeUtf8(ref DataTable dt,string[] columnNames)
        {
            string s;
            byte[] bytes;
            foreach (DataRow dr in dt.Rows)
            {
                foreach(var name in columnNames)
                {
                    s = dr[name].ToString();
                    bytes = Encoding.Default.GetBytes(s);
                    dr[name] = Encoding.UTF8.GetString(bytes);
                }                
            }
        }

        public static bool checkJsonMapExists(string numcar)
        {
            return File.Exists($"json/map_data/{numcar}.json");            
        }


        public static string encodeUTF8(string str)
        {
            byte[] bytes = Encoding.Default.GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

    }    
}
