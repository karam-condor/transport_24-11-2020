using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using Oracle.ManagedDataAccess.Client;
namespace transport
{
    class DBConnection
    {
        String connStr = "Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = 192.168.0.26)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = LOGISTIC))); User Id =LOGISTICA ; Password=logist;";
        OracleConnection conn;
        private void openConnection()
        {
            try
            {
                conn = new OracleConnection(connStr);
                conn.Open();                                    
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        public void write(String sql, String[] parasNames , String[] paramsValues)
        {
            try
            {
              openConnection();
              OracleCommand cmd = new OracleCommand(sql, conn);
              if (parasNames != null && paramsValues != null)
              {
                  cmd.BindByName = true;
                  if (parasNames != null && paramsValues != null)
                  {
                      for (int i = 0; i < paramsValues.Length; i++)
                      {
                          cmd.Parameters.Add(parasNames[i], paramsValues[i]);
                      }
                  }
              }
              cmd.ExecuteNonQuery();
              closeConnection();
            }
            catch(OracleException ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }                        
        }
        

        public DataTable readByAdapter(String sql, String[] parasNames, String[] paramsValues)
        {
            try
            {
              DataTable dt = new DataTable();
              openConnection();
              OracleCommand cmd = new OracleCommand(sql, conn);                
              cmd.BindByName = true;
              if (parasNames != null && paramsValues != null)
              {
                  for (int i = 0; i < paramsValues.Length; i++)
                  {
                      cmd.Parameters.Add(parasNames[i], paramsValues[i]);
                  }
              }
              OracleDataAdapter adapter = new OracleDataAdapter(cmd);                
              adapter.Fill(dt);
              closeConnection();
              return dt;
            }
            catch(OracleException ex)
            {
                MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }            
        }


        private void closeConnection()
        {
            conn.Close();
            conn.Dispose();
        }

    }
}
