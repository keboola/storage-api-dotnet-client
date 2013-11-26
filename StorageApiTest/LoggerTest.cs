using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Keboola.CSV;
using System.IO;
using Keboola.LogHelper;


namespace Keboola.StorageApiTest
{

    [TestFixture]
    public class LoggerTest
    {

        [Test]
        public void LoggerGenerateOutputTest()
        {
            Logger logger = new Logger(System.Reflection.Assembly.GetExecutingAssembly());
            logger.Exception("exception #1");
            logger.Exception("exception #2");
            logger.Exception("exception #3");
            logger.Exception("exception #4");

            logger.Error("Error 1");
            logger.Error("Error 2");
            logger.Error("Error 3");

            logger.Warn("Warn 1");
            logger.Warn("Warn 2");
            logger.Warn("Warn 3");


            string id = logger.LogResume();

            Assert.Pass(id);



        }





    }
}