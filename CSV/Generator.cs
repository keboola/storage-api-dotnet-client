using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Keboola.LogHelper;

namespace Keboola.CSV
{
    public class Generator
    {
        static Logger logger = new Logger(System.Reflection.Assembly.GetExecutingAssembly());
        private ThreadLocal<StreamWriter> _streamWriter = null;
        private string _filename;
        private char _quote = '"';
        private char _delimiter = ',';
        private string _errorMessage = "";
        private string _newLine = System.Environment.NewLine;
        private int _flushCountLimit = 20000;
        private int _writtenCount = 0;
        private bool _isDelimitedInFront = false;


        public int Written
        {
            get { return _writtenCount; }
            private set { ;}        
        }

        public bool IsOpen
        {
            get { return Streamer != null; }
            private set { ;}       
        }

        public string LastErrorMessage
        {
            get { return _errorMessage; }
            private set { ;}        
        }

        public string FileName
        {
            get { return _filename; }
            private set { _filename = value; }
        }

        private StreamWriter Streamer
        {
            get {
                try
                {
                    return _streamWriter.Value;
                }
                catch (Exception ee)
                {
                    throw ee;
                    //return null; 
                
                }
            }    
        }
        

        public Generator(string filename)
        {
            _writtenCount = 0;
            _filename = filename;
            _streamWriter = new ThreadLocal<StreamWriter>(() => 
                {
                    try
                    {
                        return new StreamWriter(_filename);
                    }
                    catch (Exception ee)
                    {
                        _errorMessage = "Open Error:" + ee.Message;
                        throw ee;
                        //return null;
                    }
                });
        }

        private void _WriteChar(char c)
        {
            try
            {
                Streamer.Write(c);
                _writtenCount++;
                _CheckFlush();
            }
            catch(Exception ee)
            {
                _errorMessage = "Write char Error:" + ee.Message;
                throw ee;                
            }
           
        }

        private void _CheckFlush()
        {
            return;
            //if (_flushCountLimit % _writtenCount == 0)
             //   Streamer.Flush();
        }

        private void _WriteString(string s)
        {
            try
            {
                Streamer.Write(s);
                _writtenCount += s.Length;
                _CheckFlush();
            }
            catch (Exception ee)
            {
                _errorMessage = "Write string Error:" + ee.Message;
                throw ee;
                //return false;
            }
          
        }



        private void _WriteQoute()
        {
            _WriteChar(_quote);
        }
        public void WriteNewLine()
        {
            _isDelimitedInFront = false;
            _WriteString(_newLine);           

        }

        private void _WriteDelimiter()
        {
            if (_isDelimitedInFront)
            {
                _WriteChar(_delimiter);
                return;
            }
            _isDelimitedInFront = true;         

        }

        public void WriteEmptyValue()
        {           
           _WriteDelimiter();
           _WriteQoute();
           _WriteQoute();       
        }


        public void WriteValue(string value)
        {
           _WriteDelimiter();

           _WriteQoute();

            //write each character of the value, if the character contains quote then another qoute before it            
            foreach (char c in value)
            {
                //we skip the white spaces
                if (c == '\n' || c == '\t' || c == '\r')
                    continue;

                if (c == _quote)
                   _WriteQoute();

               _WriteChar(c);
            }

            _WriteQoute();
           
        }           

        public void Close()
        {            
            try
            {
                Streamer.Close();
            }
            catch (Exception ee)
            {
                _errorMessage = "Close error:" + ee.Message;
                throw ee;
                //return false;
            }

            
        
        }

       


    }
}
