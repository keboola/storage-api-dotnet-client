using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keboola.LogHelper;
using Keboola.StorageAPI.DataStructures;
using LINQtoCSV;
using Newtonsoft.Json.Linq;


namespace Keboola.StorageAPI
{
    public class StorageApiClient
    {
        static Logger logger = new Logger(System.Reflection.Assembly.GetExecutingAssembly());
        public static string ServerAddress = "https://connection.keboola.com";
        public static string HeaderTokenName = "X-StorageApi-Token";        
        public static string HeaderRunIdName = "X-KBC-RunId";
        public static string HeaderUserAgentName = "User-Agent";


        private HttpMultipartDataClient _client;
        private string _token = null;
        public string Token { get { return _token; } private set { ;} }

        public StorageApiClient(string token, string runId = "", string userAgent= "sapi-dotnet-client")
        {
            _token = token;
            _client = new HttpMultipartDataClient(ServerAddress,
                new Dictionary<string, string>() { { HeaderTokenName, _token },
                                                   {HeaderRunIdName, runId },
                                                   {HeaderUserAgentName, userAgent}
                                                    }, "/v2");
            
        }       


        /// <summary>
        /// if table exists it returns its id otherwise it returns null
        /// </summary>
        /// <param name="bucketid"></param>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public string TableExists(string bucketid, string tablename)
        {
            logger.Trace("table " + tablename + " exists? in " + bucketid);
            List<TableInfo> tables = TablesInBucket(bucketid);
            return (from table in tables
                    where table.Name.Equals(tablename, StringComparison.InvariantCultureIgnoreCase)
                    select table.Id).FirstOrDefault();
        }


        public TokenInfo VerifyToken()
        {
            logger.Trace("Verifying token");
            string uri = "/storage/tokens/verify";
            TokenInfo result = null;
            result = _client.SendGetRequestToJson<TokenInfo>(uri);
            return result;

        }

        public bool DropBucket(string bucketid)
        {
            string uri = "/storage/buckets/" + bucketid;
            return _client.SendDeleteRequest(uri);

        }

        public Bucket CreatBucket(string token, string name, string stage, string description)
        {
            var postData = new Dictionary<string, string>() { 
                {"token", token},
                {"name", name},           
                {"stage", stage},           
                {"description", description},           
            };
            string uri = "/storage/buckets";
            _client.AddStringFormData(postData);
            return _client.SendPostRequestToJson<Bucket>(uri);
        }


        public void DeleteTableAttribute(string tableid, string attribute)
        {
            logger.Trace("Removing table attribute:" + attribute + " for " + tableid);
            string uri = "/storage/tables/" + tableid + "/attributes/" + attribute;
            _client.SendDeleteRequest(uri);

        }


        Dictionary<string,string> ConvertAttributesToDict(List<AttributeInfo> attrs)
        {
            return attrs
                .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);        
        }

        //
        /// <summary>
        ///https://connection.keboola.com/storage/tables/sys.c-MSCRM.Centrum/attributes/organization 
        /// </summary>
        /// <param name="tableid"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        public void SetTableAttribute(string tableid, string attribute, string value = "")
        {
            logger.Trace("Setting table attribute:" + attribute + " for " + tableid);
            Dictionary<string, string> formData = new Dictionary<string, string>()
            {
                {"value", value}
            };
            string uri = "/storage/tables/" + tableid + "/attributes/" + attribute;

            _client.AddStringFormData(formData);
            var result = ConvertAttributesToDict(_client.SendPostRequestToJson<List<AttributeInfo>>(uri));

            if (result[attribute] != value)
                throw new Exception("attribute was set but returned wrong result");
        }

        /// <summary>
        /// returns attributes of table in Dictionary<string,string>
        /// GET "/storage/tables/" + tableid + "/attributes/" + attributeKey;
        /// </summary>
        /// <param name="tableid">tableid</param>
        /// <param name="attributeKey">atributte if not specified then all atributtes are returned</param>
        /// <returns></returns>
        public Dictionary<string, string> ListTableAttributes(string tableid, string attributeKey = "")
        {
            string uri = "/storage/tables/" + tableid + "/attributes/" + attributeKey;
            var attrs = _client.SendGetRequestToJson<List<AttributeInfo>>(uri);
            return ConvertAttributesToDict(attrs);
                


        }

