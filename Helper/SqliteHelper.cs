using AIP.DataObj;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace AIP.Helper
{
    class SqliteHelper
    {
        public SQLiteConnection SQLiteConn = new SQLiteConnection(StaticSetting.SQLitePATH);

        public SqliteHelper()
        {
            SQLiteConn.Open();
            SQLiteCommand sqlCom = SQLiteConn.CreateCommand();
            sqlCom.CommandText = "VACUUM";
            sqlCom.ExecuteNonQuery();
        }
    }
}
