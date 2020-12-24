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


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadUseres();
        }

        private void loadUseres()
        {
            DBConnection connection = new DBConnection();
            usersTable = connection.readByAdapter("SELECT USUARIO,CASE CARGO WHEN 'MO'THEN 'APP' WHEN 'TR' THEN 'ROTINA(TRANSPORTE)' WHEN 'PD' THEN 'ROTINA(PENDENCIA)' END AS TIPO , TOKEN FROM "+tabelas.userTable+" WHERE CARGO <> 'AD' ORDER BY USUARIO", null, null);
            if(usersTable != null && usersTable.Rows.Count > 0)
            {
                usersDataGrid.ItemsSource = usersTable.DefaultView;
            }
            else{
                usersDataGrid.ItemsSource = new DataTable().DefaultView;
            }
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
                    connection.write("DELETE FROM "+ tabelas.userTable + " WHERE USUARIO =:USUARIO", new string[] { ":USUARIO" }, new string[] { user });
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
                        connection.write("UPDATE "+ tabelas.userTable + " SET TOKEN=:TOKEN WHERE USUARIO =:USUARIO", new string[] { ":USUARIO", ":TOKEN" }, new string[] { user, token });
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
                        MessageBox.Show("Caso usuario do app, o campo 'usuario' deve ser preenchido em números (codigo de motorista)", "", MessageBoxButton.OK, MessageBoxImage.Error);
                        typeComboBox.Text = "";
                    }
                }else
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
            switch (typeComboBox.SelectedIndex) 
            {
                case 0:
                    returned = "MO";
                    break;
                case 1:
                    returned = "TR";
                    break;
                case 2:
                    returned = "PD";
                    break;
            }                            
            return returned;
        }

        private void addUser(string user,string password)
        {
            string CryptedPass = Crypter.Blowfish.Crypt(password);
            string token = Guid.NewGuid().ToString().Substring(0, 6);
            DBConnection connection = new DBConnection();
            connection.write("INSERT INTO "+ tabelas.userTable + " (USUARIO,SENHA,TOKEN,CARGO) VALUES (:USUARIO,:SENHA,CASE WHEN (:CARGO='TR' OR :CARGO='PD') THEN '000000' else :TOKEN end,:CARGO)", new string[] {":USUARIO",":SENHA",":TOKEN",":CARGO"}, new string[] {user,CryptedPass,token,getType()});
            MessageBox.Show("Usuario foi adicionado com sucesso", "", MessageBoxButton.OK, MessageBoxImage.Information);
            userTextBox.Text = "";
            passworkTextBox.Password = "";
            loadUseres();
        }

        private void latTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Methods.acceptJustFloatingNumbers(sender,e);
        }


        
        
    }
}
        