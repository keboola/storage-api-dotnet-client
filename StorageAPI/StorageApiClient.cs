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


namespace Keboola.StorageAPI
{
    public class StorageApiClient
    {
        static Logger logger = new Logger(System.Reflection.Assembly.GetExecutingAssembly());
        public static string ServerAddress = "https://connection.keboola.com/v2";
        public static string HeaderTokenName = "X-StorageApi-Token";

        private HttpMultipartDataClient _client;
        private string _token = null;
        public string Token { get { return _token; } private set { ;} }

        public StorageApiClient(string token)        
        {
            _token = token;
            _client = new HttpMultipartDataClient(ServerAddress,
                new Dictionary<string, string>() { { HeaderTokenName, _token } });       
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


        //
        /// <summary>
        ///https://connection.keboola.com/storage/tables/sys.c-MSCRM.Centrum/attributes/organization 
        /// </summary>
        /// <param name="tableid"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        public void SetTableAttribute(string tableid, string attribute,string value = "")
        {
            logger.Trace("Setting table attribute:" + attribute + " for " + tableid);
            Dictionary<string, string> formData = new Dictionary<string, string>()
            {
                {"value", value}
            };
            string uri = "/storage/tables/" + tableid + "/attributes/" + attribute ;

            _client.AddStringFormData(formData);
            var result = _client.SendPostRequestToJson<Dictionary<string, string>>(uri);

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
            return _client.SendGetRequestToJson<Dictionary<string, string>>(uri);

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
                             where bucketTmp.Id.Equals( bucketidComposed, comparisonType: StringComparison.InvariantCultureIgnoreCase)
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

        public Stream UpdateTable(string token, string tableid, FileInfo file, bool isIncremental = false)
        {
            string incremental = "0";
            string incrementalMsg = "";

            if (isIncremental)
            {
                incremental = "1";
                incrementalMsg = " incrementaly";
            }

            logger.Trace("Updating table " + tableid + " with " + file.Name + incrementalMsg);
            var postData = new Dictionary<string, string>() { 
                {"token", token},                       
                {"incremental", incremental}
            };
            
            string uri = "/storage/tables/" + tableid +"/import";
            _client.AddStringFormData(postData);
            
            _client.AddFileFormData("data", file);
            return _client.SendPostRequestReadAsStream(uri);

        }


        public Stream CreateTableFromCsv(string bucketid,string token, string tableName, FileInfo file, string primaryKey = "Id")
        {
            logger.Trace("Creating new table " + tableName + " in " + bucketid + " with " + file.Name);
            var postData = new Dictionary<string, string>() { 
                {"token", token},
                {"name", tableName}, 
                
            };

            if (primaryKey != "")
                postData.Add("primaryKey", primaryKey);

            string uri = "/storage/buckets/" + bucketid + "/tables";
            _client.AddStringFormData(postData);
            _client.AddFileFormData("data", file);
            return _client.SendPostRequestReadAsStream(uri);                 
        
        }


        /// <summary>
        /// prepares and returns URI for get table data request
        /// </summary>
        /// <param name="tableid"></param>
        /// <returns></returns>
        private string _GetTableDataUri(string tableid)
        { 
            return "/storage/tables/"+tableid+"/export";        
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
            bool enforceCsvColumnAttributes = false) where TRowType:class, new()
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
