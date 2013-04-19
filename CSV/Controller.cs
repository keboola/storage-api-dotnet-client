using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Keboola.LogHelper;

namespace Keboola.CSV
{



    public delegate IEnumerable<string> WriteLineMethod();
    public class Controller
    {
        static Logger logger = new Logger(System.Reflection.Assembly.GetExecutingAssembly());
        FileInfo _fileInfo = null;
        Generator _generator = null;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="filename"></param>
        public Controller(string filename, bool useAppData = true)
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
            if (useAppData == false)
                dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

            if (Directory.Exists(dataDir) == false)
                Directory.CreateDirectory(dataDir);

            string path = Path.Combine(dataDir, filename + ".csv");
            
            _fileInfo = new FileInfo(path);          
            _generator = new Generator(_fileInfo.FullName);        
        }

        public string FullPathName
        {
            get { return _fileInfo.FullName; }
            private set { ;}        
        }

        /// <summary>
        /// writes exactly one line composed from many generated strings(IEnumerable<string>)
        /// </summary>
        /// <param name="method">Method functor that returns IEnumerable<string></param>
        public void WriteLine(WriteLineMethod method)
        {
            foreach(var value in method())
            {
                if(value == null)
                        _generator.WriteEmptyValue();
                    else
                        _generator.WriteValue(value);           
            }

            _generator.WriteNewLine();
        }

        private IEnumerable<string> FlushListDelegate(List<string> list)
        {
            foreach (var item in list)
                yield return item;        
        }

        public void FlushListInToCSV(List<string> list)
        {
            WriteLine(() => { return FlushListDelegate(list); });
        
        }

        /// <summary>
        /// same as WriteLine but can count number of attributes which then can be used to check
        /// number of written values in WriteLine, the check control ability is not implemented yet!        
        /// </summary>
        /// <param name="method"></param>
        public void WriteHeader(WriteLineMethod method)
        {
            foreach (var value in method())
            {
                if (value == null)
                    _generator.WriteEmptyValue();
                else
                    _generator.WriteValue(value);
            }
            _generator.WriteNewLine();                    
        }

        public static string Compress(FileInfo fileToCompress)
        {
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                            return compressedFileStream.Name;
                            //Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                             //   fileToCompress.Name, fileToCompress.Length.ToString(), compressedFileStream.Length.ToString());
                        }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Closes the handle of opened file
        /// </summary>
        public void Close()
        {
            _generator.Close();        
        }


    }
}
