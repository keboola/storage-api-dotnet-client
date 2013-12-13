using CommandLine;
using Keboola.StorageAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace console
{
   public  class WriteTableCommand : CommonSubOptions, ISapiCommand
    {

        [Option('t', "token", Required = true, HelpText = "Token to Storage API")]
        public string Token { get; set; }


       // [Option('f', "filepath", Required = true, HelpText = "Path to input csv file")]
       [ValueOption(1)]
        public string filePath { get; set; }


        //[Option('d', "dest-tableid", Required = true, HelpText = "destination table id")]
        [ValueOption(0)]
        public string tableId { get; set; }


        [Option('i',"incremental", HelpText = "incremental load")]
        public bool Incremental { get; set; }


        public override string Usage()
        {
            string usage = "write-table ";
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

        public string GetName() { return "write-table"; }

        FileInfo _fileToUpload = null;


        public StorageApiClient PrepareCommand()
        {

            Log.ToConsole("Destination table id:" + tableId, LogLevel.debug);
            Log.ToConsole("Is incremental:" + Incremental, LogLevel.debug);

            //verify source file
            Log.ToConsole("Verifying filepath.." + filePath, LogLevel.debug);
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
                    Log.ToConsole("There was an unknown error while compresing file:"+ file.FullName, LogLevel.error);
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
            Log.ToConsole("Sending data, please wait...");
            client.UpdateTableAsync(Token, tableId, _fileToUpload, Incremental);
            Log.ToConsole("Table updated successfully.");
            return true;        
        }
        

    }
}