        public TResult ListTableAttributes<TResult>(string tableid, string attributeKey = "") where TResult : class
        {
            string uri = "/storage/tables/" + tableid + "/attributes" + attributeKey;
            return _client.SendGetRequestToJson<TResult>(uri);
        }


        /// <summary>
        /// it checks if specified bucket exist, if not it creates new bucket and returns true
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="stage"></param>
        /// <param name="description"></param>
        /// <returns>if bucket exists it returns false</returns>
        public bool CheckBucket(string bucketName, string stage, string description)
        {
            var buckets = ListAllBuckets();
            string bucketidComposed = (stage + ".c-" + bucketName);
            Bucket bucket = (from bucketTmp in buckets
                             where bucketTmp.Id.Equals(bucketidComposed, comparisonType: StringComparison.InvariantCultureIgnoreCase)
                             select bucketTmp).FirstOrDefault();
            if (bucket != default(Bucket))
                return false;
            logger.Trace("Bucket " + bucketidComposed + " doesnt exist, creating new one..");
            //bucket does not exist, we will create it
            bucket = CreatBucket(_token, bucketName, stage, description);
            if (bucket == null)
                throw new Exception("CreateBucket failed: bucket" + bucketName + " == null");
            return true;

        }


        public List<Bucket> ListAllBuckets()
        {
            logger.Trace("Listing all buckets:");
            string uri = "/storage/buckets";

            var result = _client.SendGetRequestToJson<List<Bucket>>(uri);
            logger.Trace("Listing all buckets:" + uri + " returned count=" + result.Count);
            return result;

        }

        public bool DropTable(string tableid)
        {
            string uri = "/storage/tables/" + tableid;
            return _client.SendDeleteRequest(uri);

        }


        public bool UpdateTableAsync(string token, string tableid, FileInfo file, bool isIncremental = false)
        {
            string incremental = "0";
            string incrementalMsg = "";

            if (isIncremental)
            {
                incremental = "1";
                incrementalMsg = " incrementaly";
            }
            //upload file to S3 and obtain its id from SAPI
            logger.Trace("uplaoding file " + file.FullName);
            string uploadFileId = UploadFile(file.FullName, false, false);     


            logger.Trace("Updating table " + tableid + " with " + file.Name + incrementalMsg);
            var postData = new Dictionary<string, string>() { 
                {"dataFileId", uploadFileId},                       
                {"incremental", incremental}
            };

            string uri = "/storage/tables/" + tableid + "/import-async";
            _client.AddStringFormData(postData);
            var updateStream = _client.SendPostRequestReadAsStream(uri);
            string jobid = StreamToJson(updateStream)["id"].ToObject<string>();

            WaitForTaskToFinish(jobid);
                        
            return true;

        }

        //OBSOLETE!!!
        //public Stream UpdateTable(string token, string tableid, FileInfo file, bool isIncremental = false)
        //{
        //    string incremental = "0";
        //    string incrementalMsg = "";

        //    if (isIncremental)
        //    {
        //        incremental = "1";
        //        incrementalMsg = " incrementaly";
        //    }

        //    logger.Trace("Updating table " + tableid + " with " + file.Name + incrementalMsg);
        //    var postData = new Dictionary<string, string>() { 
        //        {"token", token},                       
        //        {"incremental", incremental}
        //    };

        //    string uri = "/storage/tables/" + tableid + "/import";
        //    _client.AddStringFormData(postData);

        //    _client.AddFileFormData("data", file);
        //    return _client.SendPostRequestReadAsStream(uri);

        //}

        JObject StreamToJson(Stream stream)
        {

            string strResponse = new StreamReader(stream).ReadToEnd();
            return JObject.Parse(strResponse);    
        
        }

