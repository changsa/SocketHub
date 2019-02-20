using AIP.DataObj;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;

namespace AIP.Helper
{
    class PanaDBHelper
    { 
        public SqlConnection con_Pana = null;
        public SqlCommand cmd_Pana = null;
        public SqlDataReader SqlDataReaderData = null;
        string str_PanaDBConnection = null;

        private bool ConnectPanaCIMDB()
        {
            if (StaticSetting.Database_PanaCIM_SERVERNAME.Length == 0 || StaticSetting.Database_PanaCIM_DATABASE.Length == 0
                || StaticSetting.Database_PanaCIM_UserName.Length == 0 || StaticSetting.Database_PanaCIM_Password.Length == 0)
            {
                MessageBox.Show("Necessary Value is Empty for DB");
            }
            try
            {
                str_PanaDBConnection = "SERVER=" + StaticSetting.Database_PanaCIM_SERVERNAME 
                                    + ";DATABASE=" + StaticSetting.Database_PanaCIM_DATABASE 
                                    + ";PWD=" + StaticSetting.Database_PanaCIM_UserName 
                                    + ";UID=" + StaticSetting.Database_PanaCIM_Password + ";";

                con_Pana = new SqlConnection(str_PanaDBConnection);
                cmd_Pana = new SqlCommand();
                cmd_Pana.Connection = con_Pana;

                if (con_Pana.State != ConnectionState.Open)
                {
                    con_Pana.Open();
                    return true;
                }
                return true;
            }
            catch
            {
                MessageBox.Show("Can not connect To PanaCIM DB");
                return false;
            }
        }

        public bool ReConnect2PanaCIMDB()
        {
            StaticSetting.Database_PanaCIM_Connected = ConnectPanaCIMDB();
            return StaticSetting.Database_PanaCIM_Connected;
        }

        public bool DisConnect2PanaCIMDB()
        {
            try
            {
                con_Pana.Close();
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}