using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// Azure Management dependencies
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;

namespace Company.Function
{
    public static class HttpUpdateDnsARecord
    {
        [FunctionName("HttpUpdateDnsARecord")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get",  Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");



            // Details for subscription and Service Principal account
            // This demo assumes the Service Principal account uses password-based authentication (certificate-based is also possible)
            // See https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/ for details
            var tenantId = System.Environment.GetEnvironmentVariable("TenantId");
            var clientId = System.Environment.GetEnvironmentVariable("AADclientId");
            var secret = System.Environment.GetEnvironmentVariable("AADClientSecret");
            var subscriptionId = System.Environment.GetEnvironmentVariable("SubscriptionId");
            var resourceGroupName = System.Environment.GetEnvironmentVariable("ResourceGroupName");
            var zoneName = System.Environment.GetEnvironmentVariable("DnsZoneName");            



            string ip = req.Query["IP"];
            string host=req.Query["HOST"];

            //get aa from aa.bb.cc
            host=host.Split('.')[0];


             // Build the service credentials and DNS management client
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret);
            var dnsClient = new DnsManagementClient(serviceCreds);
            dnsClient.SubscriptionId = subscriptionId;

             #region Update A Record
            // **********************************************************************************************************
            // Update A Record
            // **********************************************************************************************************

            log.LogInformation("Updating DNS 'A' record set with name '{0}' and IP '{1}'...", host,ip);
            
            try
            {
                var recordSet = dnsClient.RecordSets.Get(resourceGroupName, zoneName, host, RecordType.A);             


                // Add a new record to the local object.  Note that records in a record set must be unique/distinct
                recordSet.ARecords.Clear();
                recordSet.ARecords.Add(new ARecord(ip));
               

                // Update the record set in Azure DNS
                // Note: ETAG check specified, update will be rejected if the record set has changed in the meantime
                recordSet = await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, host, RecordType.A, recordSet, recordSet.Etag);

                
                log.LogInformation("Update successfully");

                return new OkObjectResult("OK");
            }
            catch (System.Exception e)
            {
                log.LogError("Update failed: {0}", e.Message);

                return new ObjectResult("Error");
            }
            #endregion



        }
    }
}