        /// <summary>
        /// Uploads file to S3 via SAPI
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isPublic"></param>
        /// <param name="notify"></param>
        /// <returns></returns>
        public string UploadFile(string filePath, bool isPublic = false, bool notify = true)
        {
            // 1. prepare resource
            FileInfo finfo = new FileInfo(filePath);
            var postData = new Dictionary<string, string>() { 
                {"isPublic", isPublic.ToString()},                       
                {"notify", notify.ToString()},                       
                {"name", finfo.Name},                       
                {"sizeBytes", finfo.Length.ToString()}                       
               
            };
            _client.AddStringFormData(postData);
            //_client.AddFileFormData("data", file);
            string prepareUri = "/storage/files/prepare";
            Stream result = _client.SendPostRequestReadAsStream(prepareUri);
            string strResponse = new StreamReader(result).ReadToEnd();            
            var json = Newtonsoft.Json.Linq.JObject.Parse(strResponse);            
            var uploadParams = json["uploadParams"];


            // 2. upload directly do S3 using returned credentials
            var s3Client = new HttpMultipartDataClient(uploadParams["url"].ToObject<string>(), new Dictionary<string, string>());
            postData = new Dictionary<string, string>() { 
                {"key", uploadParams["key"].ToObject<string>()},                       
                {"acl", uploadParams["acl"].ToObject<string>()},                       
                {"signature", uploadParams["signature"].ToObject<string>()},                       
                {"policy", uploadParams["policy"].ToObject<string>()},
                {"AWSAccessKeyId", uploadParams["AWSAccessKeyId"].ToObject<string>()}                      
            };
            string uploadUri = "/";
            s3Client.AddStringFormData(postData, false);
            s3Client.AddFileFormData("file", finfo);
            s3Client.SendPostRequestReadAsStream(uploadUri);

            return json["id"].ToObject<string>();
        }


    public Stream GetJob(string jobid)
    {
        string uri = "storage/jobs/" + jobid;
        return _client.SendGetRequestReadAsStream(uri);
    
    }

        /// <summary>
        /// wait for async task to finish synchroniuosly
        /// </summary>
        /// <param name="jobid"></param>
        /// <returns></returns>
     private bool WaitForTaskToFinish(string jobid)
    {
         var jobdata = StreamToJson(GetJob(jobid));
         string jobstatus = jobdata["status"].ToObject<string>();
         //set timeout to 40 minutes
        DateTime maxEndTime = DateTime.Now.AddMinutes(40);
        int maxWaitPeriod = 60;
        int retries = 0;
        int waitSeconds = 0;
        // poll for status
        do {           

            if (DateTime.Now >= maxEndTime) {
                throw new Exception("Poll timeout after 40 minutes");
            }

            waitSeconds = Math.Min(System.Convert.ToInt32(Math.Pow(2.0, System.Convert.ToDouble(retries))),  
                maxWaitPeriod);
            System.Threading.Thread.Sleep(waitSeconds * 1000);
            retries++;

            jobdata = StreamToJson(GetJob(jobid));
            jobstatus = jobdata["status"].ToObject<string>();
        } while(jobstatus != "success" && jobstatus != "error");


        if (jobstatus == "error") {
            throw new Exception("Error waiting for the job:" +
                jobdata["error"]["code"].ToObject<string>() +
                jobdata["error"]["message"].ToObject<string>() );
        }

        return true;
    
    }



        public bool CreateTableFromCsvAsync(string bucketid, string token, string tableName, FileInfo file, string primaryKey = "Id")
        {
            logger.Trace("Creating new table " + tableName + " in " + bucketid + " with " + file.Name);
            string uri = "/storage/buckets/" + bucketid + "/tables-async";           

            //upload file to S3 and obtain its id from SAPI
            string uploadFileId = UploadFile(file.FullName, false, false);            

            //prepare multipart form data
            var postData = new Dictionary<string, string>() { 
                {"dataFileId", uploadFileId},
                {"name", tableName}                 
            };

            if (primaryKey != "")
                postData.Add("primaryKey", primaryKey);
            
            _client.AddStringFormData(postData);
            //send the request
            var createStream = _client.SendPostRequestReadAsStream(uri);
            string createResultStr = new StreamReader(createStream).ReadToEnd();
            var resultJson = Newtonsoft.Json.Linq.JObject.Parse(createResultStr);
            string jobid = resultJson["id"].ToObject<string>();
            WaitForTaskToFinish(jobid);
            return true;
           
        }


        //OBSOLETE!!!
        //public Stream CreateTableFromCsv(string bucketid, string token, string tableName, FileInfo file, string primaryKey = "Id")
        //{
        //    logger.Trace("Creating new table " + tableName + " in " + bucketid + " with " + file.Name);
        //    var postData = new Dictionary<string, string>() { 
        //        {"token", token},
        //        {"name", tableName},                 
        //    };

