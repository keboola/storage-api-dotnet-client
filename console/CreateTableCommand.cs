using CommandLine;
using Keboola.StorageAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console
{
    public class CreateTableCommand : CommonSubOptions, ISapiCommand
    {


        [Option('t', "token", Required = true, HelpText = "Token to Storage API")]
        public string Token { get; set; }

        //[Option('f', "filePath", Required = true, HelpText = "Path to input csv file")]
        [ValueOption(2)]
        public string filePath { get; set; }


        //[Option('b', "bucketId", Required = true, HelpText = "destination bucket")]
        [ValueOption(0)]
        public string bucketId { get; set; }


        //[Option('n', "name", Required = true, HelpText = "table name")]
        [ValueOption(1)]
        public string name { get; set; }



        [Option("primary-key", DefaultValue = "", HelpText = "Name of the column set as primary key(e.g. id)")]
        public string PrimaryKey { get; set; }

        //[Option('i', "incremental", HelpText = "Specifies that the upload will be incremental.")]
        //public bool IsIncremental { get; set; }

        public override string Usage()
        {
            string usage = "create-table ";
            foreach (var arg in GetArgumentsNames())
                usage += arg + " ";
            usage += " [options]";
            return usage;
        
        }

        LogClass Log = null;

        public void SetLog(LogClass log)
        {
            Log = log;
            if (Verbose)
                Log._currentLogLevel = LogLevel.debug;

            if (Quiet)
                Log._currentLogLevel = LogLevel.nolog;
        }


        FileInfo _fileToUpload = null;

        public string GetName() { return "create-table"; }


        public StorageApiClient PrepareCommand()
        {
            if (PrimaryKey == "")
                Log.ToConsole("Primary key not set", LogLevel.debug);
            else
                Log.ToConsole("Primary key set to:" + PrimaryKey, LogLevel.debug);

            Log.ToConsole("BucketId:" + bucketId, LogLevel.debug);

            Log.ToConsole("Table Name:" + name, LogLevel.debug);



            //verify source file
            Log.ToConsole("Verifying filepath..:" + filePath, LogLevel.debug);
            FileInfo file = new FileInfo(filePath);
            if (file.Exists == false)
            {
                Log.ToConsole("filepath " + filePath + "invalid", LogLevel.error);
                return null;
            }

            Log.ToConsole("compressing file:" + file.FullName, LogLevel.debug);

            string gzipedPath = file.FullName;
            if (file.Extension != ".gz")
            {
                gzipedPath = Keboola.CSV.Controller.Compress(file);
                if (gzipedPath == null)
                {
                    Log.ToConsole("There was an unknown error while compresing file:" + file.FullName, LogLevel.error);
                    return null;
                }
            }
            else
            {
                Log.ToConsole("file already gziped - skipping compression", LogLevel.debug);

            }




            _fileToUpload = new FileInfo(gzipedPath);

            //verify token
            StorageApiClient client = new StorageApiClient(Token);
            try
            {
                Log.ToConsole("verifying token", LogLevel.debug);
                var tokenifno = client.VerifyToken();

                if (tokenifno == null)
                {
                    Log.ToConsole("invalid/unknown token", LogLevel.error);
                    return null;
                }
            }
            catch (Exception ee)
            {
                Log.ToConsole("invalid/unknown token:" + ee.Message, LogLevel.error);
                return null;
            }
            //check destination
            Log.ToConsole("All data prepared", LogLevel.debug);
            return client;
        }



        public bool ExecuteCommand(StorageApiClient client)
        {
            if (client == null)
            {
                Log.ToConsole("Instance of the client is invalid");
                return false;
            }

            Log.ToConsole("Sending data, please wait...");
            client.CreateTableFromCsvAsync(bucketId, Token, name, _fileToUpload, PrimaryKey);
            Log.ToConsole("Table created successfully.");
            //REMOVE GZIPED FILE _fileToUpload
            return true;
        }





    }
}
