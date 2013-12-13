using console;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace consoleTest
{
    [TestFixture]
    public class CmdParserTest
    {

        private string[][] InvalidArgs =
        {
          new string[]{""},
          new string[]{ "sdfsdf" },
          new string[]{ "upload" },
          new string[]{ "upload sdf sdf sdf" },
          new string[]{ "-v" },
          new string[]{ "upload", "-v" }
        };


        [Test]
        [TestCaseSource("InvalidArgs")]
        public void ParseArgsShouldFail(string[] args)
        {
            //string[] args = new string[0];
            var  command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            bool result = ctrl.CheckCommandLineArguments();
            Assert.That(result, Is.False);
        }


        private string[][] QuietArgs =
        {
          
          new string[] { "create-table", "asdasd", "-t", "adasd", "SomeName",  "sdf", "-q", "-v" },         
          new string[] { "create-table", "asdasd", "-t", "adasd", "SomeName",  "sdf", "--quiet", "--verbose" },
         //NOT ALLOWD: new string[] { "-q", "create-table", "-f", "asdasd", "-t", "adasd", "-nSomeName", "-b", "sdf" },         
        };


        [Test]
        [TestCaseSource("QuietArgs")]
        public void ParseQuietArgTest(string[] args)
        {
            //string[] args = new string[] { "-q", "-v" };
            //string[] args = new string[] { "create-table", "-f", "asdasd", "-t", "adasd", "-nSomeName", "-b", "sdf", "-q", "-v" };
            //string [] args = new string[]{ };
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);

            Assert.That(ctrl._currentLogLevel, Is.EqualTo(LogLevel.nolog));
            //Assert.That(CmdParser.GlobalOpations.Verbose, Is.True);        

        }


        private string[][] VerboseArgs =
        {
          
          new string[] { "create-table", "asdasd", "-t", "adasd", "SomeName", "sdf",  "-v" },         
          new string[] { "create-table","asdasd", "-t", "adasd", "SomeName", "sdf",  "--verbose" },
          new string[] { "create-table", "asdasd", "-t", "adasd","-v", "SomeName",  "sdf"},         
         //NOT ALLOWD: new string[] { "-q", "create-table", "-f", "asdasd", "-t", "adasd", "-nSomeName", "-b", "sdf" },         
        };


        [Test]
        [TestCaseSource("VerboseArgs")]
        public void ParseVerboseArgTest(string []args)
        {
            //string[] args = new string[] { "-q", "-v" };
            ///string[] args = new string[] { "create-table", "-f", "asdasd", "-t", "adasd", "-nSomeName", "-b", "sdf", "-v" };
            //string [] args = new string[]{ };
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);

            Assert.That(ctrl._currentLogLevel, Is.EqualTo(LogLevel.debug));
            //Assert.That(CmdParser.GlobalOpations.Verbose, Is.True);        

        }


        //************************************************************
        //****************************WRITE TABLE PARSER TESTS

        Tuple<string[], WriteTableCommand>[] WriteTableValidArgs =
        {
            new Tuple<string[],WriteTableCommand>( 

                 new string[]{"write-table","destinationid", "filepath","-t","token"},

                new WriteTableCommand{
                     tableId ="destinationid",
                     filePath="filepath",
                     Incremental= false,
                      Quiet= false,
                      Verbose=false,
                       Token="token"                   
                }
            ),

           new Tuple<string[],WriteTableCommand>( 

                 new string[]{"write-table","destinationid","filepath","-t","token", "-i","-v"},

                new WriteTableCommand{
                     tableId ="destinationid",
                     filePath="filepath",
                     Incremental= true,
                      Quiet= false,
                      Verbose=true,
                       Token="token"                   
                }
            ),

      new Tuple<string[],WriteTableCommand>( 

                 new string[]{"write-table","destinationid", "filepath","--token","token","-v","-q","--incremental"},

                new WriteTableCommand{
                     tableId ="destinationid",
                     filePath="filepath",
                     Incremental= true,
                      Quiet= true,
                      Verbose=true,
                       Token="token"                   
                }
            ), 
        
        };

        [Test]
        [TestCaseSource("WriteTableValidArgs")]
        public void WriteTableParseArgsOK(Tuple<string[], WriteTableCommand> args)
        {
            WriteTableCommand command = (WriteTableCommand)CmdParser.ParseArguments(args.Item1);
            WriteTableCommand shouldbe = (WriteTableCommand)args.Item2;
            // Assert.That(command, Is.InstanceOf(typeof(WriteTableCommand)));              
            //   Assert.AreEqual(command, shouldbe);

            command.ShouldHave().AllProperties().EqualTo(shouldbe);
        }



        private string[][] WriteTableInvalidArgs =
        {            
            new string[]{ "create-table", "asdasd", "-t","adasd"},          
            new string[]{ "create-table", "asdasd", "-t","adasd","-i" },                    
            new string[]{ "create-table",  "asdasd", "adasd" },          
            
   //       new string[]{ "upload", "asdasd", "-t","adasd", "-s", "adsasd","-d","asdasd" },                    
        };


        [Test]
        [TestCaseSource("WriteTableInvalidArgs")]
        public void WriteTableParseArgsTestShouldFail(string[] args)
        {
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            bool result = ctrl.CheckCommandLineArguments();
            Assert.That(result, Is.False);
        }



        //***********************************************************
        //*****************PARSE CREATE TABLE******************************
        //***********************************************************

        private string[][] CreateTableArgs =
        {          
          new string[]{ "create-table", "c:\\sdf\\sdf", "-t","adasd", "main", "testtable" },          
          new string[]{ "create-table", "asdasd", "-t","adasd", "SomeName", "ffds" },          
          new string[]{ "create-table", "asdasd", "adasd",  "adsasd", "--token=sdfasd" },          
          
        };


        [Test]
        [TestCaseSource("CreateTableArgs")]
        public void ParseCreateTableArgumentsTest(string[] args)
        {
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            bool result = ctrl.CheckCommandLineArguments();

            Assert.That(result, Is.True);
            Assert.That(command, Is.InstanceOf(typeof(CreateTableCommand)));
        }


        private string[][] CreateTableInvalidArgs =
        {            
            new string[]{ "create-table", "asdasd", "-t", "adasd", "main"},          
            new string[]{ "create-table", "asdasd", "adsasd", "--primary-key=asdasdasd" , "-t","adasd",},          
            new string[]{ "create-table", "asdasd", "adsasd", "-t","adasd" },          
            new string[]{ "create-table", "asdasd", "adsasd", "-t","adasd", "sdfsdf", "sdfsdf" },  
            new string[]{ "create-table", "asdasd", "adasd",  "adsasd" },          
            new string[]{ "create-table", "asdasd", "adasd",  "adsasd", "sdfsdf" },          
            
           new string[]{ "upload", "asdasd", "-t","adasd", "-s", "adsasd","-d","asdasd" },                    
        };



        [Test]
        [TestCaseSource("CreateTableInvalidArgs")]
        public void ParseCreateTableArgumentsTestShouldFail(string[] args)
        {
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            bool result = ctrl.CheckCommandLineArguments();
            Assert.That(result, Is.False);
        }




    }
}
