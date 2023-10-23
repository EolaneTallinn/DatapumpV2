using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace DataPumpV2.Classes
{
    public class ConfigurationManager
    {
        public class YamlCollection
        {
            [YamlMember(Alias = "ComputerCollection")]
            public Dictionary<string, ComputerInfo> ComputerCollection { get; set; }

            public class ComputerInfo
            {
                [YamlMember(Alias = "OperationName")]
                public string OperationName { get; set; }

                [YamlMember(Alias = "ProcessDataDir")]
                public string ProcessDataDir { get; set; }

                [YamlMember(Alias = "SubDirFilter")]
                public string SubDirFilter { get; set; }

                [YamlMember(Alias = "LogNameFilter")]
                public string LogNameFilter { get; set; }

                [YamlMember(Alias = "DeepResearch")]
                public string DeepResearch { get; set; }

                [YamlMember(Alias = "AppDebug")]
                public string AppDebug { get; set; }

                [YamlMember(Alias = "AllowedExtensions")]
                public string AllowedExtensions { get; set; }

                [YamlMember(Alias = "ImportMode")]
                public string ImportMode { get; set; }
            }
        }

        private static string yamlContent;

        /// <summary>
        /// Method to store parameters in app variables from the yaml on the server
        /// </summary>
        public static void LoadConfiguration()
        {
            AppSettings.CurrentVersion = FileManager.GetFileContent(Globals.VersionPath);

            //Deserialize the YAML file
            try
            {
                IDeserializer deserializer = new DeserializerBuilder().Build();
                yamlContent = System.Text.Encoding.UTF8.GetString(FTPManager.Download("/3.ScriptVault/DATAPUMP.YAML"));
                YamlCollection yamlResult = deserializer.Deserialize<YamlCollection>(yamlContent);

                // Convert all dic keys to uppercase
                yamlResult.ComputerCollection = yamlResult.ComputerCollection.ToDictionary(kv => kv.Key.ToUpper(), kv => kv.Value);

                //Populate AppSettings based on the YAML data
                if (yamlResult.ComputerCollection.TryGetValue(Globals.computerName, out var computerSettings))
                {
                    AppSettings.OperationName = CheckString("OperationName", computerSettings.OperationName);
                    AppSettings.ProcessDataDir = CheckString("ProcessDataDir", computerSettings.ProcessDataDir);
                    AppSettings.SubDirFilter = (computerSettings.SubDirFilter?.Split('|') ?? new string[0]).ToList();
                    AppSettings.LogNameFilter = computerSettings.LogNameFilter;//it can be empty
                    AppSettings.DeepResearch = Convert.ToBoolean(CheckString("DeepResearch", computerSettings.DeepResearch));
                    AppSettings.AppDebug = Convert.ToBoolean(CheckString("AppLog", computerSettings.AppDebug));
                    AppSettings.AllowedExtensions = CheckString("AllowedExtensions", computerSettings.AllowedExtensions).Split('|').ToList();
                    AppSettings.ImportMode = (ImportMode)Enum.Parse(typeof(ImportMode), computerSettings.ImportMode, true);

                    PrintConfiguration();
                }
                else
                {
                    throw new Exception("Could not find an applicable configuration for your computer -> " + Globals.computerName);
                }
            }
            catch (Exception exception)
            {
                MessageManager.InitializeMessage(AppSteps.Configuration, "LoadConfiguration : " + exception.Message, SeverityEnum.Error);
            }
        }

        /// <summary>
        /// Method to check if we don't attribute null or empty to app setting vars
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static string CheckString(string PropertyName, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{PropertyName} is null or empty.");
            }

            if (PropertyName == "ProcessDataDir" && !System.IO.Directory.Exists(value))
            {
                throw new ArgumentException($"{value} is not a valid path.");
            }

            return value;
        }

        /// <summary>
        /// Method to print on the console the app settings var in debug mode
        /// </summary>
        private static void PrintConfiguration()
        {
            MessageManager.InitializeMessage(AppSteps.Configuration, "Current Hash Version         : " + AppSettings.CurrentVersion, SeverityEnum.Debug);
            MessageManager.InitializeMessage(AppSteps.Configuration, "Operation Name               : " + AppSettings.OperationName, SeverityEnum.Debug);
            MessageManager.InitializeMessage(AppSteps.Configuration, "Process Data Directory       : " + AppSettings.ProcessDataDir, SeverityEnum.Debug);
            MessageManager.InitializeMessage(AppSteps.Configuration, "SubDirectory Filter          : " + string.Join(", ", AppSettings.SubDirFilter), SeverityEnum.Debug);
            MessageManager.InitializeMessage(AppSteps.Configuration, "DeepResearch                 : " + AppSettings.DeepResearch.ToString(), SeverityEnum.Debug);
            MessageManager.InitializeMessage(AppSteps.Configuration, "Allowed Extensions           : " + string.Join(", ", AppSettings.AllowedExtensions), SeverityEnum.Debug);
            MessageManager.InitializeMessage(AppSteps.Configuration, "Import Mode                  : " + AppSettings.ImportMode, SeverityEnum.Debug);
        }

        /// <summary>
        /// Method to check if 
        /// </summary>
        internal static void CheckConfigurationChange()
        {
            //string NewVersion = FileManager.GetFileContent(Globals.hashVersionPath);
            //string NewYamlContent = FileManager.GetFileContent(Globals.configurationPath);

            try
            {
                string NewVersion = System.Text.Encoding.UTF8.GetString(FTPManager.Download("/3.ScriptVault/" + Path.GetFileName(Globals.VersionPath)));
                string NewYamlContent = System.Text.Encoding.UTF8.GetString(FTPManager.Download("/3.ScriptVault/" + Path.GetFileName(Globals.configurationPath)));

                if (AppSettings.CurrentVersion != NewVersion)
                {
                    MessageManager.InitializeMessage(AppSteps.Restart, "App Update has been detected, application will restart to apply it.", SeverityEnum.Warning);
                    Program.UpdateApplication();
                }

                if (yamlContent != NewYamlContent)
                {
                    MessageManager.InitializeMessage(AppSteps.Restart, "Yamlfile has been modified, configuration will be reloaded.", SeverityEnum.Warning);
                    LoadConfiguration();
                }
            }
            catch (Exception exception)
            {
                if (exception.Message.ToLower().Contains(".str"))
                {
                    MessageManager.InitializeMessage(AppSteps.Restart, "Impossible to download version file", SeverityEnum.Warning);
                }
                else if (exception.Message.ToLower().Contains(".yaml"))
                {
                    MessageManager.InitializeMessage(AppSteps.Restart, "Impossible to download yaml file", SeverityEnum.Error);
                }
                else
                {
                    MessageManager.InitializeMessage(AppSteps.Restart, exception.Message, SeverityEnum.Error);
                }
            }
        }
    }
}
