using Oracle.ManagedDataAccess.Client;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CryptSharp;

namespace transport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DBConnection dbconnecttion = new DBConnection();            
            String user = loginUserTextBox.Text;
            String pass = loginPassTextBox.Password;
            if(user != String.Empty && pass != String.Empty)
            {
                OracleParameter parameter = new OracleParameter();

                DataTable dt = dbconnecttion.readByAdapter("SELECT * FROM LOGTRANSUSU WHERE USUARIO = :USUARIO AND CARGO IN ('LO','AD')" ,
                    new string[]{":USUARIO"}, new string[] {user});
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        try
                        {
                            if (Crypter.CheckPassword(pass, Convert.ToString(dt.Rows[0]["SENHA"])))
                            {
                                Methods.loginType = Convert.ToString(dt.Rows[0]["CARGO"]);
                                var selectWindow = new SelectWindow();
                                selectWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Senha errada", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        }                        
                    }
                    else
                    {
                        MessageBox.Show("Usuário errado", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {

                }                
            }
            else
            {
                MessageBox.Show("Insere usuário ou senha","Erro",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            
        }

  

      
    }
}
