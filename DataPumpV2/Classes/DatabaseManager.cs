using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Linq;

namespace DataPumpV2.Classes
{
    /// <summary>
    /// This class is meant for everything that is related to the Database data manipulation and exectuion.
    /// </summary>
    internal class DatabaseManager
    {
        /// <summary>
        /// This Method executes a provided to it stored procedure, with it's adjacent parameters.
        /// </summary>
        /// <param name="storedProcedureName"></param>
        /// <param name="procedureParameters"></param>
        /// <returns>dictionnary of query's result</returns>
        internal static List<Dictionary<string, object>> ExecuteStoredProcedure(SQLProcedures.ProcedureInfo storedProcedureName, Dictionary<string, object> procedureParameters = null)
        {
            List<Dictionary<string, object>> outResult = new List<Dictionary<string, object>>();

            string[] logins;

            if (storedProcedureName == SQLProcedures.NewNotification)
            {
                logins = Globals.SQLLogins["ExceptionDB"];
            }
            else
            {
                logins = Globals.SQLLogins["DatapumpDB"];
            }

            try
            {
                using (SqlConnection sqlConn = new SqlConnection($@"Server={logins[0]};Database={logins[1]};User Id={logins[2]};Password={logins[3]};"))
                {
                    sqlConn.Open();

                    // create a command object identifying the stored procedure
                    SqlCommand sqlCmd = new SqlCommand(storedProcedureName.Name, sqlConn);

                    // set the command object so it knows to execute a stored procedure
                    sqlCmd.CommandType = CommandType.StoredProcedure;

                    // add parameter to command, which will be passed to the stored procedure
                    if(procedureParameters != null)
                    {
                        foreach (KeyValuePair<string, object> param in procedureParameters)
                        {
                            sqlCmd.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    // execute the command
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        //if storedProcedure returns data, we read it
                        if (storedProcedureName.Return)
                        {
                            while(reader.Read())
                            {
                                Dictionary<string, object> subOutResult = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    subOutResult.Add(reader.GetName(i), reader.GetValue(i));
                                }
                                outResult.Add(subOutResult);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Running, "ExecuteStoredProcedure : " + exception.Message, SeverityEnum.Error);
            }

            if (storedProcedureName.Return && outResult.Count <= 0)
            {
                throw new ArgumentException("There were no records returned from the stored procedure - '" + storedProcedureName.Name + "'");
            }
            
            MessageManager.InitializeMessage(AppSteps.Running, "Procedure '" + storedProcedureName.Name + "' - Executed successfuly.", SeverityEnum.Debug);

            return outResult;
        }

        /// <summary>
        /// This method check whether the DBs are available.
        /// </summary>
        /// <param name="sqlConn"></param>
        internal static void PingDB()
        {
            try
            {
                foreach (var kvp in Globals.SQLLogins)
                {
                    string[] loginInfo = kvp.Value;
                    using (SqlConnection sqlConn = new SqlConnection($@"Server={loginInfo[0]};Database={loginInfo[1]};User Id={loginInfo[2]};Password={loginInfo[3]};"))
                    {
                        MessageManager.InitializeMessage(AppSteps.Initializion, $"Trying to connect to {loginInfo[1]}", SeverityEnum.Debug);
                        sqlConn.Open();
                        MessageManager.InitializeMessage(AppSteps.Initializion, $"Successfully established a connection to {loginInfo[1]}.", SeverityEnum.Debug);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Initializion, "PingDB :" + exception.Message, SeverityEnum.Error);
            }
        }

        /// <summary>
        /// Method to check if the computer exists in the Database
        /// the sql query return count of presence resourcename, not a bool, it's converted to true if is >0
        /// </summary>
        /// <param name="ResourceName"></param>
        /// <returns>bool</returns>
        internal static bool CheckResourcePresence(string ResourceName)
        {
            Dictionary<string, object> param = new Dictionary<string, object> { { "@ResourceName", ResourceName } };

            int result = (int)(ExecuteStoredProcedure(SQLProcedures.CheckResourcePresence, param).First()["Presence"]);//On DB, the resource name must be written in uppercase.

            if (result != 0){
                return true;
            }
            else 
            { 
                return false; 
            }
        }
    }
}
