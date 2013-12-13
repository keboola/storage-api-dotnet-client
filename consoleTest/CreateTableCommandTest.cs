using console;
using Keboola.StorageAPI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace consoleTest
{
    [TestFixture]
    class CreateTableCommandTest
    {
        public static string LoadValueFromConfigFile(string key)
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;

            if (appSettings.AllKeys.Contains(key) == false)
                return null;
            return appSettings[key];
        }


        static string   testToken = LoadValueFromConfigFile("token");
        
        static string   testBucketName = "testBucket";
        static string   testBucketStage = "in";
        static string   testBucketId = testBucketStage + ".c-" + testBucketName;
        static string   testTableName = "testTable";
        static string   testRunId = "SAPI console create table test";
        static string   testFilename = "testFile.csv";
        static string   testTableId = testBucketId + "." + testTableName;
        static string[] testFileData = { "Id,fullname", "1,somename", "2,someothername" };


        [TestFixtureSetUp]
        public void SetupUp()
        {
            StorageApiClient client = new StorageApiClient(testToken, testRunId);
            client.CheckBucket(testBucketName, testBucketStage, "MSRCM-Extractor:testing storage api client CheckBucketTest");
            

            //creates test file
            FileInfo testFileInfo = new FileInfo(testFilename);
            System.IO.File.WriteAllLines(testFileInfo.FullName, testFileData);
            
        }

        [TestFixtureTearDown]
        public void DestroySetup()
        {
            StorageApiClient client = new StorageApiClient(testToken, testRunId);
            client.DropTable(testTableId);
            client.DropTable(testBucketId + "." + "existingTableTest");
            client.DropBucket(testBucketId);
            FileInfo testFileInfo = new FileInfo(testFilename);
            File.Delete(testFileInfo.FullName);

        }


        //*********************************************************************************
        //**************************************PREPARE*TESTS**********************        


        [Test]
        public void BadTokenTest()
        {
            string[] args = new string[] { "create-table", "main", "testtable", testFilename, "-t", "adasd" };
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            var result = ctrl.Execute();
            Assert.That(result, Is.False);
        }



        [Test]
        public void BadFileTest()
        {
            string[] args = new string[] { "create-table", "main", "testtable", "sdfsdfsdf" , "-t", testToken };
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            var result = ctrl.Execute();
            Assert.That(result, Is.False);                    
        }


        
      


        [Test]
        public void CreateTableOKTest()
        {
            string[] args = new string[] { "create-table", testBucketId,testTableName, testFilename, "-t", testToken};
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            
            var result = ctrl.Execute();
            Assert.That(result, Is.True);
        }


        [Test]
        public void CreateTableNonExistingBucketTest()
        {
            string[] args = new string[] { "create-table", "hopefullynonexistingbucket",testTableName, testFilename, "-t", testToken };
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);          
            var result = ctrl.Execute();
            Assert.That(result, Is.False);
        }


        [Test]
        public void CreateOnExistingTableTestShouldFail()
        {
            string[] args = new string[] { "create-table", testBucketId, "existingTableTest",testFilename, "-t", testToken};
            var command = CmdParser.ParseArguments(args);
            Controller ctrl = new Controller(command);
            var firstResult = ctrl.Execute();
            var secondResult = ctrl.Execute();             
            Assert.That(secondResult, Is.False);         
        }     





    }
}
