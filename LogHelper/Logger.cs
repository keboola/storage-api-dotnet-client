using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Keboola.LogHelper
{

    public static class CommonUtils
    {

        static string IsDevelModeString = "isDevel";
        static string ApplicationName = "MSCRM-Extractor";

        /// <summary>
        /// Converts a dictionary into string readable format
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dic"></param>
        /// <param name="format"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ToFormattedString<TKey, TValue>(this IDictionary<TKey, TValue> dic, string format, string separator)
        {
            return String.Join(
                !String.IsNullOrEmpty(separator) ? separator : " ",
                dic.Select(p => String.Format(
                    !String.IsNullOrEmpty(format) ? format : "{0}='{1}'",
                    p.Key, p.Value)));
        }

        /// <summary>
        /// Converts date into into local time zone date string in format yyyy-MM-dd HH:mm:sszzz
        /// eg. 2012-11-30 20:00:00+01:00
        /// </summary>
        /// <param name="date">date in UTC + offset</param>
        /// <returns></returns>
        public static string ToLocalDateStr(DateTime date)
        {
            return date.ToLocalTime().ToString("yyyy-MM-dd HH:mm:sszzz");       
        }
        /// <summary>
        /// checks application settings in Web.config of the OrchestratorServiceHost project and
        /// returns true if it is in devel mode otherwise false
        /// </summary>
        /// <returns></returns>
        public static bool IsDevel()
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            if (appSettings.AllKeys.Contains(IsDevelModeString) == false)
                return true;

            return (appSettings[IsDevelModeString] == "true");

        }

        /// <summary>
        /// returns application name thats is primary intented to use in logs
        /// if application is in devel mode which depends on the deploy configuration(Debug/Release)
        /// it returns app name with "-Devel" appended in its end
        /// </summary>
        /// <returns></returns>
        public static string GetApplicationName()
        { 
            if(IsDevel())
                return ApplicationName + "-devel";
            
            return ApplicationName;
        }
    
    }
    public class ErrorList
    {
        class Identification
        {
            public string Method;
            public string Message;    
            public string Component;
        }

        List<Identification> Errors = new List<Identification>();
        List<Identification> Warnings = new List<Identification>();
        List<Identification> Exceptions = new List<Identification>();

        public string GetCounts()
        {
            return "Warnings:" + Warnings.Count + ", Errors:" + Errors.Count + ", Criticals:" + Exceptions.Count;
        
        }

        public bool HasErrorsOrExceptions()
        {
            return Errors.Count > 0 || Exceptions.Count > 0 || Warnings.Count > 0;
        
        }


        Dictionary<string,object> GetDict(List<Identification> list, string priority)
        {
            var result = new Dictionary<string,object>();
            result.Add("Count", list.Count.ToString());
            int idx = 1;
            List<object> tmpList = new List<object>();
       
            foreach(var item in list)
            {
                Dictionary<string,object> errorItem = new Dictionary<string,object>(){
                {"Component", item.Component},
                {"Method",item.Method},
                {"Message", item.Message}
                };
                tmpList.Add(errorItem);
                
                idx++;            
            }
            result.Add(priority, tmpList);
            return result;        
        }


        public Dictionary<string,object> GenerateOutput()
        {
            var result = new Dictionary<string,object>(){           
            {"Warnings", GetDict(Warnings , "WARN")},
            {"Errors", GetDict(Errors , "ERR")},
            {"Criticals", GetDict(Exceptions , "CRIT")}
             };      

            return result;
        }


      
        public int AddException(string method, string message, string component)
        {
            Exceptions.Add(new Identification { Method = method, Message = message, Component = component });
            return Exceptions.Count;
                
        }

        public int AddWarning(string method, string message, string component)
        {
            Warnings.Add(new Identification { Method = method, Message = message, Component = component });
            return Warnings.Count;
        
        }

        public int AddError(string method, string message, string component)
        {
            Errors.Add(new Identification { Method = method, Message = message, Component = component });
            return Errors.Count;        
        }

        public void Clear()
        {
            Errors.Clear();
            Warnings.Clear();
            Exceptions.Clear();        
        }
    
    }

    /// <summary>
    /// LOGGER CLASS
    /// </summary>
    public class Logger
    {      


        string Component = "";

        private static ThreadLocal<NLog.Logger> _logger = new ThreadLocal<NLog.Logger>(() => { return ConfigureLoggerOnInit(); });
        public  NLog.Logger sLogger
        {
            get { return _logger.Value; }
        }

        public  static ThreadLocal<ErrorList>  ErrorResume = new ThreadLocal<ErrorList>(() => new ErrorList());

        public static ThreadLocal<string> Application = new ThreadLocal<string>(() => CommonUtils.GetApplicationName());
        public  static ThreadLocal<string> Uri = new ThreadLocal<string>(() => "");
        public static ThreadLocal<string> Configuration = new ThreadLocal<string>(() => "");
        public  static ThreadLocal<string> Token = new ThreadLocal<string>(() => "");
        public  static ThreadLocal<string> Pid = new ThreadLocal<string>(() => { return Process.GetCurrentProcess().Id.ToString(); });



        public Logger(Assembly assembly)
        {

            //LogManager.ThrowExceptions = true;
            var version = assembly.GetName().Version;
            Component = assembly.GetName().Name + "-" + version;

        }


        public void DestroyStaticData()
        {
            Uri.Value = "";
            Token.Value = "";
            ErrorResume.Value.Clear();
           
        }

        private static Target ConfigureFileLog(LoggingConfiguration config, string filename = "file.log")
        {

            Uri uri = new System.Uri(Assembly.GetExecutingAssembly().CodeBase);
            //Uri uri = new System.Uri(Directory.GetCurrentDirectory());
            var baseDir = new DirectoryInfo(uri.AbsolutePath).Parent.Parent;

            string filepath = "";
            if (baseDir.Exists)
                filepath = Path.Combine(baseDir.FullName, "App_Data", "file.log");
            else
                return null;

            NLog.Targets.FileTarget target = new FileTarget();
            target.FileName = filepath;
            //target.Name = "f";
            target.Layout = "${longdate} ${message}";
            NLog.Config.LoggingRule rule = new NLog.Config.LoggingRule("*", LogLevel.Trace, target);
            config.AddTarget("file", target);
            config.LoggingRules.Add(rule);

            return target;
        }



        public static NLog.Logger ConfigureLoggerOnInit()
        {

            //throw new Exception("Configureing");
            var logger = LogManager.GetCurrentClassLogger();
            LogManager.ThrowExceptions = true;

            var config = new NLog.Config.LoggingConfiguration();
            ConfigureFileLog(config);

            ConfigurePaperTrailLog(config);         

            LogManager.Configuration = config;
            logger.Info("LOGGER INITIALIZED");
            return logger;


        }

        private static void ConfigurePaperTrailLog(LoggingConfiguration config)
        {

            Syslog papertrail = new Syslog();
            papertrail.Port = 49730;
            // papertrail.Name = "TomaskoExtractor";
            papertrail.Sender = "MSCRM-Extractor";
            papertrail.SyslogServer = "logs.papertrailapp.com";
            papertrail.Facility = Syslog.SyslogFacility.Local7;
            var syslogRule = new NLog.Config.LoggingRule("*", LogLevel.Trace, papertrail);
            config.AddTarget("PaperTrailLogging", papertrail);
            config.LoggingRules.Add(syslogRule);
        }




        public void SetToken(string token)
        {
            Token.Value = token;
        }

        public void SetUri(string uri, bool appendTimeStamp = true)
        {
            if (appendTimeStamp)
                Uri.Value = uri + DateTime.Now.ToString("yyyy-MM-dd|HH:mm:ss");
            else
                Uri.Value = uri;
        
        }

        public void SetConfiguration(string clientname)
        {
            Configuration.Value = clientname;
        }

        public void UnsetConfiguration()
        {
            Configuration.Value = string.Empty;
        }

        private string PrepareData(string priority,string message, string method)
        {     
            var data = new Dictionary<string, string>()
            {               
                  {"message",message},
                  {"priority",priority},
                  {"app", Application.Value},                  
                  {"pid", Pid.Value},               
                  {"runId",Uri.Value},                  
                  {"token",Token.Value},
                  {"configurationId", Configuration.Value},
                  {"Component",Component},
                  {"Method",method}
                  
            
            };
            
            JavaScriptSerializer ser = new JavaScriptSerializer();            
            return ser.Serialize(data) ;

        }

        //trace checnge to debug priority
        public void Trace(string message, bool LogCurrentMethodName = true)
        {
            
           string method = GetCurrentCallingMethod(LogCurrentMethodName);
          
            var data = PrepareData("DEBUG", message, method);


            sLogger.Trace(data);
            
           // _Trace(data);
        
        }

        public void Info(string message, bool LogCurrentMethodName = true)
        {
            string method = GetCurrentCallingMethod(LogCurrentMethodName);            
            var data = PrepareData("INFO", message, method);            
            sLogger.Info(data);

        }

        /*public void Debug(string message, bool LogCurrentMethodName = true)
        {
            string method = GetCurrentCallingMethod(LogCurrentMethodName);
            var data = PrepareData("DEBUG", message, method);
           
            sLogger.Debug(data);

        }
         */

        public void Error(string message, bool LogCurrentMethodName = true)
        {
            string method = GetCurrentCallingMethod(LogCurrentMethodName);
            int idx = ErrorResume.Value.AddError(method, message, Component);
            var data = PrepareData("ERR", message, method);
            sLogger.Error(data);

        }

        public void Warn(string message, bool LogCurrentMethodName = true)
        {
            string method = GetCurrentCallingMethod(LogCurrentMethodName);
            int idx = ErrorResume.Value.AddWarning(method, message, Component);
            var data = PrepareData("WARN", message, method);
            sLogger.Warn(data);

        }

        public void Exception(string message, bool LogCurrentMethodName = true)
        {
            string method = GetCurrentCallingMethod(LogCurrentMethodName);
            int idx = ErrorResume.Value.AddException(method, message, Component);
            var data = PrepareData("CRIT"
                , message, method);
            
            sLogger.Fatal(data);

        }

        public bool IsAnyErrorOrExcpetion()
        {
            return ErrorResume.Value.HasErrorsOrExceptions();        
        }


        public string GetErrorsWarningsExceptionsCount()
        {
            return ErrorResume.Value.GetCounts();        
        }

        public string LogResume()
        {
            string resumeId = GetErrorUniqueId(Uri.Value);
           
            Dictionary<string,object> data = new Dictionary<string,object>(){
                {"Priority","Info"},                 
                {"app", Application.Value},
                {"pid", Pid.Value},                                              
                {"runId",Uri.Value},
                {"token",Token.Value},
                {"configurationId", Configuration.Value},
                {"ResumeID", resumeId},
                {"message", ErrorResume.Value.GenerateOutput()}            
            };
            JavaScriptSerializer ser = new JavaScriptSerializer();
            sLogger.Info(ser.Serialize(data));        
            return resumeId;
        }

        private string GetCurrentCallingMethod(bool LogCurrentMethodName)
        {
            
            if (LogCurrentMethodName)
            {
                try
                {
                    var trace = new StackTrace();
                    var frame = trace.GetFrame(2);

                    var method = frame.GetMethod();
                    return method.DeclaringType.Name + "." + method.Name;
                }
                catch(Exception ee)
                {
                    return "";
                
                }
            }
            return "";
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorSource"></param>
        /// <returns></returns>
        static string GetErrorUniqueId(string errorSource)
        {
            Random rnd = new Random();
            using (System.Security.Cryptography.MD5 md5Hash = System.Security.Cryptography.MD5.Create())
            {
                string hash = GetMd5Hash(md5Hash, errorSource + rnd.Next().ToString() );

                return hash;
            }
                     
        }


        static string GetMd5Hash(System.Security.Cryptography.MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        //#region _private logging obsolete
        //private  void _Trace(string message)
        //{
            
        //    sLogger.Trace(message);
         
        
        //}

        //private  void _Info(string message)
        //{

        //   sLogger.Info(message);
        
        //}

        //private  void _Error(string message)
        //{

        //    sLogger.Error(message);
        
        //}

        //private  void _Debug(string message)
        //{

        //    sLogger.Debug(message);
        
        //}


        //private  void _Warn(string message)
        //{
        //    sLogger.Warn(message);        
        //}


        //private  void _Fatal(string message)
        //{
        //    sLogger.Fatal(message);
        //}
        //#endregion



        //EventLogTarget evntTarget = new EventLogTarget();

        ////evntTarget.Name = "Keboola";
        //evntTarget.Layout = "${longdate} ${message}";
        //evntTarget.Source = "Keboola";

        //var evntrule = new NLog.Config.LoggingRule("*", LogLevel.Trace, evntTarget);
        //config.AddTarget("EventLoging", evntTarget);
        //config.LoggingRules.Add(evntrule);





      
    }
}
