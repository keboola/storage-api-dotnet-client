using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Keboola.StorageAPI;
using System.IO;

namespace console
{
    class Program
    {
        



        static int Main(string[] args)
        {
            bool success = false;
            try
            {
                // args = ParseArguments();
                // client = PrepareClient(args);
                // UploadData(args, client);

                var command = CmdParser.ParseArguments(args);           
                Controller ctrl = new Controller(command);
                success = ctrl.Execute();
           
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }

            if (success)
                return 0;
            
            return 1;
        }

        

    }
}
