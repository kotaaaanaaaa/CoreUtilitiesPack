using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Reflection;

namespace CoreUtilitiesPack
{
    public class SqliteAccessor
    {
        private string connectionString = @"Data Source=default.db;";

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
        /// DELETEする
        /// </summary>
        /// <param name="table">対象のテーブル</param>
        /// <param name="record">DELETEするデータ</param>
        /// <returns>DELETEしたレコード数</returns>
        public int Delete(string table, List<Dictionary<string, object>> records)
        {
            var result = 0;

            foreach (var record in records)
            {
                var conditions = new List<string>();
                foreach (var field in record)
                {
                    if (field.Value != null)
                    {
                        conditions.Add(field.Key + " = @" + field.Key);
                    }
                    else
                    {
                        conditions.Add(field.Key + " IS NULL");
                    }
                }

                var sql = "DELETE FROM " + table + " WHERE " + string.Join(" and ", conditions);

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

        /// <summary>
        /// Dictionaryをクラスに変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="record"></param>
        public void ToRecord<T>(Dictionary<string, object> dictionary, out T record)
        {
            record = (T)Activator.CreateInstance(typeof(T));

            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute(typeof(RecordAttribute)) as RecordAttribute;
                if (attribute == null) { continue; }

                if (dictionary.ContainsKey(attribute.Name))
                {
                    property.SetValue(record, Convert.ChangeType(dictionary[attribute.Name], property.PropertyType));
                }
            }
        }

        /// <summary>
        /// Dictionaryをクラスに変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionaries"></param>
        /// <param name="records"></param>
        public void ToRecords<T>(List<Dictionary<string, object>> dictionaries, out List<T> records)
        {
            records = new List<T>();
            foreach (var dictionary in dictionaries)
            {
                var record = (T)Activator.CreateInstance(typeof(T));
                ToRecord(dictionary, out record);
                records.Add(record);
            }
        }

        /// <summary>
        /// クラスをDictionaryに変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="record"></param>
        /// <param name="dictionary"></param>
        public void ToDictionary<T>(T record, out Dictionary<string, object> dictionary)
        {
            dictionary = new Dictionary<string, object>();
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute(typeof(RecordAttribute)) as RecordAttribute;
                if (attribute == null) { continue; }
                dictionary.Add(attribute.Name, property.GetValue(record));
            }
        }

        /// <summary>
        /// クラスをDictionaryに変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="records"></param>
        /// <param name="dictionaries"></param>
        public void ToDictionaries<T>(List<T> records, out List<Dictionary<string, object>> dictionaries)
        {
            dictionaries = new List<Dictionary<string, object>>();
            foreach (var record in records)
            {
                var dictionary = new Dictionary<string, object>();
                ToDictionary(record, out dictionary);
                dictionaries.Add(dictionary);
            }
        }

        /// <summary>
        /// テーブル作成用のSQLを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tableName"></param>
        /// <param name="record"></param>
        /// <returns>SQL文</returns>
        public static string GetCreateTableSQL<T>(string tableName, T record)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var primaryKeys = new List<string>();
            var fields = new List<string>();

            var sql = "CREATE TABLE " + tableName + " (";
            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute(typeof(RecordAttribute)) as RecordAttribute;
                if (attribute == null) { continue; }

                if (attribute.Type == RecordAttribute.FieldType.NONE)
                {
                    sql += attribute.Name + ",";
                }
                else
                {
                    sql += attribute.Name + " " + attribute.Type + ",";
                }

                if (attribute.Primary)
                {
                    primaryKeys.Add(attribute.Name);
                }
                fields.Add(attribute.Name);
            }

            sql += "PRIMARY KEY (";
            if (primaryKeys.Count != 0)
            {
                sql += string.Join(", ", primaryKeys);
            }
            else
            {
                sql += string.Join(", ", fields);

            }
            sql += "))";

            return sql;
        }
    }
}
