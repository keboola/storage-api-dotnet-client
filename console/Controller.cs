using Keboola.StorageAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console
{

    public interface ISapiCommand
    {
        void SetLog(LogClass Log);
        string GetName();
        StorageApiClient PrepareCommand();
        bool ExecuteCommand(StorageApiClient client);
    }

    public enum LogLevel { nolog, info, debug, error, critical };
    public abstract class LogClass
    {
        

        public LogLevel _currentLogLevel = LogLevel.info; //current log level

        public abstract string LogIdentification(); // returns string that identifies the component who made the log

        public void ToConsole(string msg, LogLevel logLevel = LogLevel.info)
        {

            string levestr = logLevel.ToString();
            string msgToLog = levestr+":"+LogIdentification() + ":" + msg;
            switch (_currentLogLevel)
            {
                case LogLevel.nolog:                    
                    break;
                case LogLevel.debug:
                    if (logLevel == LogLevel.info || logLevel == LogLevel.debug)
                        Console.Out.WriteLine(msgToLog);
                    break;
                case LogLevel.info:
                    if (logLevel == LogLevel.info)
                        Console.Out.WriteLine(msgToLog);
                    break;
            }



            if (logLevel == LogLevel.error || logLevel == LogLevel.critical)
                Console.Error.WriteLine(msgToLog);


        }
    }





    public class Controller : LogClass
    {
        ISapiCommand _command;
        public override string  LogIdentification()
        {
            if (_command != null)
                return _command.GetName();
            else
                return "";
        }


        public Controller(ISapiCommand command)
        {


            _command = command;
            if (command !=null)
                _command.SetLog(this);
            else
                ToConsole("Invalid command line arguments", LogLevel.error);            
        }


        public bool CheckCommandLineArguments()
        {
            
            //we either haven't parsed anything at all(_command is null) or unbound arguments are missing so we have to check it
            if (_command == null)
                return false;

            var common = (CommonSubOptions)_command;
            if (common.CheckMissingArguments() == false)
            {
                ToConsole("Some arguments are missing, see " + _command.GetName() + " --help for more info.", LogLevel.error);
                return false;
            }

            return true;
        
        }


        public bool Execute()
        {
            

            try
            {

                ToConsole("Command check started", LogLevel.debug);
                if (CheckCommandLineArguments() == false)
                    return false;

                StorageApiClient client = _command.PrepareCommand();
                ToConsole("Command check finished", LogLevel.debug);

                if (client != null)
                    return _command.ExecuteCommand(client);
                else
                    ToConsole("Could not prepare command", LogLevel.error);
            }

            catch (Exception ee)
            {
                ToConsole(ee.Message, LogLevel.critical);
            }

            return false;

        }


    }
}
