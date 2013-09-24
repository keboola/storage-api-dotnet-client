using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using Keboola.StorageAPI;

using System.IO;
using System.Runtime.Serialization;
using System.Collections.Specialized;
using System.Net.Http;
using Keboola.StorageAPI.DataStructures;


namespace Keboola.StorageApiTest
{

    [TestFixture]
    public class StorageApiClientTest
    {
        static string token = LoadValueFromConfigFile("token");
        static string testTableName = "SomeTable";
        static string testBucketId = "sys.c-TestingBucket";
        static string testBucketName = "TestingBucket";
        static string testBucketStage = "sys";
        static string testTableId = testBucketId + "." + testTableName;
        static string testRunId =  "MS-SAPIClientTest";


        static string testFilename = "test.csv";
        static string[] testFileData = { "Id,fullname", "1,somename", "2,someothername" };


        [TestFixtureSetUp]
        public void SetupUp()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            client.CheckBucket(testBucketName, testBucketStage, "MSRCM-Extractor:testing storage api client CheckBucketTest");
            if (client.TableExists(testBucketId, testTableName) != null)
                client.DropTable(testTableId);

            //creates test file
            FileInfo testFileInfo = new FileInfo(testFilename);
            System.IO.File.WriteAllLines(testFileInfo.FullName, testFileData);
            client.CreateTableFromCsvAsync(testBucketId, token, testTableName, testFileInfo);
        }

