using System;
using System.IO;
using System.Net;

namespace DataPumpV2.Classes
{
    internal static class FTPManager
    {
        private static readonly NetworkCredential networkCredential = new NetworkCredential(Globals.ftpUsername, Globals.ftpPassword);

        /// <summary>
        /// Method to download content from ftpserver
        /// </summary>
        /// <param name="LocationUrl">URL of the file</param>
        /// <returns>bytes array</returns>
        /// <exception cref="Exception"></exception>
        internal static byte[] Download(string LocationUrl)
        {
            try
            {
                LocationUrl = LocationUrl.Replace('\\', '/');
                using (WebClient client = new WebClient())
                {
                    client.Credentials = networkCredential;
                    return client.DownloadData(Globals.ftpPath + LocationUrl);
                }
            }
            catch (WebException exception)
            {
                throw new WebException("Download at " + LocationUrl + " : " + exception.Message);
            }
        }

        /// <summary>
        /// Method to upload file to the ftpserver
        /// </summary>
        /// <param name="FtpDestinationDir"></param>
        /// <param name="FilePath"></param>
        internal static void Upload(string FtpDestinationDir, string FilePath)
        {
            string currentDir = Globals.ftpPath;
            string DirRename = FtpDestinationDir.Replace('\\', '/');

            try
            {
                string[] subDirs = DirRename.Split('/');
                
                try
                {
                    foreach (string subDir in subDirs)
                    {
                        currentDir = currentDir + "/" + subDir;

                        WebRequest request = WebRequest.Create(currentDir);
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;
                        request.Credentials = networkCredential;

                        using (var resp = (FtpWebResponse)request.GetResponse()) { }
                    }
                }
                catch
                {
                    throw new WebException("Impossible to create the directory on the server, maybe the name is empty");
                }

                using (var client = new WebClient())
                {
                    client.Credentials = networkCredential;
                    client.UploadFile(currentDir + Path.GetFileName(FilePath), FilePath);
                }
            }
            catch (WebException exception)
            {
                throw new WebException("Upload to " + currentDir + " : " + exception.Message);
            }
        }
    }
}
