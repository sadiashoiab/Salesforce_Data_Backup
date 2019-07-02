using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;
using Salesforce_Data_Backup;

namespace SFDataDownload.test
{
    public class FunctionsTests
    {
        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async void Http_trigger_should_return_known_string()
        {
            var request = TestFactory.CreateHttpRequest("name", "https://www.nseindia.com/content/historical/DERIVATIVES/2016/AUG/fo05AUG2016bhav.csv.zip");
            var response = (OkObjectResult)await SFdatadownload.Run(request, logger);
            Assert.Equal("File Downloaded successfully, OK", response.Value);
        }

    }
}

