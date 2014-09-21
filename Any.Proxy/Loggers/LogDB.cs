using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using Any.Logs.Extentions;

namespace Any.Proxy.Loggers
{
    public class LogDB
    {
        private readonly static string Filename = Path.Combine(Environment.CurrentDirectory, "log.sqlite");

        private readonly static string СonnectionString = String.Format("data source={0};New=True;UseUTF16Encoding=True", Filename);

        public static void Initialize()
        {
            if (File.Exists(Filename)) return;

            const string sqlLog = @"CREATE TABLE 'Logs' ('Id' INTEGER PRIMARY KEY  NOT NULL ,'Summary' TEXT NOT NULL ,'Description' TEXT NOT NULL ,'Time' DATETIME NOT NULL ,'Type' INTEGER NOT NULL ,'ConnectionId' INTEGER NOT NULL )";

            var previousConnectionState = ConnectionState.Closed;
            using (var connect = new SQLiteConnection(СonnectionString))
            {
                try
                {
                    previousConnectionState = connect.State;
                    if (connect.State == ConnectionState.Closed)
                    {
                        connect.Open();
                    }

                    var command = new SQLiteCommand(sqlLog, connect);
                    command.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.GetFullMessage());
                }
                finally
                {
                    if (previousConnectionState == ConnectionState.Closed)
                    {
                        connect.Close();
                    }
                }
            }
        }

        public static void Push(string summary, string description, DateTime time, EventType type, string connectionId)
        {
            const string sqlQuery = @"INSERT INTO Logs (Summary, Description, Time, Type, TransactionId) VALUES (@Summary, @Description, @Time, @Type, @ConnectionId)";

            var previousConnectionState = ConnectionState.Closed;
            using (var connect = new SQLiteConnection(СonnectionString))
            {
                try
                {
                    previousConnectionState = connect.State;
                    if (connect.State == ConnectionState.Closed)
                    {
                        connect.Open();
                    }

                    var command = new SQLiteCommand(sqlQuery, connect);

                    command.Parameters.Add(new SQLiteParameter
                    {
                        DbType = DbType.String,
                        ParameterName = "@Summary",
                        Value = summary
                    });

                    command.Parameters.Add(new SQLiteParameter
                    {
                        DbType = DbType.String,
                        ParameterName = "@Description",
                        Value = description
                    });

                    command.Parameters.Add(new SQLiteParameter
                    {
                        DbType = DbType.DateTime2,
                        ParameterName = "@Time",
                        Value = time
                    });

                    command.Parameters.Add(new SQLiteParameter
                    {
                        DbType = DbType.Int32,
                        ParameterName = "@Type",
                        Value = (int)type
                    });

                    command.Parameters.Add(new SQLiteParameter
                    {
                        DbType = DbType.Int32,
                        ParameterName = "@ConnectionId",
                        Value = connectionId
                    });

                    command.ExecuteNonQuery();

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.GetFullMessage());
                }
                finally
                {
                    if (previousConnectionState == ConnectionState.Closed)
                    {
                        connect.Close();
                    }
                }
            }
        }
    }
}