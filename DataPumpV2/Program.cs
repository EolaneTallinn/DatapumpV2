using DataPumpV2.Classes;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DataPumpV2
{
    internal class Program
    {
        /// <summary>
        /// Initalization of the application
        /// </summary>
        internal static void Main()
        {
            InitializeConsoleApplication();
            ActionLoop();
        }

        /// <summary>
        /// This is an initalization method of this console application.
        /// It sets all of the visual inputs, and determines whether the configuration provided is correct.
        /// </summary>
        internal static void InitializeConsoleApplication()
        {
            //Purely Visual Manipulations
            Console.BackgroundColor = MessageManager.standardBgColor;
            Console.ForegroundColor = MessageManager.standardLoggingColor;
            Console.Title = "DataPump-V2 : Loading...";

            //Logging of the initial Messages
            MessageManager.InitializeMessage(AppSteps.Initializion, "----* APPLICATION START *----", SeverityEnum.Info);
            MessageManager.InitializeMessage(AppSteps.Initializion, "Initializing a session...", SeverityEnum.Info);

            //Loading of the necessary configuration files
            ConfigurationManager.LoadConfiguration();
            DatabaseManager.PingDB();

            MessageManager.InitializeMessage(AppSteps.Initializion, "Computer Name -> " + Globals.computerName, SeverityEnum.Debug);
            if (!DatabaseManager.CheckResourcePresence(Globals.computerName))
            {
                MessageManager.InitializeMessage(AppSteps.Initializion, "Your computer isn't registered in the Database", SeverityEnum.Warning);
            }

            //Finalization of the loading
            Console.Title = "DataPump-V2 : " + Globals.computerName;
            MessageManager.InitializeMessage(AppSteps.Initializion, "DataPump-V2 has been loaded. For help with this application, contact 'helpdesk@eolane.ee'", SeverityEnum.Info);
            
            //Graphical separator
            Console.WriteLine("-----------");
        }

        /// <summary>
        /// This is the main loop of action.
        /// </summary>
        internal static void ActionLoop()
        {
            while (Globals.isRunning)
            {
                ConfigurationManager.CheckConfigurationChange();
                
                Thread.Sleep(Globals.msTimeOut);

                if (FileManager.ListCompatibleFiles())
                {
                    LogExtraction.ResultWork();
                    //Graphical separator
                    Console.WriteLine("------------------");
                }

                Thread.Sleep(Globals.msMainLoopTimeOut);
            }
        }

        /// <summary>
        /// This basically restarts the application.
        /// </summary>
        /// <param name="message"></param>
        internal static void UpdateApplication()
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string exeName = Path.GetFileName(exePath);
            string exeDir = Path.GetDirectoryName(exePath);
            string TempDir = exeDir + @"\TempFiles\";
            string VersionFileName = Path.GetFileName(Globals.VersionPath);

            try
            {
                MessageManager.InitializeMessage(AppSteps.Restart, "Restarting", SeverityEnum.Warning);

                File.WriteAllBytes(TempDir + exeName, FTPManager.Download("/3.ScriptVault/" + exeName));
                File.WriteAllBytes(TempDir + VersionFileName, FTPManager.Download("/3.ScriptVault/" + VersionFileName));

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true //Disable the window display
                };

                process.StartInfo = startInfo;
                process.Start();

                process.StandardInput.Write(
                    "ping 0.0.0.0\n" + //We use a random function that takes time because the "timeout" function does not allow another instruction after it.
                    "move /y \"{0}\" \"{1}\"\n" +
                    "move /y \"{2}\" \"{3}\"\n" +
                    "cd \"{4}\"\n" +
                    "start {5}\n" +
                    "exit"
                    , TempDir + exeName, exePath, TempDir + VersionFileName, Globals.VersionPath, exeDir, exeName);

                Environment.Exit(0);
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Restart, "RestartApplication : " + exception.Message, SeverityEnum.Error);
            }
        }

        /// <summary>
        /// This method stops the application from executing further.
        /// </summary>
        internal static void StopApplication()
        {
            Console.ForegroundColor = MessageManager.standardLoggingColor;
            Console.WriteLine("Application terminates, Press ANY key to exit.");
            Console.ReadKey();

            Environment.Exit(0);
        }
    }
}