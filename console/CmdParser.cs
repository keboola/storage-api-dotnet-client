using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;
using System.Reflection; // if you want text formatting helpers (recommended)

namespace console
{

    public abstract class CommonSubOptions
    {
        [Option('q', "quiet",  HelpText = "Suppress summary message.")]
        public bool Quiet { get; set; }

        [Option('v', "verbose", HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        public abstract string Usage();


        IEnumerable<Tuple<object, object,object>> GetAttributeAndValueAndProperty(Type attributeType)
        { 
            var properties = this.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var attrs = prop.GetCustomAttributes(false);
                foreach (var attr in attrs)
                    if (attr.GetType() == attributeType)
                    {
                        yield return new Tuple<object, object,object>(attr, prop.GetValue(this,null),prop);
                    }
            }
        
        }

        public bool CheckMissingArguments()
        {
            foreach (var arg in GetCommandArgumentsValues())
                if (arg == null || arg.ToString() == ""  )
                    return false;
            return true;
        
        }

        


        IEnumerable<object> GetCommandArgumentsValues()
        {
            foreach (var item in GetAttributeAndValueAndProperty(typeof(ValueOptionAttribute)))
                yield return item.Item2;
        
        }


        public List<string> GetArgumentsNames()
        {


            var result = new List<Tuple<string,int>>();
            foreach (var item in GetAttributeAndValueAndProperty(typeof(ValueOptionAttribute)))
            {
                var idx = ((ValueOptionAttribute)item.Item1).Index;
                var property = item.Item3 as PropertyInfo;
                result.Add(new Tuple<string, int>(property.Name, idx));
            }


            var aa = (from items in result
                      orderby items.Item2 ascending
                      select items.Item1).ToList();

            return aa;        
        }
    }



    public class Options
    {


        public Options()
        {
            // Since we create this instance the parser will not overwrite it
            UploadVerb = new CreateTableCommand { };
            WriteTableVerb = new WriteTableCommand { };
            TestVerb = new TestCommand { };

        }

        [VerbOption("test", HelpText = "For testing purposes")]
        public TestCommand TestVerb { get; set; }

        [VerbOption("create-table", HelpText = "Create table in bucket")]
        public CreateTableCommand UploadVerb { get; set; }


        [VerbOption("write-table", HelpText = "Write data into table")]
        public WriteTableCommand WriteTableVerb { get; set; }



        

        /// <summary>
        /// returns an instance of the member of this class according to the VerbOption LongName
        /// </summary>
        /// <param name="verb"></param>
        /// <returns></returns>
        object GetVerbCommandInstance(string verb)
        { 
            var properties = this.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var attrs = prop.GetCustomAttributes(false);
                foreach(var attr in attrs)
                    if (attr is VerbOptionAttribute)
                    { 
                        var a = (VerbOptionAttribute)attr;
                        if(a.LongName == verb)                            
                           return prop.GetValue(this, null);              
                    }           
            }
        
            return null;
        }



        [HelpVerbOption]
        public string GetUsage(string verb)
        {                
            var htext = HelpText.AutoBuild(this, verb);

            if (verb != null)
            {
                object verbInstance = GetVerbCommandInstance(verb);
                CommonSubOptions suboption = (CommonSubOptions)verbInstance;
                //htext.AddPostOptionsLine();
                htext.Heading = "usage: " + suboption.Usage();
                htext.Copyright = "options:";
            }
            else
            {
                object[] sapiattrs = Assembly.GetAssembly(typeof(Keboola.StorageAPI.StorageApiClient)).GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                string sapiclientversion = sapiattrs.Length == 0 ? "" : ((AssemblyInformationalVersionAttribute)sapiattrs[0]).InformationalVersion;
                htext.Copyright = "Storage Api Client " + sapiclientversion;
            }
            return htext;             
        }
     

    };

    
    public static class CmdParser
    {
        
        public static ISapiCommand ParseArguments(string[] args)
        {
            string invokedVerb;
            ISapiCommand invokedVerbInstance = null;

            var options = new Options();
        
            if (CommandLine.Parser.Default.ParseArguments(args, options,
              (verb, subOptions) =>
              {
                  // if parsing succeeds the verb name and correct instance
                  // will be passed to onVerbCommand delegate (string,object)                    
                  
                  invokedVerb = verb;
                  if (subOptions != null)
                  {
                      invokedVerbInstance = (ISapiCommand)subOptions;
                  }
              }) == false)
            {
                //Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
                return null;
            }           

            return invokedVerbInstance;
        }
    }
}
