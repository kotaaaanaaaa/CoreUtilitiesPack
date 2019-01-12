using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Reflection;

namespace CoreUtilitiesPack
{
    public class SqliteAccessor
    {
        private string connectionString = @"Data Source=:memory:;";

        /// <summary>
        /// SQLを実行する
        /// </summary>
        /// <param name="sql">実行するSQL文</param>
        /// <returns>INSERT/UPDATEした行数</returns>
        public int ExecuteNonQuery(string sql)
        {
            var result = 0;

            using (var conn = new SQLiteConnection(connectionString))
            using (var cmd = new SQLiteCommand())
            {
                conn.Open();
                cmd.Connection = conn;
                cmd.CommandText = sql;
                result = cmd.ExecuteNonQuery();
                conn.Close();

            }

            return result;
        }

        /// <summary>
        /// SQLを実行する
        /// </summary>
        /// <param name="sql">実行するSQL文</param>
        /// <returns>実行結果</returns>
        public List<Dictionary<string, object>> ExecuteQuery(string sql)
        {
            var records = new List<Dictionary<string, object>>();

            using (var conn = new SQLiteConnection(connectionString))
            using (var cmd = new SQLiteCommand())
            {
                conn.Open();
                cmd.Connection = conn;
                cmd.CommandText = sql;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader[i].GetType() == typeof(DBNull))
                            {
                                record.Add(reader.GetName(i).ToString(), null);
                            }
                            else
                            {
                                record.Add(reader.GetName(i).ToString(), reader[i]);
                            }
                        }

                        records.Add(record);
                    }
                }

                conn.Close();
            }

            return records;
        }

        /// <summary>
        /// UPSERTする(データがあれば更新する。無ければ追加する。)
        /// </summary>
        /// <param name="table">対象のテーブル</param>
        /// <param name="record">UPSERTするデータ</param>
        /// <returns>UPSERTしたレコード数</returns>
        public int Upsert(string table, Dictionary<string, object> record)
        {
            var records = new List<Dictionary<string, object>>();
            records.Add(record);
            return Upserts(table, records);
        }

        /// <summary>
        /// UPSERTする(データがあれば更新する。無ければ追加する。)
        /// </summary>
        /// <param name="table">対象のテーブル</param>
        /// <param name="records">UPSERTするデータのリスト</param>
        /// <returns>UPSERTしたレコード数</returns>
        public int Upserts(string table, List<Dictionary<string, object>> records)
        {
            var result = 0;

            foreach (var record in records)
            {
                var colums = new List<string>();
                var values = new List<string>();
                foreach (var field in record)
                {
                    colums.Add(field.Key);
                    values.Add("@" + field.Key);
                }

                var sql = "INSERT OR REPLACE INTO " + table + " (" + string.Join(", ", colums) + ") VALUES (" + string.Join(", ", values) + ")";

                using (var conn = new SQLiteConnection(connectionString))
                using (var cmd = new SQLiteCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;
                    cmd.CommandText = sql;
                    foreach (var field in record)
                    {
                        cmd.Parameters.Add(new SQLiteParameter(field.Key, field.Value));
                    }

                    result += cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            return result;
        }

        /// <summary>
        /// テーブルのリストを取得する
        /// </summary>
        /// <returns>テーブルのリスト</returns>
        public List<string> GetTablesList()
        {
            var tableList = new List<string>();

            var sql = "SELECT * FROM sqlite_master WHERE type='table'";
            var records = ExecuteQuery(sql);
            foreach (var record in records)
            {
                tableList.Add(record["tbl_name"].ToString());
            }

            return tableList;
        }

        /// <summary>
        /// テーブルがDBにあるか確認する
        /// </summary>
        /// <param name="tableName">テーブル名</param>
        /// <returns></returns>
        public Boolean HasTable(string tableName)
        {
            var tableList = GetTablesList();
            if (tableList.Contains(tableName))
            {
                return true;
            }
            return false;
        }
    }
}
