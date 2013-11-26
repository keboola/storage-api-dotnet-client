using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LINQtoCSV;

namespace Keboola.StorageAPI.DataStructures
{
    /*
     * "uri": "https://connection.keboola.com/storage/tables/in.c-main.tweets",
        "id": "in.c-main.tweets",
        "name": "tweets",
        "gdName": "",
        "created": "2012-07-13 09:58:47",
        "lastImportDate": "2012-07-13 10:00:42"
     */

    public class CrmQueryTable
    {
        [CsvColumn(Name="load")]
        public string LoadType;
        [CsvColumn(Name = "query")]
        public string Query;    
    
    }

    [DataContract]
    public class ImportConfiguration
    {
        [DataMember(Name="load")]
        [CsvColumn(Name = "load")]
        public string LoadType;

        [DataMember(Name = "query_type")]
        [CsvColumn(Name = "query_type")]
        public string QueryType;

        [DataMember(Name = "is_history")]
        [CsvColumn(Name = "is_history")]
        public bool IsHistory;

        [DataMember(Name = "query")]
        [CsvColumn(Name = "query")]
        public string Query;

        [DataMember(Name = "active")]
        [CsvColumn(Name = "active")]
        public bool Active;
            
    }


    public interface IDataRow
    {
        // Number of data row items in the row.
        int Count { get; }

        // Clear the collection of data row items.
        void Clear();

        // Add a data row item to the collection.
        void Add(DataRowItem item);

        // Allows you to access each data row item with an array index, such as
        // row[i]
        DataRowItem this[int index] { get; set; }
    }
    /// <summary>
    /// object consist of list of DataRowItems which represents one line in CSV file es Enumerable<DataRow>
    /// each DataRow then contains LineNbr(int) which is line number and Value(string) which is the whole liine itself
    /// </summary>
    public class AnonymCsvRow : List<DataRowItem>,IDataRow
    {   
    }



//    {
//    "id": "44",
//    "token": "your_token",
//    "description": "",
//    "uri": "https://connection-devel.keboola.com/storage/tokens/44",
//    "isMasterToken": false,
//    "bucketPermissions": [],
//    "owner": {
//        "name": "Buckets 5",
//        "id": "58"
//    }
//}


    [DataContract]    
    public class TokenInfo
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "token")]
        public string Token { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "uri")]
        public string Uri { get; set; }

        [DataMember(Name = "isMasterToken")]
        public bool IsMasterToken { get; set; }

        [DataMember(Name = "bucketPermissions")]
         public Dictionary<string,string> BucketPermissions { get; set; }

      //  [DataMember(Name = "owner")]
     //   public Dictionary<string,string> Owner { get; set; }             
    
    
    }

    [DataContract]    
    public class TableInfo
    {
        
        [DataMember(Name = "uri")]        
        public string Uri { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }
       

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "gdName")]
        public string GdName { get; set; }


        [DataMember(Name = "created")]
        public DateTime Created { get; set; }

        [DataMember(Name = "lastImportDate")]
        public DateTime LastImport { get; set; }



    }

   
    /*
    "uri": "https://connection.keboola.com/storage/buckets/in.c-ga",
    "id": "in.c-ga",
    "name": "c-ga",
    "stage": "in",
    "description": "Google Analytics",
    "tables": "https://connection.keboola.com/storage/buckets/in.c-ga/tables"
     */
    [DataContract]
    public class Bucket
    {
        [DataMember(Name="id")]
        public string Id { get; set; }


        [DataMember(Name="uri")]
        public string Uri { get; set; }

        [DataMember(Name="name")]
        public string Name { get; set; }    


        [DataMember(Name="stage")]
        public string Stage { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "tables")]
        public string Tables { get; set; }

    
    }



    [DataContract]
    public class BucketRequest
    {

        [DataMember(Name = "name")]
        public string Name { get; set; }


        [DataMember(Name = "stage")]
        public string Stage { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "token")]
        public string Token { get; set; }


    }
}