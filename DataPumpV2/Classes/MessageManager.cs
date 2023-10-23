using System;
using System.Collections.Generic;

namespace DataPumpV2.Classes
{
    internal class MessageManager
    {
        //Specified variables below are for console elements and standard colors of things with logging.
        public readonly static ConsoleColor standardLoggingColor = ConsoleColor.White;
        public readonly static ConsoleColor warningLoggingColor = ConsoleColor.Yellow;
        public readonly static ConsoleColor errorLoggingColor = ConsoleColor.Red;
        public readonly static ConsoleColor noticeLoggingColor = ConsoleColor.Cyan;
        public readonly static ConsoleColor debugLoggingColor = ConsoleColor.Magenta;//TBC
        public readonly static ConsoleColor standardBgColor = ConsoleColor.Black;

        /// <summary>
        /// This method is used for printing anything to the console, and whether that needs logging or not, and severity of the issue.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="severity"></param>
        /// <param name="doLog"></param>
        /// TO:DO - logging.
        internal static void InitializeMessage(AppSteps Context, string message, SeverityEnum severity)
        {
            string timer = "[" + DateTime.Now + "] ";
            string finalMessage = "";

            switch(severity)
            {
                case SeverityEnum.Warning:
                    Console.ForegroundColor = warningLoggingColor;
                    finalMessage = timer + ": (WARNING) " + message;
                    break;

                case SeverityEnum.Error:
                    Console.ForegroundColor = errorLoggingColor;
                    finalMessage = timer + ": (ERROR) " + message;
                    break;

                case SeverityEnum.Debug:
                    Console.ForegroundColor = debugLoggingColor;
                    finalMessage = timer + ": (DEBUG) " + message;
                    break;

                case SeverityEnum.Notice:
                    Console.ForegroundColor = noticeLoggingColor;
                    finalMessage = timer + ": (NOTICE) " + message;
                    break;

                case SeverityEnum.Info:
                    Console.ForegroundColor = standardLoggingColor;
                    finalMessage = timer + ": " + message;
                    break;
            }        

            if (!(AppSettings.AppDebug == false && severity == SeverityEnum.Debug))
            {
                Console.WriteLine(finalMessage);
            }

            //Logging of the process - append for needed severity.
            if (severity == SeverityEnum.Warning || severity == SeverityEnum.Error || severity == SeverityEnum.Info)
            {
                FileManager.WriteToLog(finalMessage);
            }

            //Logging of the process - append for needed severity.
            if (severity == SeverityEnum.Warning || severity == SeverityEnum.Error)
            {
                SendToDB(severity, Context.ToString(), message);
            }

            if (severity == SeverityEnum.Error) { Program.StopApplication(); }
        }

        //TBC
        private static void SendToDB(SeverityEnum type, string Context, string Message)
        {
            //Assembling a dictionary to be passed in SqlParams
            Dictionary<string, object> NewNotification = new Dictionary<string, object>();

            NewNotification.Add("@ResourceName", Globals.computerName);
            NewNotification.Add("@Type", type.ToString());
            NewNotification.Add("@Context", Context);
            NewNotification.Add("@Details", Message);

            //We create a record in DB for the specified serial number.
            DatabaseManager.ExecuteStoredProcedure(SQLProcedures.NewNotification, NewNotification);
        }
    }
}
