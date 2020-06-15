using CryptSharp;
using System;
using System.Collections.Generic;
using System.Data;
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
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        DataTable usersTable;
        int selectedUser =-1;
        public ConfigurationWindow()
        {
            InitializeComponent();            
        }

        

        private void loadUseres()
        {
            DBConnection connection = new DBConnection();
            usersTable = connection.readByAdapter("SELECT USUARIO,CASE CARGO WHEN 'MO'THEN 'APP' WHEN 'LO' THEN 'ROTINA' END AS TIPO , TOKEN FROM LOGTRANSUSU WHERE CARGO <> 'AD' ORDER BY USUARIO", null, null);
            if(usersTable != null && usersTable.Rows.Count > 0)
            {
                usersDataGrid.ItemsSource = usersTable.DefaultView;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Methods.loginType == "AD")
            {
                deleteButton.IsEnabled = true;
                tokenButton.IsEnabled = true;
            }
            else
            {
                deleteButton.IsEnabled = false;
                tokenButton.IsEnabled = false;
            }
            loadUseres();            
        }

        private void usersDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            selectedUser = usersDataGrid.SelectedIndex;
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if(usersTable != null && usersTable.Rows.Count>0 && selectedUser > -1)
            {
                string user = Convert.ToString(usersTable.Rows[selectedUser]["USUARIO"]);
                MessageBoxResult result = MessageBox.Show("Deseja excluir o usuario: "+user, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if(result == MessageBoxResult.Yes)
                {
                    DBConnection connection = new DBConnection();
                    connection.write("DELETE FROM LOGTRANSUSU WHERE USUARIO =:USUARIO", new string[] { ":USUARIO" }, new string[] { user });
                    loadUseres();
                    MessageBox.Show("Usuario excluido com sucesso", "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Seleciona um usuario", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void tokenButton_Click(object sender, RoutedEventArgs e)
        {
            if (usersTable != null && usersTable.Rows.Count > 0 && selectedUser > -1)
            {
                string user = Convert.ToString(usersTable.Rows[selectedUser]["USUARIO"]);
                string token = Guid.NewGuid().ToString().Substring(0,6);
                if (Convert.ToString(usersTable.Rows[selectedUser]["TIPO"]) == "APP")
                {
                    MessageBoxResult result = MessageBox.Show("Deseja gerar novo token para o usuario : " + user, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        DBConnection connection = new DBConnection();
                        connection.write("UPDATE LOGTRANSUSU SET TOKEN=:TOKEN WHERE USUARIO =:USUARIO", new string[] { ":USUARIO", ":TOKEN" }, new string[] { user, token });
                        loadUseres();
                        MessageBox.Show("Token gerado com sucesso", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }                
            }
            else
            {
                MessageBox.Show("Seleciona um usuario", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if(userTextBox.Text != "" && passworkTextBox.Password != "")
            {
                if (typeComboBox.SelectedIndex == 0)
                {
                    if (Methods.IsAllDigits(userTextBox.Text))
                    {
                        addUser(userTextBox.Text, passworkTextBox.Password);
                    }
                    else
                    {
                        MessageBox.Show("Caso usuario do app, o usuario tem que ser so numero (codigo de motorista)", "", MessageBoxButton.OK, MessageBoxImage.Error);
                        typeComboBox.Text = "";
                    }
                }else if (typeComboBox.SelectedIndex == 1)
                {
                    addUser(userTextBox.Text, passworkTextBox.Password);
                }
            }
            else
            {
                MessageBox.Show("falta de informação do usuarion", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private string getType()
        {
            string returned = "";
            if (typeComboBox.SelectedIndex == 0)
            {
                returned = "MO";
            }else if(typeComboBox.SelectedIndex == 1)
            {
                returned = "LO";
            }
            return returned;
        }

        private void addUser(string user,string password)
        {
            string CryptedPass = Crypter.Blowfish.Crypt(password);
            string token = Guid.NewGuid().ToString().Substring(0, 6);
            DBConnection connection = new DBConnection();
            connection.write("INSERT INTO LOGTRANSUSU (USUARIO,SENHA,TOKEN,CARGO) VALUES (:USUARIO,:SENHA,CASE WHEN :CARGO='LO' THEN '000000' else :TOKEN end,:CARGO)",new string[] {":USUARIO",":SENHA",":TOKEN",":CARGO"}, new string[] {user,CryptedPass,token,getType()});
            MessageBox.Show("Usuario foi adicionado com sucesso", "", MessageBoxButton.OK, MessageBoxImage.Information);
            userTextBox.Text = "";
            passworkTextBox.Password = "";
            loadUseres();
        }     
    }
}
