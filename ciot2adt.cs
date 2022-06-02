using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System;
using Azure;
using System.Net.Http;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace iot.adt
{
    public class ciot2adt
    {
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient httpClient = new HttpClient();
        private static HttpClient client = new HttpClient();
        
        [FunctionName("ciot2adt")]
        public async Task Run([IoTHubTrigger("messages/*", Connection = "conn_iot4rade")]EventData message, ILogger log)
        {
            if (adtInstanceUrl == null) log.LogError("Application setting \"ADT_SERVICE_URL\" not set");

            log.LogInformation($"C# IoT Hub trigger function processed a message: {Encoding.UTF8.GetString(message.Body.Array)}");
            try
            {
                // Authenticate with Digital Twins
                var cred = new DefaultAzureCredential();
                var client = new DigitalTwinsClient(new Uri(adtInstanceUrl), cred);
                log.LogInformation($"ADT service client connection created.");
            
                if (message != null && message.Body != null)
                {
                    JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body.Array));
                    // string deviceId = (string)deviceMessage["systemProperties"]["iothub-connection-device-id"];
                    var name = deviceMessage["name"].Value<String>() + "01";
                    var quantity = deviceMessage["quantity"].Value<int>();

                    log.LogInformation($"name is:{name}");

                    // <Update_twin_with_device_quantity>    
                    var updateTwinData = new JsonPatchDocument();
                    updateTwinData.AppendReplace("/count", quantity);
                    await client.UpdateDigitalTwinAsync(name, updateTwinData);
                    // </Update_twin_with_device_quantity>
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in ingest function: {ex}");
            }
        }
    }
}