using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using console;
using Keboola.StorageAPI;
using System.IO;

namespace consoleTest
{
    [TestFixture]
    class WriteTableCommandTest : SapiTemplate
    {
        Random rnd = new Random();


        /// <summary>
        /// prepares random data to upload
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        string[] PrepareDataToUpdate(int lines)
        {
            List<string> result = new List<string>();
            result.Add("Id,fullname");
            for (int i = 0; i < lines; i++ )
            {
                int value = (i + 100);
                result.Add(value.ToString() + "," + value.ToString());
            }
            return result.ToArray();
        }


        [TestFixtureSetUp]
        public void CreateTable()
        {
            StorageApiClient client = new StorageApiClient(testToken, testRunId);
            client.CreateTableFromCsvAsync(testBucketId, testToken, testTableName, new FileInfo(testFilename), "");        
        }

        [TestFixtureTearDown]
        public void DestroySapiData()
        {
            StorageApiClient client = new StorageApiClient(testToken, testRunId);
            client.DropTable(testTableId);          
            client.DropBucket(testBucketId);
            FileInfo testFileInfo = new FileInfo(testFilename);
            File.Delete(testFileInfo.FullName);        
        }



        [Test]
        public void UpdateExistingTableTest()
        {
            string updateFileName = "filetoupdate.csv";
            string[] args = new string[] { "write-table",  testTableId,  updateFileName, "-t", testToken };        
            
            //creates test file
            FileInfo updateFileInfo = new FileInfo(updateFileName);
            string[] dataToUpdate = PrepareDataToUpdate(100);
            System.IO.File.WriteAllLines(updateFileInfo.FullName, dataToUpdate);

            var command = CmdParser.ParseArguments(args);

            Controller ctrl = new Controller(command);
            bool result = ctrl.Execute();

            Assert.That(result, Is.True);

            updateFileInfo.Delete();

        }



        [Test]
        [TestCase("in.c-testBucket.hopefullynonexistingtable")]
       // [TestCase("blablabla")]
        public void UpdateNonExistingTableTest(string nonExistingTableId)
        {

            string updateFileName = "filetoupdate" + DateTime.Now.Ticks + ".csv";
            string[] args = new string[] { "write-table",  nonExistingTableId,  updateFileName, "-t", testToken };

            //creates test file
            FileInfo updateFileInfo = new FileInfo(updateFileName);
            string[] dataToUpdate = PrepareDataToUpdate(100);
            System.IO.File.WriteAllLines(updateFileInfo.FullName, dataToUpdate);

            var command = CmdParser.ParseArguments(args);

            Controller ctrl = new Controller(command);
            bool result = ctrl.Execute();

            Assert.That(result, Is.False);

            updateFileInfo.Delete();

        }

        





       
       
        


    }
}