        //    if (primaryKey != "")
        //        postData.Add("primaryKey", primaryKey);

        //    string uri = "/storage/buckets/" + bucketid + "/tables";
        //    _client.AddStringFormData(postData);
        //    _client.AddFileFormData("data", file);
        //    return _client.SendPostRequestReadAsStream(uri);

        //}


        /// <summary>
        /// prepares and returns URI for get table data request
        /// </summary>
        /// <param name="tableid"></param>
        /// <returns></returns>
        private string _GetTableDataUri(string tableid)
        {
            return "/storage/tables/" + tableid + "/export";
        }
        /// <summary>
        /// GET "/storage/tables/" + tableid + "/export"
        /// Exports table into csv:
        /// returns data contained in the table specified by tableid
        /// data are returned in CSV format
        /// </summary>
        /// <param name="tableid"></param>
        /// <returns></returns>
        public Stream GetTableDataStream(string tableid)
        {
            logger.Trace("get table data as stream from " + tableid);
            string uri = _GetTableDataUri(tableid);
            Stream result = _client.SendGetRequestReadAsStream(uri);
            return result;
        }



        //DOES NOT WORK, need more tesint!
        /// <summary>
        /// GET "/storage/tables/" + tableid + "/export"
        /// !!!can be aplied only to a CSV without firs line columne set
        /// Exports table into csv:
        /// returns data contained in the table specified by tableid
        /// data are returned in CSV format
        /// </summary>
        /// <param name="tableid"></param>
        /// <returns></returns>
        //public IEnumerable<AnonymCsvRow> GetTableData(string tableid)
        //{
        //    return GetTableData<AnonymCsvRow>(tableid,'\n',true,false);    
        //}


        /// <summary>
        /// /// <summary>
        /// GET "/storage/tables/" + tableid + "/export"
        /// Exports table into csv according LINQtoCsv conenctions see:
        /// http://www.aspnetperformance.com/post/LINQ-to-CSV-library.aspx
        /// returns data contained in the table specified by tableid
        /// data are returned in CSV format
        /// </summary>
        /// <typeparam name="TRowType"></typeparam>
        /// <param name="tableid"></param>
        /// <param name="firstLineHasColumn">if first line has column definition</param>
        /// <param name="enforceCsvColumnAttributes">if all members of the TRowType class represent columns(true),
        /// if false : only those who has Csv attribute parameter set</param>
        /// <returns></returns>
        public IEnumerable<TRowType> GetTableData<TRowType>(string tableid,
            char separator = ',',
            bool firstLineHasColumn = true,
            bool enforceCsvColumnAttributes = false) where TRowType : class, new()
        {
            Stream stream = GetTableDataStream(tableid);

            CsvFileDescription inputFileDescription = new CsvFileDescription
            {
                SeparatorChar = separator,
                FirstLineHasColumnNames = firstLineHasColumn,
                EnforceCsvColumnAttribute = enforceCsvColumnAttributes
            };

            CsvContext cc = new CsvContext();
            StreamReader reader = new StreamReader(stream);
            logger.Trace("reading table data from stream into CSV");
            return cc.Read<TRowType>(reader, inputFileDescription);
        }


        /// <summary>
        /// GET /storage/buckets/{bucket_id}/tables
        /// returns all info about tables in the bucket
        /// </summary>
        /// <param name="bucketid"></param>
        /// <returns>TableInfo</returns>
        public List<TableInfo> TablesInBucket(string bucketid)
        {
            //BUCKET EXISTS ????
            var buckets = ListAllBuckets();
            if (buckets.Where(a => a.Id.Equals(bucketid, comparisonType: StringComparison.InvariantCultureIgnoreCase)).Count() < 1)
                return new List<TableInfo>();


            string uri = "/storage/buckets/" + bucketid + "/tables";
            List<TableInfo> result = _client.SendGetRequestToJson<List<TableInfo>>(uri);
            return result;
        }




        /* DEPRECATED
        public string ReadResponsePost(string postRequest, NameValueCollection body)
        {            
            for(
            return _client.SendGetRequestReadAsStream(string);        
        }

        public System.IO.Stream ListTableAttributesStream(string bucketid, string attributekey)
        {
            string uri = "/storage/tables/" + bucketid + "/attributes/" + attributekey;
            return _client.SendGetRequestReadAsStream(uri);

        }
         */




    }
}
