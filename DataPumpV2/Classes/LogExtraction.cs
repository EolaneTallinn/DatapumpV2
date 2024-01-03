using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data.Common;
using System.Runtime.Remoting.Messaging;
using System.Reflection.Emit;
using System.Net.Sockets;
using Renci.SshNet.Messages;
using YamlDotNet.Core.Tokens;

namespace DataPumpV2.Classes
{
    /// <summary>
    /// This class is meant for the High-Level importing sequence (Basically it will do all the DB, path info and etc.)
    /// </summary>
    internal class LogExtraction
    {
        internal static List<string> logFilePaths = new List<string>();

        //TBC add check values
        private class ProductResult
        {
            public string SerialNb { get; set; }
            public string Status { get; set; }
            public string ResourceName { get; set; }
            public string Program { get; set; }
            //public string ResultForm { get; set; }
            public DateTime? Timedate { get; set; }
        }

        //TBC add check values
        private class RawFileAttribute
        {
            internal RawFileAttribute(string attributeName, string data, Regex regex)
            {
                this.AttributeName = attributeName;
                this.Data = data;
                this.Regex = regex;
            }
            public string AttributeName { get; set; }
            public string Data { get; set; }
            public Regex Regex { get; set; }
        }

        //TBC add check values
        private class FileAttribute
        {
            internal FileAttribute(string attributName, string value)
            {
                this.AttributeName = attributName;
                this.Value = value;
            }
            public string AttributeName { get; set; }
            public string Value { get; set; }
        }

        private static List<ProductResult> ProductResultCollection = new List<ProductResult>();
        private static List<FileAttribute> FileAttributesCollection = new List<FileAttribute>();

        private static List<string> StandardCSVHeader = new List<string> { "SerialNb", "Resource", "Form", "Result", "DateTime", "Operation", "Program" };

