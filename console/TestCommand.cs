using CommandLine;
using Keboola.StorageAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace console
{
    public class TestCommand : CommonSubOptions, ISapiCommand
    {

        //[Option(Required=true)]
        [ValueOption(0)]
        //[Option(HelpText = "first", Required = true)]        
        //[Option(Required = true, HelpText = "first")]
        public string First { get; set; }

       // [Option(Required = true)]
        [ValueOption(1)]
        //[ValueList(string)] 
        //[Option( HelpText = "second", Required=true)]
        public string Second { get; set; }



        [Option('t', "token", Required = true, HelpText = "Token to Storage API")]
        public string Token { get; set; }
       

      //  [Option('d', "dest", Required = true, HelpText = "destination table id")]
      //  public string DestinationTableId { get; set; }


        [Option('i',"incremental", HelpText = "incremental load")]
        public bool Incremental { get; set; }

     

        LogClass Log = null;

        public override string Usage()
        {
            string usage = "test " ;
            foreach(var arg in GetArgumentsNames())
                usage += arg + " ";
            usage += " [options]";
            return usage;

        }


        public void SetLog(LogClass log)
        {
            Log = log;
            if (Verbose)
                Log._currentLogLevel = LogLevel.debug;

            if (Quiet)
                Log._currentLogLevel = LogLevel.nolog;
        }

        public string GetName() { return "test"; }

        public StorageApiClient PrepareCommand()
        {
            Console.WriteLine("First:" + First);
            Console.WriteLine("Second:" + Second);
            Console.WriteLine("Token:" + Token);
         //   Console.WriteLine("DestinationTableId:" + DestinationTableId);
        //    Console.WriteLine("First:" + );
         //   Console.WriteLine("First:" + );
            return null;
        
        }


        public bool ExecuteCommand(StorageApiClient client)
        {
            return true;
        }
    }
}