        [TestFixtureTearDown]
        public void DestroySetup()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            client.DropTable(testTableId);
            client.DropBucket(testBucketId);
            FileInfo testFileInfo = new FileInfo(testFilename);
            File.Delete(testFileInfo.FullName);
        }


        public static string LoadValueFromConfigFile(string key)
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;

            if (appSettings.AllKeys.Contains(key) == false)
                return null;

            return appSettings[key];

        }

        public static string ToUnixTimestamp(DateTime target)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, target.Kind);
            var unixTimestamp = System.Convert.ToInt64((target - date).TotalSeconds);

            return unixTimestamp.ToString();
        }
        //#########################################TESTS#################################################

        




        [Test]
        public void SAPICreateAndUpdateTableAsyncTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            string tablename = "asynctable";
            string filename = "asyncCreateTest"+ ToUnixTimestamp(DateTime.Now)+ ".csv";

            if (client.TableExists(testBucketId, tablename) != null)
                client.DropTable(testBucketId + "." + tablename);
            

            //creates test data to be written into the testfile
            var data = new StringBuilder("column1,column2,column3\n");
            foreach (var i in Enumerable.Range(0, 100))
                data.AppendFormat("{0},{1},{2}\n", i, i, i);

            //creates test file
            FileInfo testFileInfo = new FileInfo(filename);
            System.IO.File.WriteAllText(testFileInfo.FullName, data.ToString());

            var gzipfname = CSV.Controller.Compress(testFileInfo);


            client.CreateTableFromCsvAsync(testBucketId, token, tablename, new FileInfo(gzipfname), "");
            File.Delete(testFileInfo.FullName);
            File.Delete(gzipfname);


            //UPDATE TABLE ASYNC
            filename = "asyncUpdateTest" + ToUnixTimestamp(DateTime.Now) + ".csv";
            //creates test data to be written into the testfile
            data = new StringBuilder("column1,column2,column3\n");
            foreach (var i in Enumerable.Range(0, 100))
                data.AppendFormat("c,a,b\n");

            //creates test file
            testFileInfo = new FileInfo(filename);
            System.IO.File.WriteAllText(testFileInfo.FullName, data.ToString());
            gzipfname = CSV.Controller.Compress(testFileInfo);
            client.UpdateTableAsync(token, testBucketId + "." + tablename, new FileInfo(gzipfname));


            client.DropTable(testBucketId + "." + tablename);
            File.Delete(testFileInfo.FullName);
            File.Delete(gzipfname);
        }


          [Test]
        public void SAPIUploadFileTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
               string filename = "uploadFileTest"+ ToUnixTimestamp(DateTime.Now);
               //creates test data to be written into the testfile
               var data = new StringBuilder("column1,column2,column3\n");
               foreach (var i in Enumerable.Range(0, 10000))
                   data.AppendFormat("{0},{1},{2}\n", i, i, i);

               //creates test file
               FileInfo testFileInfo = new FileInfo(filename);
               System.IO.File.WriteAllText(testFileInfo.FullName, data.ToString());

               var id = client.UploadFile(filename, false, false);
               Assert.That(id, Is.Not.Null);
               Assert.That(id, Is.InstanceOf(typeof(string)));
        
        }




        [Test]
        public void SAPIUpdateTableAsyncTest()
        {


        }

        [Test]
        public void SAPITableExistsTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            var result = client.TableExists(testBucketId, testTableName);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(typeof(string)));
            Assert.That(result, Is.EqualTo(testTableId));
        }

      


        [Test]
        public void SAPICreateAndDropBucketTest()
        {
            string newBucketName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string newBucketId = "sys.c-" + newBucketName;
            StorageApiClient client = new StorageApiClient(token, testRunId);
            var bucket = client.CreatBucket(token, newBucketName, "sys", "created from SAPICreateAndDropBucketTest test");
            Assert.That(bucket, Is.Not.Null);
            Assert.That(bucket.Id, Is.EqualTo(newBucketId));
            var result = client.DropBucket(bucket.Id);
            Assert.That(result, Is.True);
        }



        [Test]
        public void SAPIDropBucketTest_ShouldFailIfNotEmpty()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            //should fail because the bucket is not empty
            var result = client.DropBucket(testBucketId);
            Assert.That(result, Is.False);
        }


        [Test]
        public void SAPICaseSensitiveTableExistsTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            //testTableName
            string table = client.TableExists(testBucketId, testTableName.ToUpper());
            Assert.That(table, Is.Not.Null);
        }


        [Test]
        public void SAPICaseSensitiveCreateAndCheckBucketTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);

            string caseBucketName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            //createbucket
            Bucket bucket = client.CreatBucket(token, caseBucketName + "CASE", "sys", "MSCRM Extractor test SAPICaseSensitiveTestCreateBucket");
            Assert.That(bucket, Is.Not.Null);
            Assert.That(bucket.Stage, Is.EqualTo("sys"));
            Assert.That(bucket.Name, Is.EqualTo("c-" + caseBucketName + "CASE"));
            //checkbucket
            bool check = client.CheckBucket(caseBucketName + "case", "sys", "MSCRM Extractor test SAPICaseSensitiveTestCreateBucket");
            Assert.That(check, Is.False); //returns false if bucket already exists

            //drop bucket
            bool isdrop = client.DropBucket(bucket.Id);
            Assert.That(isdrop, Is.True);
        }





        /// <summary>
        /// Creates a table then updates the table and then drop the table
        /// tablename is stored in tablename
        /// </summary>
        [Test]
        public void SAPICompressCreateUpdateDropTableFromCsvTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            string tablename = "CSVTestTable" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            string bucketid = testBucketId;
            string tableId = bucketid + "." + tablename;
            string csvNewFileName = CreateTableTestCase_GenerateCSV(tablename);

            //CREATE TABLE
            FileInfo file1 = new FileInfo(csvNewFileName);
            CSV.Controller.Compress(file1);
            client.CreateTableFromCsvAsync(bucketid, token, tablename, new FileInfo(file1.FullName + ".gz"));
            string createdTableId = client.TableExists(bucketid, tablename);
            Assert.That(createdTableId, Is.Not.Null);
            Assert.That(createdTableId, Is.EqualTo(tableId));
            File.Delete(csvNewFileName);

            //UPDATE TABLE
            string csvUpdateFileName = CreateTableTestCase_GenerateCSV("update" + tablename);
            FileInfo file2 = new FileInfo(csvUpdateFileName);
            CSV.Controller.Compress(file2);
            client.UpdateTableAsync(token, createdTableId, new FileInfo(file2.FullName + ".gz"));
            string updatedTableId = client.TableExists(bucketid, tablename);
            Assert.That(updatedTableId, Is.Not.Null);
            Assert.That(updatedTableId, Is.EqualTo(tableId));
            File.Delete(csvUpdateFileName);

            //DROP TABLE
            var dropResult = client.DropTable(tableId);

            Assert.That(dropResult, Is.True);

            //DELETE FILE 1
            File.Delete(file1.FullName);
            File.Delete(file1.FullName + ".gz");
            //DELETE FILE 2
            File.Delete(file2.FullName);
            File.Delete(file2.FullName + ".gz");
        }

        private static string CreateTableTestCase_GenerateCSV(string filename)
        {
            var rnd = new Random();
            //string filename = "CSVTestTable" + DateTime.Now.ToString("YYY_MM_dd_HH_mm_ss") + ".csv";
            return GenerateTestCSV(filename, CSVTestColumnNames, rnd.Next(1000, 2000));
        }

        private static List<string> CSVTestColumnNames = new List<string> { "Id", "SomeName", "SomeOwner", "SomeShit" };
        private static string GenerateTestCSV(string filename, List<string> columns, int rowsCount)
        {
            CSV.Controller csvGen = new CSV.Controller(filename, false);

            //write header
            csvGen.FlushListInToCSV(columns);

            Random rnd = new Random();
            foreach (var i in Enumerable.Range(0, rowsCount))
            {
                List<string> newLine = new List<string>();
                foreach (var column in columns)
                {
                    newLine.Add(rnd.Next() + column + i.ToString());
                }
                csvGen.FlushListInToCSV(newLine);
            }
            csvGen.Close();
            return csvGen.FullPathName;
        }

        [Test]
        public void SAPIListAllBucketsTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            var result = client.ListAllBuckets();
            CollectionAssert.AllItemsAreNotNull(result);
            CollectionAssert.AllItemsAreInstancesOfType(result, typeof(Bucket));

        }

        [Test]
        public void SAPIVerifyTokenTest()
        {

            StorageApiClient client = new StorageApiClient(token, testRunId);
            var info = client.VerifyToken();

            Assert.That(info, Is.Not.Null);
            Assert.That(info, Is.InstanceOf(typeof(TokenInfo)));
        }


        [DataContract]
        public class SomeTableRow
        {
            [DataMember(Name = "Id")]
            [LINQtoCSV.CsvColumn(Name = "Id")]
            public string Id;

            [DataMember(Name = "fullname")]
            [LINQtoCSV.CsvColumn(Name = "fullname")]
            public string Fullname;
        }


        [Test]
        public void SAPIDeleteAttributeTest()
        {
            Random rnd = new Random();
            string value = "samplevalue" + rnd.Next();
            string key = "testAttribute";
            StorageApiClient client = new StorageApiClient(token, testRunId);
            client.SetTableAttribute(testTableId, key, value);
            var attributes = client.ListTableAttributes(testTableId);
            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.ContainsKey(key), Is.True);
            Assert.That(attributes[key], Is.EqualTo(value));

            client.DeleteTableAttribute(testTableId, key);
            var attributesAfter = client.ListTableAttributes(testTableId);
            Assert.That(attributesAfter, Is.Not.Null);
            Assert.That(attributesAfter.ContainsKey(key), Is.False);
        }

        [Test]
        public void SAPISetAttributeTest()
        {
            Random rnd = new Random();
            string value = "samplevalue" + rnd.Next();
            string key = "testAttribute";
            StorageApiClient client = new StorageApiClient(token, testRunId);
            client.SetTableAttribute(testTableId, key, value);
            var attributes = client.ListTableAttributes(testTableId);
            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.ContainsKey(key), Is.True);
            Assert.That(attributes[key], Is.EqualTo(value));
        }


        [Test]
        public void SAPIGetTableDataTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            string tableid = testTableId;
            // "clientSecret"
            IEnumerable<SomeTableRow> result = client.GetTableData<SomeTableRow>(tableid);

            Assert.That(result, Is.Not.Null);
            CollectionAssert.AllItemsAreNotNull(result);
            CollectionAssert.AllItemsAreInstancesOfType(result, typeof(SomeTableRow));
            Assert.That(result, Is.InstanceOf(typeof(IEnumerable<SomeTableRow>)));

        }


        [Test]
        public void SAPIListTableAttributesTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            string tableid = testTableId;
            // "clientSecret"
            var result = client.ListTableAttributes(tableid);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(typeof(Dictionary<string, string>)));
        }

        [Test]
        public void SAPICheckBucketTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            var result = client.CheckBucket(testBucketName, testBucketStage, "MSRCM-Extractor:testing storage api client CheckBucketTest");
            //if bucket already exists it should return false otherwise it creates a new bucket and return true
            Assert.That(result, Is.False);

        }

        [Test]
        public void SAPITablesInBucketTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            string bucketid = testBucketId;
            var result = client.TablesInBucket(bucketid);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(typeof(List<TableInfo>)));

            //testing case insesitivity
            result = client.TablesInBucket(bucketid.ToUpper());
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(typeof(List<TableInfo>)));
        }

        [Test]
        public void SAPIEmptyBucketTableExistTest()
        {
            StorageApiClient client = new StorageApiClient(token, testRunId);
            Random rnd = new Random();


            //create an empty bucketname and assure that bucket does not exist
            string randomBuckeName = token.Substring(2, 5) + rnd.Next().ToString();
            string stage = "in";
            string randomBucketId = stage + ".c-" + randomBuckeName;


            //call tableexist and check the return count is 0
            var result = client.TablesInBucket(randomBucketId);
            CollectionAssert.IsEmpty(result);




        }

    }
    //[TestFixture]
    //public class ClientTest
    //{
    //    [Test]
    //    public void SimpleCreateClientTest()
    //    {
    //        HttpClient c = new HttpClient();
    //        c.BaseAddress = new Uri("http://connection.keboola.com");
    //        c.DefaultRequestHeaders.Add("X-StorageApi-Token", "f20cb34ada408a99fd2d2d3739f96d60");

    //        MultipartFormDataContent multipartcontent = new MultipartFormDataContent();           
    //        multipartcontent.Add(new StringContent("pokusTomas6",Encoding.UTF8),"name");
    //        multipartcontent.Add(new StringContent("in",Encoding.UTF8),"stage");
    //        multipartcontent.Add(new StringContent("pokus description",Encoding.UTF8),"description");
    //        multipartcontent.Add(new StringContent("f20cb34ada408a99fd2d2d3739f96d60",Encoding.UTF8),"token");
    //        var task =  c.PostAsync("/storage/buckets",multipartcontent);
    //        task.Wait();

    //        HttpResponseMessage response = task.Result;
    //        var task2 = response.Content.ReadAsAsync<Bucket>();

    //        task2.Wait();
    //        Bucket bucket = task2.Result;

    //        Assert.Pass(task2.Result.ToString());           

    //    }
    //}
}
