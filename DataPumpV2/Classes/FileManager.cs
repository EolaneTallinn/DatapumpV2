using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static DataPumpV2.Classes.ConfigurationManager;
using static System.Net.Mime.MediaTypeNames;

namespace DataPumpV2.Classes
{
    internal class FileManager
    {
        /// <summary>
        /// This method is used for returning a literal string from a provided file path.
        /// </summary>
        /// <returns>String LiteralText</returns>//TBC
        internal static string GetFileContent(string filepath)
        {
            try
            {
                return File.ReadAllText(filepath);
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Running, "GetFileContent :" + exception.Message, SeverityEnum.Warning);
                return null;
            }
        }

        /// <summary>
        /// Method which move the logfile to the server (ftp at the moment) and delete it
        /// </summary>
        public static void MovingToServer(string DestinationDir, string FilePath)
        {
            try
            {
                FTPManager.Upload(DestinationDir, FilePath);
                File.Delete(FilePath);
                MessageManager.InitializeMessage(AppSteps.Running, "File was moved to the server and deleted", SeverityEnum.Debug);
            }
            catch (WebException exception)
            {
                MessageManager.InitializeMessage(AppSteps.Running, "MovingToServer : " + exception.Message, SeverityEnum.Error);
                MovingToErrorFolder(FilePath, FileError.ServerMoving);
            }
            catch 
            {
                MessageManager.InitializeMessage(AppSteps.Running, "MovingToServer : The file cannot be deleted", SeverityEnum.Error);
                MovingToErrorFolder(FilePath, FileError.Delete);
            }
        }

        /// <summary>
        /// Method which move the logfile to the error folder with type indicated
        /// </summary>
        internal static void MovingToErrorFolder(String FilePath, FileError Type)
        {
            string ErrorFolderPath = Path.GetDirectoryName(FilePath) + @"\Error";
            string FileName = Path.GetFileName(FilePath);
            string newNameFullPath = null;
            string message = null;

            switch (Type)
            {
                case FileError.EmptyFile:
                    message = FileName + " was skipped, due to the Result object being null (probably empty file, or not up to standard)";
                    newNameFullPath = ErrorFolderPath + @"\Empty_" + Path.GetFileName(FilePath);
                    break;

                case FileError.ServerMoving:
                    message = FileName + " was impossible to move to Server";
                    newNameFullPath = ErrorFolderPath + @"\ServerError_" + FileName;
                    break;

                case FileError.DBError:
                    message = "SQL query returns anything";
                    newNameFullPath = ErrorFolderPath + @"\DBError_" + FileName;
                    break;

                case FileError.Delete:
                    message = FileName + " was impossible to delete";
                    newNameFullPath = ErrorFolderPath + @"\DeleteError_" + FileName;
                    break;
            }

            MessageManager.InitializeMessage(AppSteps.Running, "MovingToErrorFolder : " + message, SeverityEnum.Debug);

            //We move the file with the error type added to the name
            try
            {
                if (!Directory.Exists(ErrorFolderPath))
                {
                    Directory.CreateDirectory(ErrorFolderPath);
                }
                File.Move(FilePath, newNameFullPath);
                MessageManager.InitializeMessage(AppSteps.Running, "The file was moved to error file", SeverityEnum.Info);
            }
            catch
            {
                MessageManager.InitializeMessage(AppSteps.Running, "MovingToErrorFolder : The file cannot be moved (probably already existing)", SeverityEnum.Error);
            }
        }

        /// <summary>
        /// Method to create logfile if isnt existing and append a log message//TBC
        /// </summary>
        /// <param name="message"></param>
        static DateTime ServerWriteTimer = DateTime.Now;
        internal static void WriteToLog(string message)
        {
            string dateTimeOperator = DateTime.Now.ToString("yyyyMMdd");
            
            string TempFolder = Globals.executingDirectory + @"TempFiles\";
            string TempFileName = Globals.computerName + "-" + dateTimeOperator + ".log";
            string TempFilePath = TempFolder + TempFileName;

            string ftpDir = "/2.app_logs/";

            try
            {
                //If no directory exists, create one
                if (!Directory.Exists(TempFolder)) { Directory.CreateDirectory(TempFolder); }
                //If no file exists, create one
                if (!File.Exists(TempFilePath)) { using (File.Create(TempFilePath)) { } }
                // Adding the text to the specified file
                using (var stream = new StreamWriter(new FileStream(TempFilePath, FileMode.Append)))
                {
                    stream.WriteLine(message);
                }

                if ((DateTime.Now - ServerWriteTimer) >= TimeSpan.FromMinutes(15))
                {
                    ServerWriteTimer = DateTime.Now;
                    string NewContent = FileManager.GetFileContent(TempFilePath);

                    string OldContent = System.Text.Encoding.UTF8.GetString(FTPManager.Download(ftpDir + TempFileName));

                    using (var stream = new StreamWriter(new FileStream(TempFilePath, FileMode.Truncate)))
                    {
                        stream.WriteLine(OldContent + NewContent);
                    }

                    FTPManager.Upload(ftpDir, TempFilePath);
                }
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Running, "WriteToLog : " + exception.Message, SeverityEnum.Error);
            }
        }
                
        /// <summary>
        /// Method to determine the locations of the compatible file paths of the logs
        /// </summary>
        /// <returns>true if there are new files</returns>
        internal static bool ListCompatibleFiles()
        {
            string MainDir = AppSettings.ProcessDataDir;
            List<string> Filters = AppSettings.SubDirFilter;
            List<string> AllowedExts = AppSettings.AllowedExtensions;
            
            try
            {
                List<string> productDirectories = new List<string> { MainDir };
                //Why here is an == and not !=
                if (Filters == null)
                {
                    productDirectories.Clear();
                    foreach (string Filter in Filters)
                    {
                        productDirectories.AddRange(Directory.GetDirectories(MainDir, Filter + "*", SearchOption.TopDirectoryOnly));
                    }
                }

                if (productDirectories.Count > 0)
                {
                    List<string> filePaths = new List<string>();

                    foreach (string productDirectory in productDirectories)
                    {
                        foreach (string AllowedExt in AllowedExts)
                        {
                            SearchOption searchOption = AppSettings.DeepResearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                            List<string> allFiles = Directory.GetFiles(productDirectory, "*" + AllowedExt, searchOption).ToList();

                            // exclude files with a path containing "error" or "golden"
                            allFiles = allFiles.Where(filePath =>
                                !filePath.ToLower().Contains("error") &&
                                !filePath.ToLower().Contains("golden")).ToList();

                            if (AppSettings.LogNameFilter != null)
                            {
                                // exclude files without lognamefilter
                                allFiles = allFiles.Where(filePath => Regex.IsMatch(filePath, AppSettings.LogNameFilter)).ToList();
                            }

                            filePaths.AddRange(allFiles);
                        }
                    }

                    if (filePaths.Count > 0)
                    {
                        MessageManager.InitializeMessage(AppSteps.Running, "Found " + filePaths.Count + " log files", SeverityEnum.Info);
                        LogExtraction.logFilePaths = filePaths;
                        return true;
                    }
                    else
                    {
                        MessageManager.InitializeMessage(AppSteps.Running, "No products found that matched allowed extensions : '" + string.Join(", ", AllowedExts) + "'", SeverityEnum.Notice);
                    }
                }
                else
                {
                    MessageManager.InitializeMessage(AppSteps.Running, "No product directories found that matched filters : '" + string.Join(", ", Filters) + "'", SeverityEnum.Warning);
                }
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Running, "ListCompatibleFiles : " + exception.Message, SeverityEnum.Error);
            }
            return false;
        }
    }
}
