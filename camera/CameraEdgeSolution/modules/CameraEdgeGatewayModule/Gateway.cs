// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Net.Http;
using System.Net.Http.Headers;

namespace OnvifModule
{
    public class Gateway
    {
        // DTDL interface used: https://github.com/Azure/iot-plugandplay-models/blob/main/dtmi/com/example/temperaturecontroller-2.json
        // The TemperatureController model contains 2 Thermostat components that implement different versions of Thermostat models.
        // Both Thermostat models are identical in definition but this is done to allow IoT Central to handle
        // TemperatureController model correctly.
        //private const string ModelId = "dtmi:com:example:TemperatureController;2";
        private static string ModelId = Program.SecretDic["CAMERA_MODULE_ID"];//Environment.GetEnvironmentVariable("CAMERA_MODULE_ID");



        static Parameters globalParameters = null;


        private static ILogger s_logger;

        public static void DeleteSecretFromKeyVault(string cameraId)
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
            KeyVaultSecret secret = client.GetSecret("CONTAINER-REGISTRY-USERNAME");
            string secretName = "ONVIF-CAMERA-PASSWORD-" + cameraId;
            client.StartDeleteSecret(secretName);

        }

        public static void AddSecretToKeyVault(CameraInfoReq request)
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
            

            string secretName = "ONVIF-CAMERA-ID-" + request.CameraId;
            
            
            GatewayController.CameraId = request.CameraId;