        /// <summary>
        /// This method will be updating the DB, moving the files and deleting them, based on the results that it got from the Below-Class 'RegexLibrary'.
        /// </summary>
        internal static void ResultWork()
        {
            try
            {
                foreach (string logFilePath in logFilePaths)
                {
                    string fileName = Path.GetFileName(logFilePath);
                    MessageManager.InitializeMessage(AppSteps.Running, "Processing with " + fileName, SeverityEnum.Info);

                    FileAttributes attributes = File.GetAttributes(logFilePath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes = attributes & ~FileAttributes.ReadOnly;
                        File.SetAttributes(logFilePath, attributes);
                    }

                    //We create our product result object, to populate it with results from the log file.
                    ReturnProductResultsFromLog(logFilePath);
                    
                    //We only run SQL query if the file contains valid data
                    if (ProductResultCollection.Count > 0)
                    {
                        //Location on the pressumed network folder
                        //string serverLogPath = Globals.ftpPath + RegexLibrary.GetFirstGroup(new Regex(@"Prozessdaten(\\.+)"), Path.GetDirectoryName(logFilePath));//TBC
                        string serverLogPath = RegexLibrary.GetFirstGroup(new Regex(@"Prozessdaten(\\.+)"), Path.GetDirectoryName(logFilePath));

                        //Assembling a dictionary to be passed in SqlParams
                        Dictionary<string, object> NewLogFile = new Dictionary<string, object>();
                        NewLogFile.Add("@FileName", fileName);
                        NewLogFile.Add("@DirectoryName", @"\\varroc\results\" + Globals.computerName + serverLogPath);
                        NewLogFile.Add("@FileCreation", File.GetCreationTime(logFilePath));

                        //NewLogFile procedure call
                        Dictionary<string, object> LogFile = DatabaseManager.ExecuteStoredProcedure(SQLProcedures.NewLogFile, Globals.SQLLogins["DatapumpDB"], NewLogFile).First();


                        #region OLD DATAPUMP SYS
                        //Assembling a dictionary to be passed in SqlParams. THIS IS TEMPORARY
                        Dictionary<string, object> NewLogFileOld = new Dictionary<string, object>();
                        NewLogFileOld.Add("@MD5", 1234);
                        NewLogFileOld.Add("@FileName", fileName);
                        NewLogFileOld.Add("@DirectoryName", @"\\varroc\results\" + Globals.computerName + serverLogPath);

                        //Creating the record also in the old DB to handle the transition between the 2 programs. THIS IS TEMPORARY
                        Dictionary<string, object> LogFileOld = DatabaseManager.ExecuteStoredProcedure(SQLProcedures.NewLogFile, Globals.SQLLogins["ProductionDB"], NewLogFileOld).First();
                        #endregion


                        foreach(FileAttribute attribute in FileAttributesCollection)
                        {
                            //Assembling a dictionary to be passed in SqlParams
                            Dictionary<string, object> NewFileAttributes = new Dictionary<string, object>();
                            NewFileAttributes.Add("@LogfileHandle", LogFile["Handle"]);
                            NewFileAttributes.Add("@Attribute", attribute.AttributeName);
                            NewFileAttributes.Add("@Value", attribute.Value);

                            //NewLogFile procedure call
                            DatabaseManager.ExecuteStoredProcedure(SQLProcedures.NewActivityAttribute, Globals.SQLLogins["DatapumpDB"], NewFileAttributes);


                            #region OLD DATAPUMP SYS
                            //Assembling a dictionary to be passed in SqlParams. THIS IS TEMPORARY
                            Dictionary<string, object> NewFileAttributesOld = new Dictionary<string, object>();
                            NewFileAttributes.Add("@ActivityTraceFileHandle", LogFile["Handle"]);
                            NewFileAttributes.Add("@DataAttr", attribute.AttributeName);
                            NewFileAttributes.Add("@DataValue", attribute.Value);

                            //Creating the record also in the old DB to handle the transition between the 2 programs. THIS IS TEMPORARY
                            DatabaseManager.ExecuteStoredProcedure(SQLProcedures.AddActivityTraceField, Globals.SQLLogins["ProductionDB"], NewFileAttributesOld);
                            #endregion
                        }


                        //We only continue if we obtain result from Newlogfile procedure
                        if (LogFile.Count > 0)
                        {
                            foreach (ProductResult productResult in ProductResultCollection)
                            {
                                if (productResult.Timedate == DateTime.MinValue) { productResult.Timedate = File.GetLastWriteTime(logFilePath); }
                                if (productResult.ResourceName == "") { productResult.ResourceName = Globals.computerName; }

                                //Assembling a dictionary to be passed in SqlParams
                                Dictionary<string, object> NewActivityTrace = new Dictionary<string, object>();
                                NewActivityTrace.Add("@SerialNb", productResult.SerialNb);
                                NewActivityTrace.Add("@Status", productResult.Status);
                                NewActivityTrace.Add("@ResourceName", productResult.ResourceName);
                                NewActivityTrace.Add("@Operation", AppSettings.OperationName);
                                NewActivityTrace.Add("@Program", productResult.Program);
                                NewActivityTrace.Add("@LogfileHandle", LogFile["Handle"]);
                                NewActivityTrace.Add("@Created", productResult.Timedate);

                                //We create a record in DB for the specified serial number.
                                DatabaseManager.ExecuteStoredProcedure(SQLProcedures.NewActivityTrace, Globals.SQLLogins["DatapumpDB"], NewActivityTrace);


                                #region OLD DATAPUMP SYS
                                //Assembling a dictionary to be passed in SqlParams. THIS IS TEMPORARY
                                Dictionary<string, object> NewActivityTraceOld = new Dictionary<string, object>();
                                NewActivityTraceOld.Add("@Serial", productResult.SerialNb);
                                NewActivityTraceOld.Add("@Result", productResult.Status);
                                NewActivityTraceOld.Add("@Computer", productResult.ResourceName);
                                NewActivityTraceOld.Add("@Operation", AppSettings.OperationName);
                                NewActivityTraceOld.Add("@ProgramName", productResult.Program);
                                NewActivityTraceOld.Add("@FileHashHandle", LogFileOld["FileHashHandle"]);
                                NewActivityTraceOld.Add("@TestDate", productResult.Timedate);
                                NewActivityTraceOld.Add("@EmployeeID", String.Empty);

                                //Creating the record also in the old DB to handle the transition between the 2 programs. THIS IS TEMPORARY
                                DatabaseManager.ExecuteStoredProcedure(SQLProcedures.NewActivityTraceOld, Globals.SQLLogins["ProductionDB"], NewActivityTraceOld);
                                #endregion
                            }

                            FileManager.MovingToServer("1.test_logs" + serverLogPath, logFilePath);
                        }
                        else
                        {
                            FileManager.MovingToErrorFolder(logFilePath, FileError.DBError);
                        }
                    }
                    else
                    {
                        FileManager.MovingToErrorFolder(logFilePath, FileError.EmptyFile);
                    }
                    MessageManager.InitializeMessage(AppSteps.Running, "--- Process successful ---", SeverityEnum.Info);
                }
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Running, "ResultWork : " + exception.Message, SeverityEnum.Warning);
            }
        }

        private static void ReturnProductResultsFromLog(string logFilePath)
        {
            //empty ProductResultCollection of previous products
            ProductResultCollection = new List<ProductResult>();

            string logFileContent = FileManager.GetFileContent(logFilePath);

            List<string> ContentAsList = logFileContent.Split('\n').ToList();//TBC

            List<Dictionary<string, string>> Records = new List<Dictionary<string, string>>();

            List<RawFileAttribute> rawAttributes = new List<RawFileAttribute>();

            bool CSVMode = false;

            Regex serialNumberPattern = new Regex("");
            Regex ResultPattern = new Regex("");
            Regex programNamePattern = new Regex("");
            Regex ResourcePattern = new Regex("");
            Regex ResultformPattern = new Regex("");
            Regex datePattern = new Regex("");
            Regex timePattern = new Regex("");

            string[] Header = null;
            string csvContent = null;

            string CustomProgram = null;
            string CustomResource = null;
            string CustomStatus = null;
            DateTime? CustomDateTime = null;

            string PassResultRepresentation = null;
            //string resultForm = null;


            try
            {
                //Preset Regexes in function of ImportMode
                switch (AppSettings.ImportMode)
                {
                    case ImportMode.FAUSNG:
                        serialNumberPattern = new Regex(@"\nSerial Number:\s*([\w]+)");
                        ResultPattern = new Regex(@"\nUUT Result:\s*([\w]+)");
                        ResourcePattern = new Regex(@"\nStation ID:\s*([a-zA-Z0-9, ]+)");
                        datePattern = new Regex(@"\nDate:\s*([a-zA-Z0-9, ]+)");
                        timePattern = new Regex(@"\nTime:\s*([0-9:]+ (?:AM|PM))");

                        //progname is the folder name , no in the filecontent
                        CustomProgram = logFilePath.Replace(AppSettings.ProcessDataDir, "").Split('\\')[1];
                        PassResultRepresentation = "Passed";


                        rawAttributes.Add(new RawFileAttribute("ResultForm", "Pattern", null));

                        break;

                    case ImportMode.FAURGB:
                        serialNumberPattern = new Regex(@"SN;[^;]*;([^;]*);");
                        ResultPattern = new Regex(@"(?:Passed \(=0\) \/ Failed\(<>0\));\d+;(\d+);");

                        //progname is the folder name , no in the filecontent
                        CustomProgram = logFilePath.Replace(AppSettings.ProcessDataDir, "").Split('\\')[1];
                        PassResultRepresentation = "0";
                        rawAttributes.Add(new RawFileAttribute("ResultForm", "Pattern", null));

                        break;

                    case ImportMode.VAREOL:
                        serialNumberPattern = new Regex(@",\s*(00\w{13,}|20\w{13,}|21\w{13,}|06\d{10,}\w{3,}|06\d{14}\w{3,}),");
                        ResultPattern = new Regex(@",([PF]),");
                        programNamePattern = new Regex(@",([^,]*[-_][^,]*),");
                        datePattern = new Regex(@",(\d{8}),");
                        timePattern = new Regex(@",([0-2][0-9][0-5][0-9][0-5][0-9]),");

                        PassResultRepresentation = "P";
                        logFileContent = "," + ContentAsList[0] + ",";//we add "," to be sure there are here, for the regex match

                        rawAttributes.Add(new RawFileAttribute("ResultForm", "Pattern", null));

                        break;

                    case ImportMode.EOLANE:
                        CSVMode = true;
                        Header = new string[] { "Form", "Index", "Result", "H1", "SerialNb", "DateTime", "H2" };
                        csvContent = string.Join("\n", ContentAsList.Where(line => line.Contains(";")));

                        CustomProgram = Path.GetFileName(logFilePath).Split('_')[0];
                        PassResultRepresentation = "p";

                        Records = StringToCSV(Header, csvContent, ";");
                        Records = CompletePatternId(Records);


                        rawAttributes.Add(new RawFileAttribute("ResultForm", "Pattern", null));

                        break;

                    case ImportMode.TERADYNE:
                        CSVMode = true;
                        Header = new string[] { "Form", "Index", "Result", "H1", "SerialNb", "DateTime", "H2" };
                        csvContent = string.Join("\n", ContentAsList.Where(line => line.Contains(";")));

                        CustomProgram = ContentAsList[0].Split(',')[2];
                        PassResultRepresentation = "P";

                        Records = StringToCSV(Header, csvContent, ";");
                        Records = CompletePatternId(Records);

                        rawAttributes.Add(new RawFileAttribute("ResultForm", "Panel", null));

                        break;

                    case ImportMode.VARLDM:
                        serialNumberPattern = new Regex(@"\nSerial Number:\s*([a-zA-Z0-9]+)");
                        ResultPattern = new Regex(@"\nUUT Result:\s*([a-zA-Z0-9]+)");
                        programNamePattern = new Regex(@"\n(?:Part Number:|Product ID:)\s*([A-Za-z0-9]+)");
                        ResourcePattern = new Regex(@"\nStation ID:\s*([a-zA-Z0-9, ]+)");
                        datePattern = new Regex(@"\nDate:\s*([a-zA-Z0-9, ]+)");
                        timePattern = new Regex(@"\nTime:\s*([0-9:]+ (?:AM|PM))");

                        PassResultRepresentation = "Passed";

                        rawAttributes.Add(new RawFileAttribute("ResultForm", "Pattern", null));

                        break;

                    case ImportMode.JADE:
                        string regexResultRows = @"^(\d{2}[:]\d{2}[:]\d{2})\s+[[]SYSTEM[\]]\s+Board Processed[:]\sProgram=([_a-zA-Z-0-9. ]+)\s-\sT=(\d+)deg C - Pump=(\d+)rpm - Time=(\d+[.]\d)secs -.+Count=(\d+)$";
                        List<string> resultRows = Regex.Matches(logFileContent, regexResultRows).Cast<Match>().Select(m => m.Value).ToList();
                        resultRows = resultRows.Skip(Math.Max(0, resultRows.Count() - 1)).ToList();

                        logFileContent = resultRows[0];

                        serialNumberPattern = new Regex(@"Count=(\d+)");
                        programNamePattern = new Regex(@"Program=([_a-zA-Z-0-9. ]+)");

                        CustomResource = "PEWS2779";
                        CustomStatus = "PASS";
                        CustomDateTime = DateTime.Now;

                        break;

                    case ImportMode.ERSA:

                        break;

                    case ImportMode.NGDTAKAYA:
                        break;

                    case ImportMode.XML_EXCLUSIVE:
                        break;

                    case ImportMode.TRIICT:
                        serialNumberPattern = new Regex(@"\nPANEL SERIAL NUMBER.+(?:PASSED|FAILED) [-] (\d{2}[:/]\d{2}[:/]\d{4} \d{2}[:/]\d{2}[:/]\d{2})");
                        ResultPattern = new Regex(@"\nPANEL SERIAL NUMBER.+(PASSED|FAILED)");
                        programNamePattern = new Regex(@"\nBoard: ([A-z0-9-_]+)\n");


                        break;

                    case ImportMode.TRIICT2:
                        serialNumberPattern = new Regex(@"\nPANEL SERIAL NUMBER.+(?:PASSED|FAILED) [-] (\d{2}[:/]\d{2}[:/]\d{4} \d{2}[:/]\d{2}[:/]\d{2})");
                        ResultPattern = new Regex(@"\nPANEL SERIAL NUMBER.+(PASSED|FAILED)");
                        programNamePattern = new Regex(@"\nBoard: ([A-z0-9-_]+)\n");


                        break;

                    case ImportMode.TRACEFILE:
                        break;
                }

                if (CSVMode)
                {
                    if (Records.Count > 0)
                    {
                        foreach (Dictionary<string, string> record in Records)
                        {
                            ProductResult product = new ProductResult
                            {
                                SerialNb = record["SerialNb"],
                                Status = record["Result"].ToLower().Contains(PassResultRepresentation.ToLower()) ? "PASS" : "FAIL",
                                ResourceName = record["Resource"],
                                Program = CustomProgram ?? record["Program"],
                                //ResultForm = resultForm ?? record["Form"],
                                Timedate = GetDateTime(record["DateTime"])
                            };

                            //Add product to the collection
                            ProductResultCollection.Add(product);
                        }
                    }
                    else { throw new ArgumentException("No data is available in csv mode to complete ProductResultCollection."); }
                }
                else
                {
                    List<List<string>> Results = RegexLibrary.GetAllMatchesWithGroups(serialNumberPattern, logFileContent);
                    ////Remove this part if there isn't several sn if vareol logfiles---------
                    if (Results.Count() > 1 && AppSettings.ImportMode == ImportMode.VAREOL)
                    {
                        MessageManager.InitializeMessage(AppSteps.Running, "There are two SN in the file", SeverityEnum.Warning);
                    }
                    ////-------------
                    foreach (List<string> result in Results)
                    {
                        ProductResult product = new ProductResult
                        {
                            SerialNb = result[1],//RegexLibrary.GetFirstGroup(serialNumberPattern, logFileContent),
                            Status = CustomStatus ?? ((RegexLibrary.GetFirstGroup(ResultPattern, logFileContent).ToLower().Contains(PassResultRepresentation.ToLower())) ? "PASS" : "FAIL"),
                            ResourceName = CustomResource ?? RegexLibrary.GetFirstGroup(ResourcePattern, logFileContent),
                            Program = CustomProgram ?? RegexLibrary.GetFirstGroup(programNamePattern, logFileContent),
                            //ResultForm = resultForm ?? RegexLibrary.GetFirstGroup(ResultformPattern, logFileContent),
                            Timedate = CustomDateTime ?? GetDateTime(RegexLibrary.GetFirstGroup(datePattern, logFileContent) + RegexLibrary.GetFirstGroup(timePattern, logFileContent))
                        };

                        //Add product to the collection
                        ProductResultCollection.Add(product);
                    }
                }

                foreach (RawFileAttribute rawAttr in rawAttributes)
                {
                    string attrName, attrValue;

                    attrName = rawAttr.AttributeName;
                    if(rawAttr.Regex != null)
                    {
                        attrValue = RegexLibrary.GetFirstMatch(rawAttr.Regex, rawAttr.Data);
                    }
                    else
                    {
                        attrValue = rawAttr.Data;
                    }

                    FileAttributesCollection.Add(new FileAttribute(attrName, attrValue));
                }
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Running, "ReturnProductResultsFromLog : " + exception.Message, SeverityEnum.Warning);
            }
        }

