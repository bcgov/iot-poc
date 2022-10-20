namespace OnvifModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;

    using System.Net;
    using System.ServiceModel;
    // add the using
    using ServiceReference3;//for device client
    using ServiceReference1;//for media client
    using OnvifCamera;
    using System.Timers;
    using ServiceReference5;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Devices.Provisioning.Client;
    using Microsoft.Extensions.Logging;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay;
    using System.Security.Cryptography;

    using Azure.Core;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using ServiceReference2;//for onvif ptz
    using System.ServiceModel.Channels;

    using Vlc.DotNet;
    using System.Reflection;
    using System.Diagnostics;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Specialized;
    using Azure.Storage.Blobs.Models;


    class Program
    {
        static int counter;

        static int count = 0;
        private static CameraManager _cm;
        private static System.Timers.Timer aTimer;
        static List<string> images = new List<string>();
        public static DeviceInformation di;

        public static Dictionary<string, string> SecretDic = new Dictionary<string, string>(); 

        public static string OnvifUser;
        public static string OnvifPass;



        public static int Main(string[] args) => MainAsync(args).Result;

        public static async Task<int> MainAsync(string[] args)
        {   
            // bool flag = true;
            // while(flag)
            // {
            //     try
            //     {
            //         GetSecrets();
            //         await Initialize();
            //     }
            //     catch(Exception exc)
            //     {
            //         Console.WriteLine("Camera Edge Gateway fatal error - " + exc.Message);
            //     }
                

            // }
            // return 0;


            GetSecrets();
            await Initialize();
            return 0;
        }

        // static void Main(string[] args)
        // {
        //     Console.WriteLine("hello world.");

        //     TestVideoOnLinux();


        //     Init().Wait();

        //     // Wait until the app unloads or is cancelled
        //     var cts = new CancellationTokenSource();
        //     AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
        //     Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
        //     WhenCancelled(cts.Token).Wait();
        // }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            //await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", AddCamera, ioTHubModuleClient);
        }

        private static string GetSecret(string secretName)
        {
            var kvUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };

            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential(),options);
            KeyVaultSecret secret = client.GetSecret(secretName);
            return secret.Value;
        }

        private static List<string> GetSecretList(List<string> secretNames)
        {

            

            var kvUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            


            
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };
            List<string> result = new List<string>();
            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential(),options);
            string secretName = "";

            if(secretNames.Count() > 1)
            {
                secretName = secretNames[2];
            }
            else
            {
                secretName = secretNames[0];

            } 

            bool keyExists = false;
            while(!keyExists)
            {
                try
                {
                     KeyVaultSecret secret = client.GetSecret(secretName);
                    result.Add(secret.Properties.Tags["ONVIF-CAMERA-IP"]);
                    result.Add(secret.Properties.Tags["ONVIF-CAMERA-USERNAME"]);
                    result.Add(secret.Value);
                    keyExists = true;

                }
                catch(Exception exc)
                {
                    Console.WriteLine(exc.Message);
                    result.Add("96.1.111.172");
                    result.Add("royal172");
                    result.Add("9btrp2BACgpEja9");
                    keyExists = true;
                }
               

            }


            
            return result;
        }

        private static void GetSecrets_hardcode()
        {

            //Get key vault uri from environment variables
            var kvUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");

            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };

            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential(),options);

            // Console.Write("Input the value of your secret > ");
            // string secretValue = "EDGE_GATEWAY_SCOPE_ID";

            // Console.Write("Creating a secret in " + keyVaultName + " called '" + secretName + "' with the value '" + secretValue + "` ...");

            // client.SetSecret(secretName, secretValue);

            // Console.WriteLine(" done.");

            // Console.WriteLine("Forgetting your secret.");
            // secretValue = "0ne0055EB66";
            // Console.WriteLine("Your secret is '" + secretValue + "'.");

            // Console.WriteLine("Retrieving your secret from " + keyVaultName + ".");


          





            //KeyVaultSecret secret = client.GetSecret(secretName);
            try
            {

            string containerRegistryUsername = "<change to real registry name>";
            string containerRegistryPassword = "<change to the real password>";
            string containerRegistryAddress = "<change to real registry address>";
            string containerRegistryAddressEdgeGatewayModuleId = "<change to real registry address module id>";
            string azureStorageConnectionString = "<change to the real connection string>";
            string edgeGatewayModuleId = "<change to real gateway module id>";
            string cameraModuleId = "<change to real device module id>";
            string edgeGatewayDeviceId = "<change to real device id>";
            string edgeGatewayScopeId = "<change to real scope id>";
            string edgeGatewayGroupSasKey = "<change to real sas key>";
            string cameraGroupSasKey = "<change to real group sas key>";
            string onvifCameraIp = "<change to real camera ip>";
            string onvifCameraUsername = "royal172";
            string onvifCameraPassword = "<change to the real password>";
            string iotDeviceSecurityType = "DPS";
            string iothubDeviceConnectionString = "test";
            string iothubDeviceDpsEndpoint = "test";

            //secret = client.GetSecret("IOTHUB-DEVICE-DPS-ID-SCOPE");
            string iothubDeviceDpsIdScope = "test";

            //secret = client.GetSecret("IOTHUB-DEVICE-DPS-DEVICE-ID");
            string iothubDeviceDpsDeviceId = "test";

            //secret = client.GetSecret("IOTHUB-DEVICE-DPS-DEVICE-KEY");
            string iothubDeviceDpsDeviceKey = "test";

              SecretDic.Add("CONTAINER_REGISTRY_USERNAME", containerRegistryUsername);
            SecretDic.Add("CONTAINER_REGISTRY_PASSWORD", containerRegistryPassword);
            SecretDic.Add("CONTAINER_REGISTRY_ADDRESS", containerRegistryAddress);
            SecretDic.Add("CONTAINER_REGISTRY_ADDRESS_EDGE_GATEWAY_MODULE_ID", containerRegistryAddressEdgeGatewayModuleId);
            SecretDic.Add("AZURE_STORAGE_CONNECTION_STRING", azureStorageConnectionString);

            SecretDic.Add("EDGE_GATEWAY_MODULE_ID", edgeGatewayModuleId);
            SecretDic.Add("CAMERA_MODULE_ID", cameraModuleId);
            SecretDic.Add("EDGE_GATEWAY_DEVICE_ID", edgeGatewayDeviceId);
            SecretDic.Add("EDGE_GATEWAY_SCOPE_ID", edgeGatewayScopeId);
            SecretDic.Add("EDGE_GATEWAY_GROUP_SAS_KEY", edgeGatewayGroupSasKey);

            SecretDic.Add("CAMERA_GROUP_SAS_KEY", cameraGroupSasKey);
            SecretDic.Add("ONVIF_CAMERA_IP", onvifCameraIp);
            SecretDic.Add("ONVIF_CAMERA_USERNAM", onvifCameraUsername);
            SecretDic.Add("ONVIF_CAMERA_PASSWORD", onvifCameraPassword);

            SecretDic.Add("IOTHUB_DEVICE_SECURITY_TYPE", iotDeviceSecurityType);
            SecretDic.Add("IOTHUB_DEVICE_CONNECTION_STRING", iothubDeviceConnectionString);
            SecretDic.Add("IOTHUB_DEVICE_DPS_ENDPOINT", iothubDeviceDpsEndpoint);
            SecretDic.Add("IOTHUB_DEVICE_DPS_DEVICE_ID", iothubDeviceDpsDeviceId);
            SecretDic.Add("IOTHUB_DEVICE_DPS_DEVICE_KEY", iothubDeviceDpsDeviceKey);
            SecretDic.Add("IOTHUB_DEVICE_DPS_ID_SCOPE", iothubDeviceDpsIdScope);


            }
            catch(Exception exc)
            {
                Console.WriteLine(exc.Message);

            }




        }


        private static void GetSecrets()
        {
            //Get key vault uri from environment variables
            var kvUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            kvUri = "https://cameraskeyvault-test.vault.azure.net";

            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };

            var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential(),options);

            try
            {

            KeyVaultSecret secret = client.GetSecret("CONTAINER-REGISTRY-USERNAME");
            string containerRegistryUsername = secret.Value;


            secret = client.GetSecret("CONTAINER-REGISTRY-PASSWORD-1");
            string containerRegistryPassword = secret.Value;

            secret = client.GetSecret("CONTAINER-REGISTRY-ADDRESS");
            string containerRegistryAddress = secret.Value;

            secret = client.GetSecret("CONTAINER-REGISTRY-ADDRESS-EDGE-GATEWAY-MODULE-ID-1");
            string containerRegistryAddressEdgeGatewayModuleId = secret.Value;

            secret = client.GetSecret("AZURE-STORAGE-CONNECTION-STRING");
            string azureStorageConnectionString = secret.Value;

            secret = client.GetSecret("EDGE-GATEWAY-MODULE-ID-1");
            string edgeGatewayModuleId = secret.Value;

            secret = client.GetSecret("CAMERA-MODULE-ID");
            string cameraModuleId = secret.Value;

            secret = client.GetSecret("EDGE-GATEWAY-DEVICE-ID");
            string edgeGatewayDeviceId = secret.Value;

            secret = client.GetSecret("EDGE-GATEWAY-SCOPE-ID");
            string edgeGatewayScopeId = secret.Value;

            secret = client.GetSecret("EDGE-GATEWAY-GROUP-SAS-KEY");
            string edgeGatewayGroupSasKey = secret.Value;

            secret = client.GetSecret("CAMERA-GROUP-SAS-KEY");
            string cameraGroupSasKey = secret.Value;

            secret = client.GetSecret("IOTHUB-DEVICE-SECURITY-TYPE");
            string iotDeviceSecurityType = secret.Value;

            secret = client.GetSecret("IOTHUB-DEVICE-CONNECTION-STRING");
            string iothubDeviceConnectionString = secret.Value;

            secret = client.GetSecret("IOTHUB-DEVICE-DPS-ENDPOINT");
            string iothubDeviceDpsEndpoint = secret.Value;

            secret = client.GetSecret("IOTHUB-DEVICE-DPS-ID-SCOPE");
            string iothubDeviceDpsIdScope = secret.Value;

            secret = client.GetSecret("IOTHUB-DEVICE-DPS-DEVICE-ID");
            string iothubDeviceDpsDeviceId = secret.Value;

            secret = client.GetSecret("IOTHUB-DEVICE-DPS-DEVICE-KEY");
            string iothubDeviceDpsDeviceKey = secret.Value;

            SecretDic.Add("CONTAINER_REGISTRY_USERNAME", containerRegistryUsername);
            SecretDic.Add("CONTAINER_REGISTRY_PASSWORD", containerRegistryPassword);
            SecretDic.Add("CONTAINER_REGISTRY_ADDRESS", containerRegistryAddress);
            SecretDic.Add("CONTAINER_REGISTRY_ADDRESS_EDGE_GATEWAY_MODULE_ID", containerRegistryAddressEdgeGatewayModuleId);
            SecretDic.Add("AZURE_STORAGE_CONNECTION_STRING", azureStorageConnectionString);

            SecretDic.Add("EDGE_GATEWAY_MODULE_ID", edgeGatewayModuleId);
            SecretDic.Add("CAMERA_MODULE_ID", cameraModuleId);
            SecretDic.Add("EDGE_GATEWAY_DEVICE_ID", edgeGatewayDeviceId);
            SecretDic.Add("EDGE_GATEWAY_SCOPE_ID", edgeGatewayScopeId);
            SecretDic.Add("EDGE_GATEWAY_GROUP_SAS_KEY", edgeGatewayGroupSasKey);

            SecretDic.Add("CAMERA_GROUP_SAS_KEY", cameraGroupSasKey);
            SecretDic.Add("IOTHUB_DEVICE_SECURITY_TYPE", iotDeviceSecurityType);
            SecretDic.Add("IOTHUB_DEVICE_CONNECTION_STRING", iothubDeviceConnectionString);
            SecretDic.Add("IOTHUB_DEVICE_DPS_ENDPOINT", iothubDeviceDpsEndpoint);
            SecretDic.Add("IOTHUB_DEVICE_DPS_DEVICE_ID", iothubDeviceDpsDeviceId);
            SecretDic.Add("IOTHUB_DEVICE_DPS_DEVICE_KEY", iothubDeviceDpsDeviceKey);
            SecretDic.Add("IOTHUB_DEVICE_DPS_ID_SCOPE", iothubDeviceDpsIdScope);

            }
            catch(Exception exc)
            {
                Console.WriteLine(exc.Message);

            }
        }


        private static async Task Initialize()
        {

            await ProgramLauncher.CreateGateController();

        }

                // The callback to handle "reboot" command. This method will send a temperature update (of 0°C) over telemetry for both associated components.
        private static async Task<MethodResponse> AddCameraMethod(MethodRequest request, object userContext)
        {
            try
            {
                int delay = JsonConvert.DeserializeObject<int>(request.DataAsJson);

                // _logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");
                await Task.Delay(delay * 1000);

              
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Command input is invalid: {ex.Message}.");
                return new MethodResponse(0);
            }

            return new MethodResponse(0);
        }

       // calculate the device key using the symetric group key
        private static string ComputeDerivedSymmetricKey(string enrollmentKey, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(enrollmentKey))
            {
                return enrollmentKey;
            }

            using var hmac = new HMACSHA256(Convert.FromBase64String(enrollmentKey));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceId)));
        }

        public static async Task PerformOperationsAsync(Microsoft.Azure.Devices.Client.DeviceClient _deviceClient, CancellationToken cancellationToken)
        {
            // This sample follows the following workflow:
            // -> Set handler to receive "targetTemperature" updates, and send the received update over reported property.
            // -> Set handler to receive "getMaxMinReport" command, and send the generated report as command response.
            // -> Periodically send "temperature" over telemetry.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set.

            Console.WriteLine($"Set handler to receive \"targetTemperature\" updates.");
            //await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(TargetTemperatureUpdateCallbackAsync, _deviceClient, cancellationToken);

            Console.WriteLine($"Set handler for \"getMaxMinReport\" command.");
            await _deviceClient.SetMethodHandlerAsync("addCamera", HandleAddCameraCommand, _deviceClient, cancellationToken);

            bool temperatureReset = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (temperatureReset)
                {
                    // // Generate a random value between 5.0°C and 45.0°C for the current temperature reading.
                    // _temperature = Math.Round(_random.NextDouble() * 40.0 + 5.0, 1);
                    // temperatureReset = false;
                }

                // await SendTemperatureAsync();
                await Task.Delay(5 * 1000);
            }
        }


                // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature
        // from the specified time to the current time.
        private static Task<MethodResponse> HandleAddCameraCommand(MethodRequest request, object userContext)
        {
            try
            {
                // DateTime sinceInUtc = JsonConvert.DeserializeObject<DateTime>(request.DataAsJson);
                // var sinceInDateTimeOffset = new DateTimeOffset(sinceInUtc);
                // _logger.LogDebug($"Command: Received - Generating max, min and avg temperature report since " +
                //     $"{sinceInDateTimeOffset.LocalDateTime}.");

                // Dictionary<DateTimeOffset, double> filteredReadings = _temperatureReadingsDateTimeOffset
                //     .Where(i => i.Key > sinceInDateTimeOffset)
                //     .ToDictionary(i => i.Key, i => i.Value);

                // if (filteredReadings != null && filteredReadings.Any())
                // {
                //     var report = new
                //     {
                //         maxTemp = filteredReadings.Values.Max<double>(),
                //         minTemp = filteredReadings.Values.Min<double>(),
                //         avgTemp = filteredReadings.Values.Average(),
                //         startTime = filteredReadings.Keys.Min(),
                //         endTime = filteredReadings.Keys.Max(),
                //     };

                //     _logger.LogDebug($"Command: MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                //         $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, " +
                //         $"startTime={report.startTime.LocalDateTime}, endTime={report.endTime.LocalDateTime}");

                //     byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
                //     return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
                // }

                // _logger.LogDebug($"Command: No relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, cannot generate any report.");
                return Task.FromResult(new MethodResponse(0));
            }
            catch (JsonReaderException exe)
            {
                Console.WriteLine($"Command input is invalid: {exe.Message}.");
                // return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
                return Task.FromResult(new MethodResponse(-1));
            }
        }




        private static async Task<Microsoft.Azure.Devices.Client.DeviceClient> SetupDeviceClientAsync(Parameters parameters, CancellationToken cancellationToken)
        {
            Microsoft.Azure.Devices.Client.DeviceClient deviceClient;
            switch (parameters.DeviceSecurityType.ToLowerInvariant())
            {
                case "dps":
                    Console.WriteLine($"Initializing via DPS");
                    DeviceRegistrationResult dpsRegistrationResult = await ProvisionDeviceAsync(parameters, cancellationToken);
                    var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(dpsRegistrationResult.DeviceId, parameters.DeviceSymmetricKey);
                    deviceClient = InitializeDeviceClient(dpsRegistrationResult.AssignedHub, authMethod);
                    break;

                case "connectionstring":
                    Console.WriteLine($"Initializing via IoT Hub connection string");
                    deviceClient = InitializeDeviceClient(parameters.PrimaryConnectionString);
                    break;

                default:
                    throw new ArgumentException($"Unrecognized value for device provisioning received: {parameters.DeviceSecurityType}." +
                        $" It should be either \"dps\" or \"connectionString\" (case-insensitive).");
            }
            return deviceClient;
        }

                // Provision a device via DPS, by sending the PnP model Id as DPS payload.
        private static async Task<DeviceRegistrationResult> ProvisionDeviceAsync(Parameters parameters, CancellationToken cancellationToken)
        {
            SecurityProvider symmetricKeyProvider = new SecurityProviderSymmetricKey(parameters.DeviceId, parameters.DeviceSymmetricKey, null);
            Microsoft.Azure.Devices.Provisioning.Client.Transport.ProvisioningTransportHandler mqttTransportHandler = new Microsoft.Azure.Devices.Provisioning.Client.Transport.ProvisioningTransportHandlerMqtt();
            ProvisioningDeviceClient pdc = ProvisioningDeviceClient.Create(parameters.DpsEndpoint, parameters.DpsIdScope, symmetricKeyProvider, mqttTransportHandler);

            var pnpPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = PnpConvention.CreateDpsPayload(testModelId),
            };
            return await pdc.RegisterAsync(pnpPayload, cancellationToken);
        }

        static string testModelId = "dtmi:iotCentralForCamerasTest:CameraEdgeGateway_6cz;1";

         // Initialize the device client instance using symmetric key based authentication, over Mqtt protocol (TCP, with fallback over Websocket)
        // and setting the ModelId into ClientOptions. This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static Microsoft.Azure.Devices.Client.DeviceClient InitializeDeviceClient(string hostname, Microsoft.Azure.Devices.Client.IAuthenticationMethod authenticationMethod)
        {
            var options = new Microsoft.Azure.Devices.Client.ClientOptions
            {
                ModelId = testModelId,
            };

            Microsoft.Azure.Devices.Client.DeviceClient deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.Create(hostname, authenticationMethod, Microsoft.Azure.Devices.Client.TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                Console.WriteLine($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }

        // Initialize the device client instance using connection string based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and
        // setting the ModelId into ClientOptions.This method also sets a connection status change callback, that will get triggered any time the device's
        // connection status changes.
        private static Microsoft.Azure.Devices.Client.DeviceClient InitializeDeviceClient(string deviceConnectionString)
        {
            var options = new Microsoft.Azure.Devices.Client.ClientOptions
            {
                ModelId = testModelId,
            };

            Microsoft.Azure.Devices.Client.DeviceClient deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                Console.WriteLine($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It provisions the camera that will be added.
        /// </summary>
        static async Task<MessageResponse> AddCamera(Microsoft.Azure.Devices.Client.Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Edge gateway received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                using (var pipeMessage = new Microsoft.Azure.Devices.Client.Message(messageBytes))
                {
                    foreach (var prop in message.Properties)
                    {
                        pipeMessage.Properties.Add(prop.Key, prop.Value);
                    }
                    await moduleClient.SendEventAsync("output1", pipeMessage);
                
                    Console.WriteLine("Received message sent");
                }
            }
            return MessageResponse.Completed;
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Microsoft.Azure.Devices.Client.Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                using (var pipeMessage = new Microsoft.Azure.Devices.Client.Message(messageBytes))
                {
                    foreach (var prop in message.Properties)
                    {
                        pipeMessage.Properties.Add(prop.Key, prop.Value);
                    }
                    await moduleClient.SendEventAsync("output1", pipeMessage);
                
                    Console.WriteLine("Received message sent");
                }
            }
            return MessageResponse.Completed;
        }


        public static void TestVideoOnLinux()
        {
             string path = Directory.GetCurrentDirectory();
             Console.WriteLine("current path: " + path);


            string strCmdText = @"-D 1 -c -B 10000000 -b 10000000 -4 -Q -F cam_eight -d 28800 -P 9000 -t -u royal172 9btrp2BACgpEja9 rtsp://royal172:9btrp2BACgpEja9@96.1.111.172:554/onvif-media/media.amp?profile=profile_1_h264&sessiontimeout=60&streamtype=unicast";

strCmdText = "openRTSP " + strCmdText;

System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo() { FileName = "/bin/bash", Arguments = strCmdText, }; 

            // System.Diagnostics.ProcessStartInfo process = new System.Diagnostics.ProcessStartInfo();
            // process.UseShellExecute = false;
            // // process.WorkingDirectory = @"/home/linuxVM";
            // process.WorkingDirectory = @"/bin";
            // process.FileName = @"openRTSP";
            // //process.FileName = @"cd";
            // process.Arguments = strCmdText;
            // process.RedirectStandardOutput = true;

            //Console.WriteLine("cd /home/linuxVM");
            try
            {
                Console.WriteLine("start running video capturing...");

                System.Diagnostics.Process proc = new System.Diagnostics.Process() { StartInfo = startInfo, };

                //System.Diagnostics.Process cmd = System.Diagnostics.Process.Start(process);
                bool result = proc.Start();
                Console.WriteLine("process started: " + result);

            }
            catch(Exception exc)
            {
                Console.WriteLine("Error: " + exc.Message);

            }
            

            Console.WriteLine("end bash");

      


           
            string[] filePaths = Directory.GetFiles(path);
            foreach (var fileName in filePaths)
            {
                Console.WriteLine("fileName: " + fileName);
                if (fileName.Contains(".mp4"))
                {
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                    Console.WriteLine("start upload video");
                    Console.WriteLine("end upload video");

                }
            }

            



        }

        public static async Task TestVideo()
        {
            string address = "http://96.1.111.172/onvif/device_service";
            var user = "royal172";
            var pass = "<change to the real password>";
            Uri uri = new Uri(address);
            MediaClientController _mediaClientController = MediaClientController.CreateMediaClientController(uri, user, pass, true, new OnvifCamera.CustomUsernameToken.TimeOnDevice(System.DateTime.UtcNow));

            ProfileInfo[] profiles = await _mediaClientController.GetCameraProfilesAsync();

            // string streamUri = await _mediaClientController.GetStreamUriAsync(profiles[0].Token);


            // JPEGStream jpegSource = new JPEGStream(streamUri);

            // jpegSource.Start();

            // Thread.Sleep(5000);

            // jpegSource.Stop();
        }

        public static async Task<List<string>> CaptureImage()
        {
            Console.WriteLine("start to create camera");
            CameraManager cm = await CreateCamera(GatewayController.CameraId);
            Console.WriteLine("end of create camera");
            DeviceInformation deviceInformation = await IsCameraOn();
            Console.WriteLine("Camera is on:" + deviceInformation.IsCameraOn);
            List<string> images = Snapshot(cm.Host, cm.User, cm.Pass);
            Thread.Sleep(30000);
            Program.OnvifUser = cm.User;
            Program.OnvifPass = cm.Pass;
            return images;
        }

        private static async Task CopyBlobAsync()
        {
            try
            {
                string connectionString = "<change to the real connection string>";
                string blobContainerName = "images";
                BlobContainerClient container = new BlobContainerClient(connectionString, blobContainerName);

                // Get the name of the first blob in the container to use as the source.
                var blobs = container.GetBlobs().OrderByDescending(x => x.Properties.LastModified).ToList();
                var updatedBlobs = new List<BlobItem>();
                for(var i = 0; i < 7; i++)
                {
                    updatedBlobs.Add(blobs[i]);
                }

                foreach (var blob in updatedBlobs)
                {
                    string blobName = blob.Name;

                    // Create a BlobClient representing the source blob to copy.
                    BlobClient sourceBlob = container.GetBlobClient(blobName);

                    // Ensure that the source blob exists.
                    if (await sourceBlob.ExistsAsync())
                    {
                        string destFileName = "";
                        // if(blobName.Contains("/")) continue;

                        // Lease the source blob for the copy operation 
                        // to prevent another client from modifying it.
                        BlobLeaseClient lease = sourceBlob.GetBlobLeaseClient();

                        // Specifying -1 for the lease interval creates an infinite lease.
                        await lease.AcquireAsync(TimeSpan.FromSeconds(-1));

                        // Get the source blob's properties and display the lease state.
                        BlobProperties sourceProperties = await sourceBlob.GetPropertiesAsync();
                        Console.WriteLine($"Lease state: {sourceProperties.LeaseState}");
                        
                        destFileName = @"../$web/" + blobName;


                        //Get a BlobClient representing the destination blob with a unique name.
                        BlobClient destBlob =
                           container.GetBlobClient(destFileName);

                        // Start the copy operation.
                        await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);



                        // Get the destination blob's properties and display the copy status.
                        BlobProperties destProperties = await destBlob.GetPropertiesAsync();

                        Console.WriteLine($"Copy status: {destProperties.CopyStatus}");
                        Console.WriteLine($"Copy progress: {destProperties.CopyProgress}");
                        Console.WriteLine($"Completion time: {destProperties.CopyCompletedOn}");
                        Console.WriteLine($"Total bytes: {destProperties.ContentLength}");

                        // Update the source blob's properties.
                        sourceProperties = await sourceBlob.GetPropertiesAsync();

                        if (sourceProperties.LeaseState == LeaseState.Leased)
                        {
                            // Break the lease on the source blob.
                            await lease.BreakAsync();

                            // Update the source blob's properties to check the lease state.
                            sourceProperties = await sourceBlob.GetPropertiesAsync();
                            Console.WriteLine($"Lease state: {sourceProperties.LeaseState}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
     
            }


        }

        public static string RecordVideo()
        {
            Console.WriteLine("start to record video");
            string videoPath = RecordVideoFromUri(di.StreamUri);
            Console.WriteLine("end to record video");
            return videoPath;
        }

        public static async Task<DeviceInformation> TestGetInformation()
        {
            Console.WriteLine("start to create camera");
            await CreateCamera(GatewayController.CameraId);
            Console.WriteLine("end of create camera");
            DeviceInformation deviceInformation = await IsCameraOn();
            return deviceInformation;
        }


        /// <summary>
        /// Connect to Onvif Camera
        /// </summary>
        /// <returns></returns>
        public static async Task<CameraManager> CreateCamera(string cameraId = null)
        {
            List<string> secretNames = new List<string>();
            if(cameraId != null)
            {
                secretNames.Add("ONVIF-CAMERA-IP-" + cameraId.ToString());
                secretNames.Add("ONVIF-CAMERA-USERNAME-" + cameraId.ToString());
                secretNames.Add("ONVIF-CAMERA-PASSWORD-" + cameraId.ToString());

            }
            else
            {
                // secretNames.Add("ONVIF-CAMERA-IP");
                // secretNames.Add("ONVIF-CAMERA-USERNAME");
                secretNames.Add("ONVIF-CAMERA-PASSWORD-0");           
            }
            List<string> secretValues = GetSecretList(secretNames);
            

            var host = @"http://" + secretValues[0] + @"/onvif/device_service";
            var user = secretValues[1];
            var pass = secretValues[2];


            var uri = new Uri(host);

            _cm = await CameraManager.CreateCameraManager(uri, user, pass);
            _cm.Host = secretValues[0];
            _cm.User = user;
            _cm.Pass = pass;
            return _cm;

        }

        /// <summary>
        /// Fetch camera status to show if the cam is online/offline
        /// </summary>
        /// <returns></returns>
        public static async Task<DeviceInformation> IsCameraOn()
        {

            di = await _cm.GetCameraInfo();
            var _mediaController = _cm.CreateMediaController();
            var _profiles = await _mediaController.GetCameraProfilesAsync();
            var _streamUri = await _mediaController.GetStreamUriAsync(_profiles[0].Token);//h264
            di.StreamUri = _streamUri;

            if (di.DHCPEnabled && di.DNSIPAddress.Count != 0)
            {
                di.IsCameraOn = true;
            }

            return di;
        }

        public static PtzController CreatePtzController(string host, string user, string pass)
        {
            PtzController pc = new PtzController();
            pc.Initialise(host, user, pass);
            return pc;

        }

        public static List<string> GetPtzPresets(PtzController pc)
        {
            List<string> result = new List<string>();
            result = pc.GetPresets(pc.CurrentProfileToken);
            return result;
        }



        /// <summary>
        /// Capture an image from the current preset when requested by the user
        /// </summary>
        public static List<string> Snapshot(string host, string user, string pass)
        {
            List<Stream> streams = new List<Stream>();
            List<string> result = new List<string>();
            MediaClientController mc = _cm.CreateMediaController();
            ProfileInfo[] profiles = mc.GetCameraProfilesAsync().Result;

            ProfileInfo profile = profiles[0];
            //foreach (var profile in profiles)
            {
                var timestamp = new DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds();
                PtzController pc = CreatePtzController(host, user, pass);
                List<string> presets = GetPtzPresets(pc);
                // PreSets = presets;
                foreach(var preset in presets)
                {
                    pc.SetView(profile.Token, preset);

                    byte[] snapshot = mc.GetSnapshotAsync(profile.Token).Result;

                    var ms = new MemoryStream(snapshot);
                    streams.Add(ms);
                    //For iot central, there is  no way to upload files to underlining iot hub
                    //UploadToIotHub(ms, imagePath, _deviceClient, _serviceClient);

                    string imagePath = "images" + "\\" + di.Hostname + "\\" + preset + "\\" + di.Hostname + "-" + preset + "-" + timestamp + ".jpg"; //@"C:\test\" + profile.Token + ".jpg";

                    //Upload to image container for storing every image
                    UploadToWeb(ms, imagePath);
                    result.Add(imagePath);
                    images = result;

                }

            }
            return result;



        }


        //Record video every 30 secs
        public static string RecordVideoFromUri(string streamUri)
        {
            return Record(streamUri);         
        }


        static string Record(string streamUri)
        {
            string videoPath = null;
            bool isOpenRtspInstalled = false;
            string aptGetInstall = "update && apt-get install -y livemedia-utils";
            ExecuteCommand("/usr/bin/apt-get", aptGetInstall, ref isOpenRtspInstalled);

            if(isOpenRtspInstalled)
            {
                Console.WriteLine("openRTSP installed");
            }
            else
            {
                Console.WriteLine("openRTSP is not installed");

            }
            string command = "-D 1 -B 10000000 -b 10000000 -4 -Q -F cam_eight -d 10 -P 900 -t -u " + Program.OnvifUser + " " + Program.OnvifPass + " " + streamUri;// rtsp://royal172:9btrp2BACgpEja9@96.1.111.172:554/onvif-media/media.amp?profile=profile_1_h264&sessiontimeout=60&streamtype=unicast";

            ExecuteCommand("/usr/bin/openRTSP", command, ref isOpenRtspInstalled);

            videoPath = SendVideoToBlobStorage(di.Hostname);
            return videoPath;

        }

 

        static string SendVideoToBlobStorage(string cameraName)
        {
            string videoPath = null;

           Thread.Sleep(15000);

           string path = Directory.GetCurrentDirectory();
           string[] filePaths = Directory.GetFiles(path);



            //get latest file
            string pattern = "*.mp4";
            var dirInfo = new DirectoryInfo(path);
            var fileName = (from f in dirInfo.GetFiles(pattern) orderby f.LastWriteTime descending select f).First();

           //foreach(var fileName in filePaths)
           {
               var file = new FileStream(fileName.FullName, FileMode.Open);


               var ms = new MemoryStream();

 

               // If using .NET 4 or later:

               file.CopyTo(ms);

 

               videoPath = "videos" + "\\" + cameraName + "\\" + "test" + ".mp4"; //@"C:\test\" + profile.Token + ".jpg";

 

               //Upload to image container for storing every video

               UploadToWeb(ms, videoPath);

               file.Close();

           }
           return videoPath;

            

        }

        public static void ExecuteCommand(string execFileName, string command, ref bool isOpenRtspInstalled)
        {

            Process proc = new System.Diagnostics.Process ();

            proc.StartInfo.FileName = execFileName;

            proc.StartInfo.Arguments = command;

            proc.StartInfo.UseShellExecute = false;

            proc.StartInfo.RedirectStandardOutput = true;

            try
            {
                proc.Start ();

            }
            catch(Exception exc)
            {
                Console.WriteLine(exc.Message);
                throw;

            }
            
            Thread.Sleep(15000);
            if(execFileName.Contains("apt"))
            {
                string path = "/bin";
            string[] filePaths = Directory.GetFiles(path);
            foreach(var fileName in filePaths)
            {
               
                if(fileName.ToLower().Contains("openrtsp"))
                {
                    isOpenRtspInstalled = true;
                    break;
                }
            }

            path = "/usr/bin";
            string[] filePaths_1 = Directory.GetFiles(path);
            foreach(var fileName in filePaths_1)
            {
                
                if(fileName.ToLower().Contains("openrtsp"))
                {
                    isOpenRtspInstalled = true;
                    break;
                }
            }


            //DirectorySearch("/");

            }
            

 

            while (!proc.StandardOutput.EndOfStream) {

                Console.WriteLine (proc.StandardOutput.ReadLine ());

            }

        }




        /// <summary>
        /// Capture images at set interval automatically (interval to be set in IoT Central)
        /// </summary>
        /// <param name="interval"></param>
        public static void SnapshotTimed(int interval)
        {
            // Create a timer and set a two second interval.
            aTimer = new System.Timers.Timer();
            aTimer.Interval = interval;

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnSnapshotTimedEvent;

            // Have the timer fire repeated events (true is the default)
            aTimer.AutoReset = true;

            // Start the timer
            aTimer.Enabled = true;

            Console.WriteLine("Press the Enter key to exit the program at any time... ");
            Console.ReadLine();


        }


        private static void OnSnapshotTimedEvent(object source, ElapsedEventArgs e)
        {

            List<string> result = new List<string>();
            MediaClientController mc = _cm.CreateMediaController();
            ProfileInfo[] profiles = mc.GetCameraProfilesAsync().Result;
            foreach (var profile in profiles)
            {
                string imagePath = profile.Token + ".jpg"; //@"C:\test\" + profile.Token + ".jpg";

                byte[] snapshot = mc.GetSnapshotAsync(profile.Token).Result;

                using (var ms = new MemoryStream(snapshot))
                {
                    // using (var fs = new FileStream(imagePath, FileMode.Create))
                    // {
                    //     ms.WriteTo(fs);
                    // }
                    UploadToBlob(ms, imagePath);
                }

                //Upload(imagePath);
                Console.WriteLine("The snapshot was captured at {0}", e.SignalTime);
                result.Add(imagePath);
                images = result;


            }

        }

        public static void GetProfiles()
        {

            MediaClientController mc = _cm.CreateMediaController();
            ProfileInfo[] profiles = mc.GetCameraProfilesAsync().Result;
            foreach (var profile in profiles)
            {
                //if(profile.Token.Contains("jpeg"))
                {
                    //ImagingClientController ic = cm.CreateImagingControllerAsync(profile.Token).Result;
                    //ImagingSettingCapabilities isc = ic.GetImagingCapabilities();

                    string imagePath = @"C:\test\" + profile.Token + ".jpg";
                    byte[] snapshot = mc.GetSnapshotAsync(profile.Token).Result;

                    using (var ms = new MemoryStream(snapshot))
                    {
                        using (var fs = new FileStream(imagePath, FileMode.Create))
                        {
                            ms.WriteTo(fs);
                        }
                    }

                }


            }

        }


        public static void UploadToBlob(string file)
        {
            var filePath = file;// @"C:\Users\BRWANG\projects\IoT\IMB-sent\Read_VIF\Read_VIF\bin\Debug\net5.0\seismicFile.txt";
            var connectionString = "<change to the real connection string>";
            
            //get blob connection string from Linux enviornment variabals
            connectionString = Program.SecretDic["AZURE_STORAGE_CONNECTION_STRING"];//Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            var fileName = file;
            Azure.Storage.Blobs.BlobClient blobClient = new Azure.Storage.Blobs.BlobClient(
            connectionString: connectionString,
            blobContainerName: "images",
            blobName: fileName
            );

            blobClient.Upload(filePath, true);

        }

        public static void UploadToWeb(Stream fileStreamSource, string file)
        {
            var filePath = file;
            var connectionString = Program.SecretDic["AZURE_STORAGE_CONNECTION_STRING"];//Environment.GetEnvironmentVariable("blobConnectionString");

            var fileName = file;
            Azure.Storage.Blobs.BlobClient blobClient = new Azure.Storage.Blobs.BlobClient(
            connectionString: connectionString,
            blobContainerName: file.Contains("mp4")? "$web": "images-src",
            blobName: fileName
            );

            fileStreamSource.Seek(0, SeekOrigin.Begin);

            try
            {
                blobClient.Upload(fileStreamSource, true);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            


        }

        public static async void UploadToIotHub(Stream fileStreamSource, string fileName, Microsoft.Azure.Devices.Client.DeviceClient _deviceClient,
            Microsoft.Azure.Devices.ServiceClient _serviceClient)
        {
            Stream tempStream = fileStreamSource;

            //for (var i = 0; i < fileStreamSource.Count; i++)
            {
                var fileUploadSasUriRequest = new FileUploadSasUriRequest
                {
                    BlobName = fileName
                };


                try
                {

                    FileUploadSasUriResponse sasUri = await _deviceClient.GetFileUploadSasUriAsync(fileUploadSasUriRequest);
                    Uri uploadUri = sasUri.GetBlobUri();

                    var blockBlobClient = new Azure.Storage.Blobs.Specialized.BlockBlobClient(uploadUri);
                    blockBlobClient.Upload(tempStream, new Azure.Storage.Blobs.Models.BlobUploadOptions());

                    var successfulFileUploadCompletionNotification = new FileUploadCompletionNotification
                    {
                        // Mandatory. Must be the same value as the correlation id returned in the sas uri response
                        CorrelationId = sasUri.CorrelationId,

                        // Mandatory. Will be present when service client receives this file upload notification
                        IsSuccess = true,

                        // Optional, user defined status code. Will be present when service client receives this file upload notification
                        StatusCode = 200,

                        // Optional, user-defined status description. Will be present when service client receives this file upload notification
                        StatusDescription = "Success"
                    };

                    await _deviceClient.CompleteFileUploadAsync(successfulFileUploadCompletionNotification);

                    //get file upload notifications from iot hub using service SDK
                    FileNotificationReceiver<FileNotification> notificationReceiver = _serviceClient.GetFileNotificationReceiver();
                    var notification = await notificationReceiver.ReceiveAsync(new CancellationToken(false));

                    var cameraInfo = new CameraInfo
                    {
                        DHCPEnabled = di.DHCPEnabled,
                        DNSIPAddress = di.DNSIPAddress,
                        Firmware = di.Firmware,
                        HardwareId = di.HardwareId,
                        Hostname = di.Hostname,
                        MACAddresses = di.MACAddresses,
                        Manufacturer = di.Manufacturer,
                        Model = di.Model,
                        SerialNumber = di.SerialNumber

                    };

                    var telemetry = new IotHubToDeviceNotification
                    {
                        DeviceId = notification.DeviceId,
                        BlobUri = notification.BlobUri,
                        BlobName = notification.BlobName,
                        LastUpdatedTime = notification.LastUpdatedTime,
                        BlobSizeInBytes = notification.BlobSizeInBytes,
                        EnqueuedTimeUtc = notification.EnqueuedTimeUtc,
                        CameraInfo = cameraInfo
                    };




                    //send back the telemetry to Iot hub
                    string dataBuffer = JsonConvert.SerializeObject(telemetry);
                    var eventMessage = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(dataBuffer));

                    await _deviceClient.SendEventAsync(eventMessage);

                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }


            }




        }


        public static void UploadToBlob(Stream fileStream, string fileName)
        {

            var connectionString = "";

            connectionString = Program.SecretDic["AZURE_STORAGE_CONNECTION_STRING"];//Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            
            Azure.Storage.Blobs.BlobClient blobClient = new Azure.Storage.Blobs.BlobClient(
            connectionString: connectionString,
            blobContainerName: "images",
            blobName: fileName
            );

            blobClient.Upload(fileStream, true);

        }

        //Read configuration
        static string CosmosDatabaseId = "CameraDB";
        static string containerId = "CameraDataContainer";

        static Database cosmosDatabase = null;

        public static async Task SaveToCosmosDb(DeviceInformation di)
        {
            //https://docs.microsoft.com/en-us/azure/cosmos-db/sql/sql-api-dotnet-v3sdk-samples
            //https://github.com/Azure/azure-cosmos-dotnet-v3/tree/master/Microsoft.Azure.Cosmos.Samples/Usage

            try
            {
                string endpoint = configuration["EndPointUrl"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new ArgumentNullException("Please specify a valid endpoint in the appSettings.json");
                }

                string authKey = configuration["AuthorizationKey"];
                
                if (string.IsNullOrEmpty(authKey) || string.Equals(authKey, "Super secret key"))
                {
                    throw new ArgumentException("Please specify a valid AuthorizationKey in the appSettings.json");
                }

                //Read the Cosmos endpointUrl and authorisationKeys from configuration
                //These values are available from the Azure Management Portal on the Cosmos Account Blade under "Keys"
                //NB > Keep these values in a safe & secure location. Together they provide Administrative access to your Cosmos account
                using (CosmosClient client = new CosmosClient(endpoint, authKey))
                {
                    await Program.RunDemoAsync(client);
                }
            }
            catch (CosmosException cre)
            {
                Console.WriteLine(cre.ToString());
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                
            }


        }

        private static async Task RunDemoAsync(CosmosClient client)
        {
            cosmosDatabase = await client.CreateDatabaseIfNotExistsAsync(CosmosDatabaseId);
            Container container = await Program.GetOrCreateContainerAsync(cosmosDatabase, containerId);
            await Program.DeleteItems(container);
            await Program.CreateItems(container, di);
        }
        // </RunDemoAsync>

        // <ItemFeed>
        private static async Task ItemFeed(Container container)
        {
            List<Family> families = new List<Family>();

            // SQL
            using (FeedIterator<Family> setIterator = container.GetItemQueryIterator<Family>(requestOptions: new QueryRequestOptions { MaxItemCount = 1 }))
            {
                while (setIterator.HasMoreResults)
                {
                    int count = 0;
                    foreach (Family item in await setIterator.ReadNextAsync())
                    {
                        Assert("Should only return 1 result at a time.", count <= 1);
                        families.Add(item);
                    }
                }
            }

            Assert("Expected two families", families.ToList().Count == 2);
        }
        // </ItemFeed>

        // <ItemStreamFeed>
        private static async Task ItemStreamFeed(Container container)
        {
            int totalCount = 0;

            // SQL
            using (FeedIterator setIterator = container.GetItemQueryStreamIterator())
            {
                while (setIterator.HasMoreResults)
                {
                    int count = 0;
                    using (ResponseMessage response = await setIterator.ReadNextAsync())
                    {
                        if (response.Diagnostics != null)
                        {
                            Console.WriteLine($"ItemStreamFeed Diagnostics: {response.Diagnostics.ToString()}");
                        }

                        response.EnsureSuccessStatusCode();
                        count++;
                        using (StreamReader sr = new StreamReader(response.Content))
                        using (JsonTextReader jtr = new JsonTextReader(sr))
                        {
                            JsonSerializer jsonSerializer = new JsonSerializer();
                            dynamic array = jsonSerializer.Deserialize<dynamic>(jtr);
                            totalCount += array.Documents.Count;
                        }
                    }

                }
            }

            Assert("Expected two families", totalCount == 2);
        }
        // </ItemStreamFeed>

        // <QueryItemsInPartitionAsStreams>
        private static async Task QueryItemsInPartitionAsStreams(Container container)
        {
            // SQL
            using (FeedIterator setIterator = container.GetItemQueryStreamIterator(
                "SELECT F.id, F.LastName, F.IsRegistered FROM Families F",
                requestOptions: new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey("Anderson"),
                    MaxConcurrency = 1,
                    MaxItemCount = 1
                }))
            {
                int count = 0;
                while (setIterator.HasMoreResults)
                {
                    using (ResponseMessage response = await setIterator.ReadNextAsync())
                    {
                        Assert("Response failed", response.IsSuccessStatusCode);
                        count++;
                        using (StreamReader sr = new StreamReader(response.Content))
                        using (JsonTextReader jtr = new JsonTextReader(sr))
                        {
                            JsonSerializer jsonSerializer = new JsonSerializer();
                            dynamic items = jsonSerializer.Deserialize<dynamic>(jtr).Documents;
                            Assert("Expected one family", items.Count == 1);
                            dynamic item = items[0];
                            Assert($"Expected LastName: Anderson Actual: {item.LastName}", string.Equals("Anderson", item.LastName.ToString(), StringComparison.InvariantCulture));
                        }
                    }
                }

                Assert("Expected 1 family", count == 1);
            }
        }
        // </QueryItemsInPartitionAsStreams>

        // <QueryWithSqlParameters>
        private static async Task QueryWithSqlParameters(Container container)
        {
            // Query using two properties within each item. WHERE Id == "" AND Address.City == ""
            // notice here how we are doing an equality comparison on the string value of City

            QueryDefinition query = new QueryDefinition("SELECT * FROM Families f WHERE f.id = @id AND f.Address.City = @city")
                .WithParameter("@id", "AndersonFamily")
                .WithParameter("@city", "Seattle");

            List<Family> results = new List<Family>();
            using (FeedIterator<Family> resultSetIterator = container.GetItemQueryIterator<Family>(
                query,
                requestOptions: new QueryRequestOptions()
                {
                    PartitionKey = new PartitionKey("Anderson")
                }))
            {
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<Family> response = await resultSetIterator.ReadNextAsync();
                    results.AddRange(response);
                    if (response.Diagnostics != null)
                    {
                        Console.WriteLine($"\nQueryWithSqlParameters Diagnostics: {response.Diagnostics.ToString()}");
                    }
                }

                Assert("Expected only 1 family", results.Count == 1);
            }
        }
        // </QueryWithSqlParameters>

        // <QueryWithContinuationTokens>
        private static async Task QueryWithContinuationTokens(Container container)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c");
            string continuation = null;

            List<Family> results = new List<Family>();
            using (FeedIterator<Family> resultSetIterator = container.GetItemQueryIterator<Family>(
                query,
                requestOptions: new QueryRequestOptions()
                {
                    MaxItemCount = 1
                }))
            {
                // Execute query and get 1 item in the results. Then, get a continuation token to resume later
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<Family> response = await resultSetIterator.ReadNextAsync();

                    results.AddRange(response);
                    if (response.Diagnostics != null)
                    {
                        Console.WriteLine($"\nQueryWithContinuationTokens Diagnostics: {response.Diagnostics.ToString()}");
                    }

                    // Get continuation token once we've gotten > 0 results. 
                    if (response.Count > 0)
                    {
                        continuation = response.ContinuationToken;
                        break;
                    }
                }
            }

            // Check if query has already been fully drained
            if (continuation == null)
            {
                return;
            }

            // Resume query using continuation token
            using (FeedIterator<Family> resultSetIterator = container.GetItemQueryIterator<Family>(
                    query,
                    requestOptions: new QueryRequestOptions()
                    {
                        MaxItemCount = -1
                    },
                    continuationToken: continuation))
            {
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<Family> response = await resultSetIterator.ReadNextAsync();

                    results.AddRange(response);
                    if (response.Diagnostics != null)
                    {
                        Console.WriteLine($"\nQueryWithContinuationTokens Diagnostics: {response.Diagnostics.ToString()}");
                    }
                }
                Assert("Expected 2 families", results.Count == 2);
            }
        }
        // </QueryWithContinuationTokens>

        // <QueryPartitionedContainerInParallelAsync>
        private static async Task QueryPartitionedContainerInParallelAsync(Container container)
        {
            List<OnvifImage> familiesSerial = new List<OnvifImage>();
            string queryText = "SELECT * FROM c";

            // 0 maximum parallel tasks, effectively serial execution
            QueryRequestOptions options = new QueryRequestOptions() { MaxBufferedItemCount = 100 };
            options.MaxConcurrency = 0;
            using (FeedIterator<OnvifImage> query = container.GetItemQueryIterator<OnvifImage>(
                queryText,
                requestOptions: options))
            {
                while (query.HasMoreResults)
                {
                    foreach (OnvifImage onvifImage in await query.ReadNextAsync())
                    {
                        familiesSerial.Add(onvifImage);

                        await container.DeleteItemAsync<OnvifImage>(onvifImage.Id, new PartitionKey(onvifImage.PartitionKey));
                    }
                }
            }

            // Assert("Parallel Query expected two families", familiesSerial.ToList().Count == 2);

            // // 1 maximum parallel tasks, 1 dedicated asynchronous task to continuously make REST calls
            // List<OnvifImage> familiesParallel1 = new List<OnvifImage>();

            // options.MaxConcurrency = 1;
            // using (FeedIterator<OnvifImage> query = container.GetItemQueryIterator<OnvifImage>(
            //     queryText,
            //     requestOptions: options))
            // {
            //     while (query.HasMoreResults)
            //     {
            //         foreach (OnvifImage family in await query.ReadNextAsync())
            //         {
            //             familiesParallel1.Add(family);
            //         }
            //     }
            // }

            // Assert("Parallel Query expected two families", familiesParallel1.ToList().Count == 2);


            // // 10 maximum parallel tasks, a maximum of 10 dedicated asynchronous tasks to continuously make REST calls
            // List<OnvifImage> familiesParallel10 = new List<OnvifImage>();

            // options.MaxConcurrency = 10;
            // using (FeedIterator<OnvifImage> query = container.GetItemQueryIterator<OnvifImage>(
            //     queryText,
            //     requestOptions: options))
            // {
            //     while (query.HasMoreResults)
            //     {
            //         foreach (OnvifImage family in await query.ReadNextAsync())
            //         {
            //             familiesParallel10.Add(family);
            //         }
            //     }
            // }

            // Assert("Parallel Query expected two families", familiesParallel10.ToList().Count == 2);
            // AssertSequenceEqual("Parallel query returns result out of order compared to serial execution", familiesSerial, familiesParallel10);
        }
        // </QueryPartitionedContainerInParallelAsync>


        private static async Task DeleteItems(Container container)
        {

            await QueryPartitionedContainerInParallelAsync(container);
            //await container.DeleteItemAsync<OnvifImage>(onvifImage, new PartitionKey(onvifImage.PartitionKey));

        }


        /// <summary>
        /// Creates the items used in this Sample
        /// </summary>
        /// <param name="container">The selfLink property for the CosmosContainer where items will be created.</param>
        /// <returns>None</returns>
        // <CreateItems>
        private static async Task CreateItems(Container container, DeviceInformation di)
        {
            string url = @"https://blobstorageformediatest.blob.core.windows.net/$web/";
            foreach (var image in images)
            {
                count++;
                var fileName = image.Replace(@"\", @"/");        
                string s1 = fileName.Substring(0, fileName.LastIndexOf("/"));
                string preset = s1.Substring(s1.LastIndexOf("/") + 1);
                OnvifImage onvifImage = new OnvifImage
                {
                    DeviceId = di.Hostname,
                    Id = di.Hostname + "-" + System.DateTime.Now.ToString("u"),


                    //DeviceId = di.Hostname + "-" + System.DateTime.Now.ToString("u"),
                    //Id = count.ToString(),
                    TimeStamp = GetJavascriptTimestamp(System.DateTime.Now.ToString("u")),
                    CameraName = di.Hostname,
                    CameraAddress = di.DNSIPAddress[0],
                    SnapshotDate = System.DateTime.Now,
                    ImagePath = url + fileName,
                    CameraModel = di.Model,
                    CameraSerialNumber = di.SerialNumber,
                    CameraManufacture = di.Manufacturer,
                    PreSet = preset
                };
                try
                {
                    await container.UpsertItemAsync<OnvifImage>(onvifImage, new PartitionKey(onvifImage.PartitionKey));

                }
                catch(Exception exc)
                {
                    Console.WriteLine(exc.Message);
                }
                
                
            }

        }
        // </CreateItems>


        static double GetJavascriptTimestamp(string datetime)
        {
            System.DateTime UnixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            System.DateTime dt = System.DateTime.Parse(datetime); ;
            TimeSpan duration = dt.Subtract(UnixEpoch);
            return duration.TotalMilliseconds;

        }

        /// <summary>
        /// Get a DocuemntContainer by id, or create a new one if one with the id provided doesn't exist.
        /// </summary>
        /// <param name="id">The id of the CosmosContainer to search for, or create.</param>
        /// <returns>The matched, or created, CosmosContainer object</returns>
        // <GetOrCreateContainerAsync>
        private static async Task<Container> GetOrCreateContainerAsync(Database database, string containerId)
        {
            ContainerProperties containerProperties = new ContainerProperties(id: containerId, partitionKeyPath: "/deviceId");

            return await database.CreateContainerIfNotExistsAsync(
                containerProperties: containerProperties,
                throughput: 400);
        }
        // </GetOrCreateContainerAsync>

        private static void Assert(string message, bool condition)
        {
            if (!condition)
            {
                throw new ApplicationException(message);
            }
        }

        private static void AssertSequenceEqual(string message, List<Family> list1, List<Family> list2)
        {
            if (!string.Join(",", list1.Select(family => family.Id).ToArray()).Equals(
                string.Join(",", list1.Select(family => family.Id).ToArray())))
            {
                throw new ApplicationException(message);
            }
        }

        internal sealed class Parent
        {
            public string FamilyName { get; set; }
            public string FirstName { get; set; }
        }

        internal sealed class Child
        {
            public string FamilyName { get; set; }
            public string FirstName { get; set; }
            public string Gender { get; set; }
            public int Grade { get; set; }
            public Pet[] Pets { get; set; }
        }

        internal sealed class Pet
        {
            public string GivenName { get; set; }
        }

        internal sealed class Address
        {
            public string State { get; set; }
            public string County { get; set; }
            public string City { get; set; }
        }

        internal sealed class Family
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            public string LastName { get; set; }

            public Parent[] Parents { get; set; }

            public Child[] Children { get; set; }

            public Address Address { get; set; }

            public bool IsRegistered { get; set; }

            public System.DateTime RegistrationDate { get; set; }

            public string PartitionKey => this.LastName;

            public static string PartitionKeyPath => "/LastName";
        }

        internal sealed class OnvifImage
        {
            [JsonProperty(PropertyName = "deviceId")]
            public string DeviceId { get; set; }

            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            public double TimeStamp {get; set;}

            public string CameraName { get; set; }

            public string CameraAddress { get; set; }

            public System.DateTime SnapshotDate { get; set; }

            public string PartitionKey => this.DeviceId;

            public string ImagePath { get; set; }


            public string CameraModel { get; set; }

            public string CameraSerialNumber { get; set; }

            public string CameraManufacture { get; set; }

            public string PreSet {get; set;}
        }

    }

    class IotHubToDeviceNotification
    {

        public string DeviceId { get; set; }
        public string BlobUri { get; set; }
        public string BlobName { get; set; }
        public DateTimeOffset? LastUpdatedTime { get; set; }
        public long BlobSizeInBytes { get; set; }
        public System.DateTime EnqueuedTimeUtc { get; set; }

        public CameraInfo CameraInfo;

    }

    class CameraInfo
    {
        public bool DHCPEnabled { get; set; }
        public IReadOnlyList<string> DNSIPAddress { get; set; }
        public string Firmware { get; set; }
        public string HardwareId { get; set; }
        public string Hostname { get; set; }
        public IReadOnlyList<string> MACAddresses { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
    }

    public class PtzController
    {

        public string CurrentProfileToken;

        private enum Direction { None, Up, Down, Left, Right };

        ServiceReference1.MediaClient mediaClient;
        PTZClient ptzClient;
        Profile profile;
        ServiceReference2.PTZSpeed velocity;

        public static ServiceReference2.PTZSpeed CurrentSpeed;




        PTZVector vector;
        PTZConfigurationOptions options;
        bool relative = false;
        bool initialised = false;
        System.Timers.Timer timer;
        Direction direction;
        float panDistance;
        float tiltDistance;

        public string ErrorMessage { get; private set; }

        public bool Initialised { get { return initialised; } }

        public int PanIncrements { get; set; } = 20;

        public int TiltIncrements { get; set; } = 20;

        public double TimerInterval { get; set; } = 1500;

        public PtzController(bool relative = false)
        {
            this.relative = relative;
        }

        public bool Initialise(string cameraAddress, string userName, string password)
        {
            bool result = false;

            try
            {
                var messageElement = new TextMessageEncodingBindingElement()
                {
                    MessageVersion = MessageVersion.CreateVersion(
                      EnvelopeVersion.Soap12, AddressingVersion.None)
                };
                HttpTransportBindingElement httpBinding = new HttpTransportBindingElement()
                {
                    AuthenticationScheme = AuthenticationSchemes.Digest
                };
                CustomBinding bind = new CustomBinding(messageElement, httpBinding);
                //mediaClient = new MediaClient(bind,
                //  new EndpointAddress($"http://{cameraAddress}/onvif/Media"));
                mediaClient = new MediaClient(bind,
                  new EndpointAddress($"http://{cameraAddress}/onvif/device_service"));
                //mediaClient.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                //  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                mediaClient.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                mediaClient.ClientCredentials.HttpDigest.ClientCredential.Password = password;
                //ptzClient = new PTZClient(bind,
                //  new EndpointAddress($"http://{cameraAddress}/onvif/PTZ"));
                ptzClient = new PTZClient(bind,
                  new EndpointAddress($"http://{cameraAddress}/onvif/device_service"));
                //ptzClient.ClientCredentials.HttpDigest.AllowedImpersonationLevel =
                //  System.Security.Principal.TokenImpersonationLevel.Impersonation;
                ptzClient.ClientCredentials.HttpDigest.ClientCredential.UserName = userName;
                ptzClient.ClientCredentials.HttpDigest.ClientCredential.Password = password;

                var profToken = mediaClient.GetProfilesAsync().Result.Profiles[1].token;
                profile = mediaClient.GetProfileAsync(profToken).Result;

                CurrentProfileToken = profToken;

                var configToken = ptzClient.GetConfigurationsAsync().Result.PTZConfiguration[0].token;

                options = ptzClient.GetConfigurationOptionsAsync(configToken).Result;

                velocity = new ServiceReference2.PTZSpeed()
                {
                    PanTilt = new ServiceReference2.Vector2D()
                    {
                        x = 0,
                        y = 0,
                        space = options.Spaces.ContinuousPanTiltVelocitySpace[0].URI,
                    },
                    Zoom = new ServiceReference2.Vector1D()
                    {
                        x = 0,
                        space = options.Spaces.ContinuousZoomVelocitySpace[0].URI,
                    }
                };
                if (relative)
                {
                    timer = new System.Timers.Timer(TimerInterval);
                    timer.Elapsed += Timer_Elapsed;
                    velocity.PanTilt.space = options.Spaces.RelativePanTiltTranslationSpace[0].URI;
                    panDistance = (options.Spaces.RelativePanTiltTranslationSpace[0].XRange.Max -
                      options.Spaces.RelativePanTiltTranslationSpace[0].XRange.Min) / PanIncrements;
                    tiltDistance = (options.Spaces.RelativePanTiltTranslationSpace[0].YRange.Max -
                      options.Spaces.RelativePanTiltTranslationSpace[0].YRange.Min) / TiltIncrements;
                }

                vector = new PTZVector()
                {
                    PanTilt = new ServiceReference2.Vector2D()
                    {
                        x = 0,
                        y = 0,
                        space = options.Spaces.RelativePanTiltTranslationSpace[0].URI
                    }
                };

                ErrorMessage = "";
                result = initialised = true;
                CurrentSpeed = velocity;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return result;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Move();
        }

        public void PanLeft()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Left;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Min;
                    velocity.PanTilt.y = 0;
                    ptzClient.ContinuousMoveAsync(profile.token, velocity, "PT10S");
                }
            }
        }

        public List<string> GetPresets(string profileToken)
        {
            List<string> result = new List<string>();
            GetPresetsResponse gpr_test = ptzClient.GetPresetsAsync(profileToken).Result;
            foreach(var preset in gpr_test.Preset)
            {
                result.Add(preset.Name);

            }

            return result;

               
            
        }

        public void SetView(string profileToken, string preset)
        {
                int presetIndex = 0;
                preset = preset.ToLower();
                if(preset.Contains("home"))
                {
                    presetIndex = 0;
                }
                if(preset.Contains("west"))
                {
                    presetIndex = 1;
                }
                if(preset.Contains("east"))
                {
                    presetIndex = 2;
                }
                if(preset.Contains("north"))
                {
                    presetIndex = 3;
                }
                if(preset.Contains("south"))
                {
                    presetIndex = 6;
                }
 
                GetPresetsResponse gpr = ptzClient.GetPresetsAsync(profileToken).Result;
 
                
                ptzClient.GotoPresetAsync(CurrentProfileToken, gpr.Preset[presetIndex].token, null);

            
        }

        public void PanRight()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Right;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max;
                    velocity.PanTilt.y = 0;
                    ptzClient.ContinuousMoveAsync(profile.token, velocity, "PT10S");
                }
            }
        }

        public void TiltUp()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Up;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max;
                    ptzClient.ContinuousMoveAsync(profile.token, velocity, "PT10S");
                }
            }
        }

        public void TiltDown()
        {
            if (initialised)
            {
                if (relative)
                {
                    direction = Direction.Down;
                    Move();
                }
                else
                {
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Min;
                    ptzClient.ContinuousMoveAsync(profile.token, velocity, "PT10S");
                }
            }
        }

        public void Stop()
        {
            if (initialised)
            {
                if (relative)
                    timer.Enabled = false;
                direction = Direction.None;
                ptzClient.StopAsync(profile.token, true, true);
            }
        }

        private void Move()
        {
            bool move = true;

            switch (direction)
            {
                case Direction.Up:
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max;
                    vector.PanTilt.x = 0;
                    vector.PanTilt.y = tiltDistance;
                    break;

                case Direction.Down:
                    velocity.PanTilt.x = 0;
                    velocity.PanTilt.y = options.Spaces.ContinuousPanTiltVelocitySpace[0].YRange.Max;
                    vector.PanTilt.x = 0;
                    vector.PanTilt.y = -tiltDistance;
                    break;

                case Direction.Left:
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max;
                    velocity.PanTilt.y = 0;
                    vector.PanTilt.x = -panDistance;
                    vector.PanTilt.y = 0;
                    break;

                case Direction.Right:
                    velocity.PanTilt.x = options.Spaces.ContinuousPanTiltVelocitySpace[0].XRange.Max;
                    velocity.PanTilt.y = 0;
                    vector.PanTilt.x = panDistance;
                    vector.PanTilt.y = 0;
                    break;

                case Direction.None:
                default:
                    move = false;
                    break;
            }
            if (move)
            {
                ptzClient.RelativeMoveAsync(profile.token, vector, velocity);
            }
            timer.Enabled = true;
        }
    }

}
    