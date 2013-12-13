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
   public  class SapiTemplate
    {
        public static string LoadValueFromConfigFile(string key)
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;

            if (appSettings.AllKeys.Contains(key) == false)
                return null;

            return appSettings[key];
        }

       

        public static string testToken = LoadValueFromConfigFile("token");
        public static string testBucketName = "testBucket";
        public static string testBucketStage = "in";
        public static string testBucketId = testBucketStage + ".c-" + testBucketName;
        public static string testTableName = "testTable";
        public static string testRunId = "SAPI console create table test";
        public static string testFilename = "testFile.csv";
        public static string testTableId = testBucketId + "." + testTableName;
        public static string[] testFileData = { "Id,fullname", "1,somename", "2,someothername" };


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
    }
}