        /// <summary>
        /// Method to convert a string (formatted in csv) to a dictionnary list
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="csvContent"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        private static List<Dictionary<string, string>> StringToCSV(string[] headers, string csvContent, string delimiter)
        {
            List<Dictionary<string, string>> Records = new List<Dictionary<string, string>>();

            using (StringReader stringReader = new StringReader(csvContent))
            using (TextFieldParser parser = new TextFieldParser(stringReader))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiter);

                //string[] headers = parser.ReadFields();

                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    Dictionary<string, string> Record = new Dictionary<string, string>();

                    // Initialize Record with default values for missing headers
                    foreach (string header in StandardCSVHeader)
                    {
                        Record[header] = "";
                    }

                    // Assign field values to corresponding headers
                    for (int i = 0; i < Math.Min(headers.Length, fields.Length); i++)
                    {
                        Record[headers[i]] = fields[i];
                    }

                    Records.Add(Record);
                }
            }

            return Records;
        }

        /// <summary>
        /// Methods to convert the DateTime to be able to be fed to the MicrosoftSQL
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns>ParsedDateTime or DateTime.MinValue if the param is empty</returns>
        /// <exception cref="ArgumentException"></exception>
        private static DateTime GetDateTime(string datetime)
        {
            if (datetime != "")
            {
                string[] supportedFormats = new string[] {
                    "dddd, MMMM d, yyyyh:m:s tt", // Format with AM/PM
                    "dddd, MMMM d, yyyyH:m:s",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyyMMddHHmmss"
                };

                DateTime parsedDateTime;

                foreach (string format in supportedFormats)
                {
                    if (DateTime.TryParseExact(datetime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
                    {
                        return parsedDateTime;
                    }
                }

                throw new ArgumentException("GetDateTime : Date/time format not supported.");
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Method for complete serialnb pattern if there are missing
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private static List<Dictionary<string, string>> CompletePatternId(List<Dictionary<string, string>> Records)
        {
            if (Records.Any(record => record["Form"].ToLower() == "pattern" && string.IsNullOrEmpty(record["SerialNb"])))
            {
                var panelRecord = Records.FirstOrDefault(record => record["Form"].ToLower() == "panel");

                Dictionary<string, object> Reference = new Dictionary<string, object>();
                Reference.Add("@Level", "ParentID");
                Reference.Add("@ID", panelRecord["SerialNb"]);

                //List<Dictionary<string, object>> Family = DatabaseManager.ExecuteStoredProcedure(SQLProcedures.GetTracePartionning, Reference);//TBC
                try
                {
                    // Update the "Pattern" form records with family serial numbers
                    foreach (var member in DatabaseManager.ExecuteStoredProcedure(SQLProcedures.GetTracePartionning, Globals.SQLLogins["DatapumpDB"], Reference))
                    {
                        string sequenceID = member["SequenceID"].ToString();
                        string childID = member["ChildID"].ToString();

                        // Update corresponding records in Records
                        foreach (var record in Records.Where(record => record["Form"].ToLower() == "pattern" && record["Index"] == sequenceID))
                        {
                            record["SerialNb"] = childID;
                        }
                    }
                }
                catch (Exception exception)
                {
                    MessageManager.InitializeMessage(AppSteps.Running, exception.Message,SeverityEnum.Debug);
                    Records.RemoveAll(record => record["Form"].ToLower() == "pattern");
                }
            }
            else
            {
                Records.RemoveAll(record => record["Form"].ToLower() == "panel" && string.IsNullOrEmpty(record["SerialNb"]));
            }

            return Records;
        }
    }
}