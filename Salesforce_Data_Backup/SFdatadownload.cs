using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.ServiceBus;

namespace Salesforce_Data_Backup
{
    public static class SFdatadownload
    {
        [FunctionName("SFdatadownload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            //gg
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            var clientId = GetCredentialsFromKeyVault("SFStoragePSW").GetAwaiter().GetResult();
            if (clientId != "null")
            {
                HttpStatusCode result;
                // string remoteFilePath = "https://www.nseindia.com/content/historical/DERIVATIVES/2016/AUG/fo05AUG2016bhav.csv.zip";
                string remoteFilePath = name;
                result = HttpStatusCode.OK;
                var account = new CloudStorageAccount(new StorageCredentials("sadiastorage", clientId), true);
                var blobClient = account.CreateCloudBlobClient();
                var blobContainer = blobClient.GetContainerReference("salesforcedownload");
                await blobContainer.CreateIfNotExistsAsync();
                var newBlockBlob = blobContainer.GetBlockBlobReference("salesforcebackup");
                await newBlockBlob.StartCopyAsync(new Uri(remoteFilePath));

                // Monitor the copy state...
                while (true)
                {
                    await newBlockBlob.FetchAttributesAsync();
                    //Console.WriteLine("Copy state: {0}", newBlockBlob.CopyState);
                    if (newBlockBlob.CopyState.Status != CopyStatus.Pending)
                    {
                        break;
                    }
                }

                return name != null
                    ? (ActionResult)new OkObjectResult($"File Downloaded successfully, {result}")
                    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");

            }
            else
            {
                return name != null
                   ? (ActionResult)new OkObjectResult($"File Downloaded successfully, {HttpStatusCode.OK}")
                   : new BadRequestObjectResult("Please pass a name on the query string or in the request body");


            }
        }



        public static async Task<string> GetCredentialsFromKeyVault(string keySecret)
        {
            try
            {
                var kv = Environment.GetEnvironmentVariable("keyvault");
                if (kv != null)
                {
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    var secret = await keyVaultClient.GetSecretAsync(kv + keySecret).ConfigureAwait(false);
                    return secret.Value.ToString();
                }
                else
                {
                    kv = "null";
                    return kv.ToString();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }
        }
    }
}