            try
            {
                secretName = "ONVIF-CAMERA-PASSWORD-" + request.CameraId;
                               
                client.SetSecret(secretName, request.Password);
                Thread.Sleep(5000);
                
                KeyVaultSecret passwordSecret = client.GetSecret(secretName);

                SecretProperties secretProperties = passwordSecret.Properties;
                secretProperties.Tags["ONVIF-CAMERA-USERNAME"] = request.Username;
                secretProperties.Tags["ONVIF-CAMERA-IP"] = request.IpAddress;
                secretProperties.Tags["ONVIF-CAMERA-NAME"] = request.CameraName;
                secretProperties.Tags["ONVIF-CAMERA-ID"] = request.CameraId;

                client.UpdateSecretPropertiesAsync(secretProperties);

            }
            catch(Azure.RequestFailedException exc)
            {
                Console.WriteLine(exc.Message);

            }

        }


        public static async void UnprovisonDevice(string id)
        {
            //call iot central api to delete the device
            string apiToten = "<change to real api token>";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("<change to real iot central address>");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Authorization", apiToten);
            HttpResponseMessage response = await client.DeleteAsync($"api/devices/{id}?api-version=1.0");

            Console.WriteLine($"delete the device.");
        }
        public static async Task ProvisonDevice(string deviceId)
        {
            // Parse application parameters
            Parameters parameters = new Parameters();
            
            parameters.DeviceSecurityType = "DPS";
            parameters.DpsEndpoint = "global.azure-devices-provisioning.net";
            parameters.DpsIdScope = Program.SecretDic["EDGE_GATEWAY_SCOPE_ID"];//Environment.GetEnvironmentVariable("EDGE_GATEWAY_SCOPE_ID");//"0ne0053A0D0";
            parameters.DeviceId = deviceId;
            //parameters.DeviceSymmetricKey = "PS411QoB3blWbx2x/B4DeBtAttT7JjrHYZ01fDZiyQ0=";
            //iot edge group sas key
            //string groupSASKey = "QG5G2/S1WesNpeRZ3DXv/B0vxF5uKDWOEXUPHGN3spwtLuHRe/fLRgZ6ZKysBhW4RKM22eFQmTMsqbUCw69Juw==";
            //iot device group sas key
            string groupSASKey = Program.SecretDic["CAMERA_GROUP_SAS_KEY"];//Environment.GetEnvironmentVariable("CAMERA_GROUP_SAS_KEY");//"4VuBDh23nR1O5oE/IfjghIjMOz7v0Ie6sX2du4FkrdzaDUke+mYGTV4WoDyy13cr7CjjdGVls0p4P03SWZhnag==";

            //// calculate device symmetric key from group symmetric key
            parameters.DeviceSymmetricKey = ComputeDerivedSymmetricKey(groupSASKey, parameters.DeviceId);

            //parameters.DeviceSymmetricKey = "g79kSbf9wQthe9Tj7b51WsTfVBm2mB77m+8neDnqNWo=";
            globalParameters = parameters;


            s_logger = InitializeConsoleDebugLogger();
            if (!parameters.Validate(s_logger))
            {
                throw new ArgumentException("Required parameters are not set. Please recheck required variables by using \"--help\"");
            }

            var runningTime = parameters.ApplicationRunningTime != null
                ? TimeSpan.FromSeconds((double)parameters.ApplicationRunningTime)
                : Timeout.InfiniteTimeSpan;

            s_logger.LogInformation("Press Control+C to quit the sample.");
            using var cts = new CancellationTokenSource(runningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                s_logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            s_logger.LogDebug($"Set up the device client.");


            using DeviceClient deviceClient = await SetupDeviceClientAsync(parameters, cts.Token);
            var sample = new GatewayController(deviceClient, s_logger);
            
            Thread thread1 = new Thread(ThreadWork.DoWork);
            thread1.Start();

            //await sample.PerformOperationsAsync(cts.Token);

            await deviceClient.SetMethodHandlerAsync("GetInformation", sample.HandleGetInformationCommand, "Thermostat2", new CancellationToken(false));


            // PerformOperationsAsync is designed to run until cancellation has been explicitly requested, either through
            // cancellation token expiration or by Console.CancelKeyPress.
            // As a result, by the time the control reaches the call to close the device client, the cancellation token source would
            // have already had cancellation requested.
            // Hence, if you want to pass a cancellation token to any subsequent calls, a new token needs to be generated.
            // For device client APIs, you can also call them without a cancellation token, which will set a default
            // cancellation timeout of 4 minutes: https://github.com/Azure/azure-iot-sdk-csharp/blob/64f6e9f24371bc40ab3ec7a8b8accbfb537f0fe1/iothub/device/src/InternalClient.cs#L1922
            //await deviceClient.CloseAsync();
        }

        internal class ThreadWork
        {
            public static async void DoWork()
            {
               CancellationToken ct = new CancellationToken(false);
               using DeviceClient deviceClient = await SetupDeviceClientAsync(globalParameters, ct);
           
               var sample = new GatewayController(deviceClient, s_logger);
                await sample.PerformDeviceOperationsAsync(ct);
              
            }
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

        private static ILogger InitializeConsoleDebugLogger()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.TimestampFormat = "[MM/dd/yyyy HH:mm:ss]";
                });
            });

            return loggerFactory.CreateLogger<GatewayController>();
        }



        private static async Task<DeviceClient> SetupDeviceClientAsync(Parameters parameters, CancellationToken cancellationToken)
        {
            DeviceClient deviceClient;
            switch (parameters.DeviceSecurityType.ToLowerInvariant())
            {
                case "dps":
                    s_logger.LogDebug($"Initializing via DPS");
                    DeviceRegistrationResult dpsRegistrationResult = await ProvisionDeviceAsync(parameters, cancellationToken);
                    var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(dpsRegistrationResult.DeviceId, parameters.DeviceSymmetricKey);
                    deviceClient = InitializeDeviceClient(dpsRegistrationResult.AssignedHub, authMethod);
                    break;

                case "connectionstring":
                    s_logger.LogDebug($"Initializing via IoT Hub connection string");
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
            ProvisioningTransportHandler mqttTransportHandler = new ProvisioningTransportHandlerMqtt();
            ProvisioningDeviceClient pdc = ProvisioningDeviceClient.Create(parameters.DpsEndpoint, parameters.DpsIdScope, symmetricKeyProvider, mqttTransportHandler);

            var pnpPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = PnpConvention.CreateDpsPayload(ModelId),
            };
            return await pdc.RegisterAsync(pnpPayload, cancellationToken);
        }

        // Initialize the device client instance using connection string based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and
        // setting the ModelId into ClientOptions.This method also sets a connection status change callback, that will get triggered any time the device's
        // connection status changes.
        private static DeviceClient InitializeDeviceClient(string deviceConnectionString)
        {
            var options = new Microsoft.Azure.Devices.Client.ClientOptions
            {
                ModelId = ModelId,
            };

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }

        // Initialize the device client instance using symmetric key based authentication, over Mqtt protocol (TCP, with fallback over Websocket)
        // and setting the ModelId into ClientOptions. This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static DeviceClient InitializeDeviceClient(string hostname, IAuthenticationMethod authenticationMethod)
        {
            var options = new Microsoft.Azure.Devices.Client.ClientOptions
            {
                ModelId = ModelId,
            };

            DeviceClient deviceClient = DeviceClient.Create(hostname, authenticationMethod, TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }
    }
}
