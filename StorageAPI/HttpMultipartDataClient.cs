using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Keboola.LogHelper;
//using System.Windows.Input;

namespace Keboola.StorageAPI
{
    public class HttpMultipartDataClient
    {
        static Logger logger = new Logger(System.Reflection.Assembly.GetExecutingAssembly());
        HttpClient _client = null;
        MultipartFormDataContent _formDataContent = null;

        public HttpMultipartDataClient(string serverAddress, Dictionary<string,string> headers)
        {
           
            _client = new HttpClient();
            _client.BaseAddress = new Uri(serverAddress);
           
            SetHeaders(headers);
        
        }

        public void SetHeaders (Dictionary<string, string> headers)
        {               
           foreach (var header in headers)
           {
               _client.DefaultRequestHeaders.Add(header.Key, header.Value);                
           }           
        }

        public void AddFileFormData(string name, FileInfo file)
        {
            FileStream fstream = File.OpenRead(file.FullName);
            //StreamReader reader = new StreamReader(file.FullName);    
            //_formDataContent = new MultipartFormDataContent(); - the file is added after AddStringFormData is called where this formdata is inicialized
            logger.Trace("Adding file stream " + file.Name + " to HTTP body request");
            _formDataContent.Add(new StreamContent(fstream), name, file.Name);
        
        }

        public void AddStringFormData(Dictionary<string,string> data)
        {
            logger.Trace("Adding values to HTTP body request:" + data.ToString());
            _formDataContent = new MultipartFormDataContent();
            foreach (var pair in data)
                _formDataContent.Add(new StringContent(pair.Value, Encoding.UTF8), pair.Key);    
        
        }


        private static bool _HandleTask(Task task)
        {
            bool isEnsure = false;
            try
            {
               
                //logger.Trace("Waiting for response...");
                task.Wait();
                if (task.GetType() == typeof(Task<HttpResponseMessage>))
                {
                    isEnsure = true;
                    ((Task<HttpResponseMessage>)task).Result.EnsureSuccessStatusCode();
                }
                return true;
            } 
            catch (AggregateException ae)
            {
                string taskType = "Failed while waiting for the HTTP response.";
                if (isEnsure)
                    taskType = "Failed while ensuring that the HTTP response was successful(Status Code in range 200-299).";
                var exception = ae.Flatten();
                string mymessage = "This mostly happens due to some error during export to SAPI. Quick fix: drop the appropiate table(if exists) that is being exported into out stage or check its events.";
                throw new Exception(mymessage + taskType + "[AggregateException]:" + exception.Message, exception);
                //
                //logger.Exception("Failed handling request task[AggregateException]:" + exception.InnerException
                //    + exception.StackTrace);
            }
            catch(Exception ee)
            {
                string taskType = "Failed while waiting for the HTTP response.";
                if (isEnsure)
                    taskType = "Failed while ensuring that the HTTP response was successful(Status Code in range 200-299).";

                string mymessage = "This mostly happens due to some error during export to SAPI. Quick fix: drop the appropiate table(if exists) that is being exported into out stage or check its events.";
                throw new Exception(mymessage + taskType + ee.Message, ee);

                //logger.Exception("Failed handling request task:" + ee.Message
                //    + ee.StackTrace);            
            }
            return false;
                    
        }

        
        private static TResult _handleRequestTask<TResult>(Task<HttpResponseMessage> task)
        {
            
            if (_HandleTask(task))
            {
                //logger.Trace("Reading response..");
                var readTask = task.Result.Content.ReadAsAsync<TResult>();
                if (_HandleTask(readTask))
                    return readTask.Result;
            }

            return default(TResult);           
        }

        public bool SendDeleteRequest(string requestUri)
        {
            logger.Trace("sending DELETE request:" + requestUri);
            var task = _client.DeleteAsync(requestUri);
            try
            {
                return _HandleTask(task);

            }
            catch(Exception ee)
            { 
                logger.Exception("Could not delete becasue:" + ee.Message);
                return false;
            
            }
        
        }
        public TResult SendPostRequestToJson<TResult>(string requestUri) where TResult: class
        {
            logger.Trace("sending POST Json request:" + requestUri);
            var task = _client.PostAsync(requestUri, _formDataContent);
            return _handleRequestTask<TResult>(task);
        
        }

        public Stream SendPostRequestReadAsStream(string requestUri)
        {
            logger.Trace("sending POST to stream response request:" + requestUri );
            var task = _client.PostAsync(requestUri, _formDataContent);
            if (_HandleTask(task))
            {
                var task2 = task.Result.Content.ReadAsStreamAsync();
                if (_HandleTask(task2))
                    return task2.Result;
            }

            return null;
        }


        public TResult SendGetRequestToJson<TResult>(string requestUri) where TResult: class
        {
            logger.Trace("sending GET Json request:" + requestUri);
            var task = _client.GetAsync(requestUri);
            return _handleRequestTask<TResult>(task);                  
        }

        public Stream SendGetRequestReadAsStream(string requestUri)
        {
            logger.Trace("sending GET to stream response request:" + requestUri);
            var task = _client.GetAsync(requestUri);
            if (_HandleTask(task))
            {
                var task2 = task.Result.Content.ReadAsStreamAsync();
                if (_HandleTask(task2))
                    return task2.Result;           
            }
           
            return null;        
        }

         
        

    }
}
