using Keboola.StorageAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sampleProject
{
    class Program
    {
        static string token = "wert"; //ADD YOUR TEST TOKEN HERE!!!
        static string testTableName = "SomeTable";
        static string testBucketId = "sys.c-TestingBucket";
        static string testBucketName = "TestingBucket";
        static string testBucketStage = "sys";
        static string testTableId = testBucketId + "." + testTableName;
        static string emptyTableName = "EmptyTable";

        static void Main(string[] args)
        {
            StorageApiClient client = new StorageApiClient(token);
             
            var result = client.TableExists(testBucketId, emptyTableName);
        }
    }
}
