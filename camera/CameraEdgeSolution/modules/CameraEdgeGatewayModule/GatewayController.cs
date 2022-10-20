// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnpHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using OnvifCamera;


namespace OnvifModule
{


        public class CameraInfoReq
        {
            public string CameraId { get; set; }
            public string CameraName { get; set; }
            public string IpAddress { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }

        }

    internal enum StatusCode
    {
        Completed = 200,
        InProgress = 202,
        NotFound = 404,
        BadRequest = 400
    }

    
    public class DevicePropery
    {
        public string DeviceId;
        public string Location;

    }

    public class GatewayController
    {

        public static string CameraId;

        public static string PresetName = null;
        private const string Thermostat1 = "thermostat1";
        private const string Thermostat2 = "thermostat2";
        private const string SerialNumber = "SR-123456";

        private const string Location = "Test Location";

        private DevicePropery DeviceProperty = new DevicePropery{DeviceId = "1000", Location = "My Test Location" };

        private static readonly Random s_random = new Random();

        private readonly DeviceClient _deviceClient;
        private readonly ILogger _logger;

        // Dictionary to hold the temperature updates sent over each "Thermostat" component.
        // NOTE: Memory constrained devices should leverage storage capabilities of an external service to store this
        // information and perform computation.
        // See https://docs.microsoft.com/en-us/azure/event-grid/compare-messaging-services for more details.
        private readonly Dictionary<string, Dictionary<DateTimeOffset, double>> _temperatureReadingsDateTimeOffset =
            new Dictionary<string, Dictionary<DateTimeOffset, double>>();

        // A dictionary to hold all desired property change callbacks that this pnp device should be able to handle.
        // The key for this dictionary is the componentName.
        private readonly IDictionary<string, DesiredPropertyUpdateCallback> _desiredPropertyUpdateCallbacks =
            new Dictionary<string, DesiredPropertyUpdateCallback>();

        // Dictionary to hold the current temperature for each "Thermostat" component.
        private readonly Dictionary<string, double> _temperature = new Dictionary<string, double>();

        // Dictionary to hold the max temperature since last reboot, for each "Thermostat" component.
        private readonly Dictionary<string, double> _maxTemp = new Dictionary<string, double>();

        public GatewayController(DeviceClient deviceClient, ILogger logger)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException($"{nameof(deviceClient)} cannot be null.");
            _logger = logger ?? LoggerFactory.Create(builer => builer.AddConsole()).CreateLogger<GatewayController>();
        }

        public async Task PerformOperationsAsync(CancellationToken cancellationToken)
        {
            // This sample follows the following workflow:
            // -> Set handler to receive "reboot" command - root interface.
            // -> Set handler to receive "getMaxMinReport" command - on "Thermostat" components.
            // -> Set handler to receive "targetTemperature" property updates from service - on "Thermostat" components.
            // -> Update device information on "deviceInformation" component.
            // -> Send initial device info - "workingSet" over telemetry, "serialNumber" over reported property update - root interface.
            // -> Periodically send "temperature" over telemetry - on "Thermostat" components.
            // -> Send "maxTempSinceLastReboot" over property update, when a new max temperature is set - on "Thermostat" components.


            _logger.LogDebug("Set handler for 'reboot' command.");
            await _deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, _deviceClient, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("AddCamera", HandleAddCameraCommand, Thermostat2, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("DeleteCamera", HandleDeleteCameraCommand, Thermostat2, cancellationToken);

            await _deviceClient.SetMethodHandlerAsync("GetInformation", HandleGetInformationCommand, Thermostat2, cancellationToken);


            await _deviceClient.SetMethodHandlerAsync("Snapshot", HandleSnapshotCommand, Thermostat2, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("RecordVideo", HandleRecordVideoCommand, Thermostat2, cancellationToken);

            _logger.LogDebug("Set handler to receive 'targetTemperature' updates.");
  
            while (!cancellationToken.IsCancellationRequested)
            {

                List<string> images = await Program.CaptureImage();
                Program.RecordVideo();
                DeviceInformation di = Program.di;
                CameraTelemetry  ct = PopulateCameraTelemetry(di, images);
                await SendCameraTelemetryAsync(ct, cancellationToken);            
                await Task.Delay(3600 * 1000);
            }
        }

        public async Task PerformGatewayOperationsAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Set handler for 'reboot' command.");
            await _deviceClient.SetMethodHandlerAsync("reboot", HandleRebootCommandAsync, _deviceClient, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("AddCamera", HandleAddCameraCommand, Thermostat2, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("DeleteCamera", HandleDeleteCameraCommand, Thermostat2, cancellationToken);
            while(true)
            {}

        }

        public async Task PerformDeviceOperationsAsync(CancellationToken cancellationToken)
        {
           
            await _deviceClient.SetMethodHandlerAsync("GetInformation", HandleGetInformationCommand, Thermostat2, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("Snapshot", HandleSnapshotCommand, Thermostat2, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("RecordVideo", HandleRecordVideoCommand, Thermostat2, cancellationToken);
            await _deviceClient.SetMethodHandlerAsync("UpdateProperty", HandleUpdatePropertyCommand, Thermostat2, cancellationToken);
            await SendDevicePropertyAsync(cancellationToken, null);
            while (!cancellationToken.IsCancellationRequested)
            {
                List<string> blobs = await Program.CaptureImage();
                string videoPath = Program.RecordVideo();
                blobs.Add(videoPath);
                DeviceInformation di = Program.di;
                CameraTelemetry  ct = PopulateCameraTelemetry(di, blobs);
                await SendCameraTelemetryAsync(ct, cancellationToken);
                await Task.Delay(3600 * 1000);
            }
        }



        private CameraTelemetry PopulateCameraTelemetry(DeviceInformation deviceInformation, List<string> blobs)
        {
            CameraTelemetry ct = new CameraTelemetry();

            CameraMetadata cameraMetadata = new CameraMetadata();
            ct.CameraMetadata = cameraMetadata;
            cameraMetadata.DeviceID = deviceInformation.Hostname;
            cameraMetadata.CameraMake = deviceInformation.Manufacturer;
            cameraMetadata.IPAddress = deviceInformation.DNSIPAddress[0];
            cameraMetadata.MAC = deviceInformation.MACAddresses[0];
            MotiCameraInfo motiCameraInfo = new MotiCameraInfo();
            motiCameraInfo.CameraID = deviceInformation.HardwareId;
            motiCameraInfo.CameraName = deviceInformation.Hostname;
            motiCameraInfo.ActivationDate = DateTime.Now.ToLongDateString();
            List<MotiCameraInfo> motiCameraInfos = new List<MotiCameraInfo>();
            motiCameraInfos.Add(motiCameraInfo);
            cameraMetadata.Cameras = motiCameraInfos;
            

            List<CameraData> cameraDatas = new List<CameraData>();
            foreach(string blob in blobs)
            {

                CameraData cameraData = new CameraData();
                cameraData.DeviceID = deviceInformation.HardwareId;
                cameraData.CameraID = deviceInformation.Hostname;
                cameraData.BlobUri = blob;
                cameraData.LastUpdatedTime = DateTime.Now.ToString();
                cameraData.BlobName = blob.Contains("mp4")? "$web": "images-src";
                cameraDatas.Add(cameraData);
            }
            ct.CameraDatas = cameraDatas;
            
            return ct;

        }

        // The callback to handle "reboot" command. This method will send a temperature update (of 0°C) over telemetry for both associated components.
        private async Task<MethodResponse> HandleRebootCommandAsync(MethodRequest request, object userContext)
        {
            try
            {
                int delay = JsonConvert.DeserializeObject<int>(request.DataAsJson);

                _logger.LogDebug($"Command: Received - Rebooting thermostat (resetting temperature reading to 0°C after {delay} seconds).");
                await Task.Delay(delay * 1000);

                _logger.LogDebug("\tRebooting...");

                _temperature[Thermostat1] = _maxTemp[Thermostat1] = 0;
                _temperature[Thermostat2] = _maxTemp[Thermostat2] = 0;

                _temperatureReadingsDateTimeOffset.Clear();

                _logger.LogDebug("\tRestored.");
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return new MethodResponse((int)StatusCode.BadRequest);
            }

            return new MethodResponse((int)StatusCode.Completed);
        }

        // The callback to handle "getMaxMinReport" command. This method will returns the max, min and average temperature from the
        // specified time to the current time.
        private Task<MethodResponse> HandleMaxMinReportCommand(MethodRequest request, object userContext)
        {
            try
            {
                string componentName = (string)userContext;
                DateTime sinceInUtc = JsonConvert.DeserializeObject<DateTime>(request.DataAsJson);
                var sinceInDateTimeOffset = new DateTimeOffset(sinceInUtc);

                if (_temperatureReadingsDateTimeOffset.ContainsKey(componentName))
                {
                    _logger.LogDebug($"Command: Received - component=\"{componentName}\", generating max, min and avg temperature " +
                        $"report since {sinceInDateTimeOffset.LocalDateTime}.");

                    Dictionary<DateTimeOffset, double> allReadings = _temperatureReadingsDateTimeOffset[componentName];
                    Dictionary<DateTimeOffset, double> filteredReadings = allReadings.Where(i => i.Key > sinceInDateTimeOffset)
                        .ToDictionary(i => i.Key, i => i.Value);

                    if (filteredReadings != null && filteredReadings.Any())
                    {
                        var report = new
                        {
                            maxTemp = filteredReadings.Values.Max<double>(),
                            minTemp = filteredReadings.Values.Min<double>(),
                            avgTemp = filteredReadings.Values.Average(),
                            startTime = filteredReadings.Keys.Min(),
                            endTime = filteredReadings.Keys.Max(),
                        };

                        _logger.LogDebug($"Command: component=\"{componentName}\", MaxMinReport since {sinceInDateTimeOffset.LocalDateTime}:" +
                            $" maxTemp={report.maxTemp}, minTemp={report.minTemp}, avgTemp={report.avgTemp}, startTime={report.startTime.LocalDateTime}, " +
                            $"endTime={report.endTime.LocalDateTime}");

                        byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
                        return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));
                    }

                    _logger.LogDebug($"Command: component=\"{componentName}\", no relevant readings found since {sinceInDateTimeOffset.LocalDateTime}, " +
                        $"cannot generate any report.");
                    return Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
                }

                _logger.LogDebug($"Command: component=\"{componentName}\", no temperature readings sent yet, cannot generate any report.");
                return Task.FromResult(new MethodResponse((int)StatusCode.NotFound));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
        }

        private Task<MethodResponse> HandleDeleteCameraCommand(MethodRequest request, object userContext)
        {
            try
            {
                string componentName = (string)userContext;
                string cameraId = JsonConvert.DeserializeObject<string>(request.DataAsJson);

                _logger.LogDebug($"Command: component=\"{componentName}\", no temperature readings sent yet, cannot generate any report.");
                Gateway.UnprovisonDevice(cameraId);
                Gateway.DeleteSecretFromKeyVault(cameraId);
                byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cameraId));
                return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));

                //return Task.FromResult(new MethodResponse((int)StatusCode.Completed));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
        }

        private async Task<MethodResponse> HandleAddCameraCommand(MethodRequest request, object userContext)
        {
            try
            {
                string componentName = (string)userContext;
                CameraInfoReq deviceInfo = JsonConvert.DeserializeObject<CameraInfoReq>(request.DataAsJson);

                _logger.LogDebug($"Command: component=\"{componentName}\", no temperature readings sent yet, cannot generate any report.");
                Console.WriteLine("command completed");

                Gateway.AddSecretToKeyVault(deviceInfo);
 
                await Gateway.ProvisonDevice(deviceInfo.CameraId);


                CameraId = deviceInfo.CameraId;



                byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceInfo));
                return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed)).Result;

                //return Task.FromResult(new MethodResponse((int)StatusCode.Completed));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest)).Result;
            }
        }

        public  Task<MethodResponse> HandleGetInformationCommand(MethodRequest request, object userContext)
        {
            try
            {
                var deviceInformation = Program.TestGetInformation();

                byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceInformation.Result));
                return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));

            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
        }

        internal class ThreadWork
        {
            public static async void Snapshot()
            {
              await Program.CaptureImage();
              
            }
            public static void RecordVideo()
            {
              Program.RecordVideo();
              
            }
        }



        private Task<MethodResponse> HandleSnapshotCommand(MethodRequest request, object userContext)
        {
            try
            {
                PresetName = JsonConvert.DeserializeObject<string>(request.DataAsJson);

                 //snapshot for single camera, new thread to take picture
                Thread threadSnapshot = new Thread(ThreadWork.Snapshot);
                threadSnapshot.Start();
                

                string result = "Snapshot taken";


                byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
                return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));

                //return Task.FromResult(new MethodResponse((int)StatusCode.Completed));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
        }

        private Task<MethodResponse> HandleRecordVideoCommand(MethodRequest request, object userContext)
        {
            try
            {
                PresetName = JsonConvert.DeserializeObject<string>(request.DataAsJson);
                Thread threadSnapshot = new Thread(ThreadWork.RecordVideo);
                threadSnapshot.Start();
                

                string result = "video recording done";


                byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
                return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed));

                //return Task.FromResult(new MethodResponse((int)StatusCode.Completed));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest));
            }
        }



        private async Task<MethodResponse> HandleUpdatePropertyCommand(MethodRequest request, object userContext)
        {
            try
            {
                DevicePropery devicePropery = JsonConvert.DeserializeObject<DevicePropery>(request.DataAsJson);


                string result = "success";
                await SendDevicePropertyAsync(new CancellationToken(false), devicePropery);


                byte[] responsePayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
                return Task.FromResult(new MethodResponse(responsePayload, (int)StatusCode.Completed)).Result;

                //return Task.FromResult(new MethodResponse((int)StatusCode.Completed));
            }
            catch (JsonReaderException ex)
            {
                _logger.LogDebug($"Command input is invalid: {ex.Message}.");
                return Task.FromResult(new MethodResponse((int)StatusCode.BadRequest)).Result;
            }
        }




        private Task SetDesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
        {
            bool callbackNotInvoked = true;

            foreach (KeyValuePair<string, object> propertyUpdate in desiredProperties)
            {
                string componentName = propertyUpdate.Key;
                if (_desiredPropertyUpdateCallbacks.ContainsKey(componentName))
                {
                    _desiredPropertyUpdateCallbacks[componentName]?.Invoke(desiredProperties, componentName);
                    callbackNotInvoked = false;
                }
            }

            if (callbackNotInvoked)
            {
                _logger.LogDebug($"Property: Received a property update that is not implemented by any associated component.");
            }

            return Task.CompletedTask;
        }

        // The desired property update callback, which receives the target temperature as a desired property update,
        // and updates the current temperature value over telemetry and property update.
        private async Task TargetTemperatureUpdateCallbackAsync(TwinCollection desiredProperties, object userContext)
        {
            const string propertyName = "targetTemperature";
            string componentName = (string)userContext;

            bool targetTempUpdateReceived = PnpConvention.TryGetPropertyFromTwin(
                desiredProperties,
                propertyName,
                out double targetTemperature,
                componentName);
            if (!targetTempUpdateReceived)
            {
                _logger.LogDebug($"Property: Update - component=\"{componentName}\", received an update which is not associated with a valid property.\n{desiredProperties.ToJson()}");
                return;
            }

            _logger.LogDebug($"Property: Received - component=\"{componentName}\", {{ \"{propertyName}\": {targetTemperature}°C }}.");

            TwinCollection pendingReportedProperty = PnpConvention.CreateComponentWritablePropertyResponse(
                componentName,
                propertyName,
                targetTemperature,
                (int)StatusCode.InProgress,
                desiredProperties.Version);

            await _deviceClient.UpdateReportedPropertiesAsync(pendingReportedProperty);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {targetTemperature} }} in °C is {StatusCode.InProgress}.");

            // Update Temperature in 2 steps
            double step = (targetTemperature - _temperature[componentName]) / 2d;
            for (int i = 1; i <= 2; i++)
            {
                _temperature[componentName] = Math.Round(_temperature[componentName] + step, 1);
                await Task.Delay(6 * 1000);
            }

            TwinCollection completedReportedProperty = PnpConvention.CreateComponentWritablePropertyResponse(
                componentName,
                propertyName,
                _temperature[componentName],
                (int)StatusCode.Completed,
                desiredProperties.Version,
                "Successfully updated target temperature");

            await _deviceClient.UpdateReportedPropertiesAsync(completedReportedProperty);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{\"{propertyName}\": {_temperature[componentName]} }} in °C is {StatusCode.Completed}");
        }

        // Report the property updates on "deviceInformation" component.
        private async Task UpdateDeviceInformationAsync(CancellationToken cancellationToken)
        {
            const string componentName = "deviceInformation";

            TwinCollection deviceInfoTc = PnpConvention.CreateComponentPropertyPatch(
                componentName,
                new Dictionary<string, object>
                {
                    { "manufacturer", "element15" },
                    { "model", "ModelIDxcdvmk" },
                    { "swVersion", "1.0.0" },
                    { "osName", "Windows 10" },
                    { "processorArchitecture", "64-bit" },
                    { "processorManufacturer", "Intel" },
                    { "totalStorage", 256 },
                    { "totalMemory", 1024 },
                });

            await _deviceClient.UpdateReportedPropertiesAsync(deviceInfoTc, cancellationToken);
            _logger.LogDebug($"Property: Update - component = '{componentName}', properties update is complete.");
        }

        // Send working set of device memory over telemetry.
        private void SendDeviceMemoryAsync(CancellationToken cancellationToken)
        {
            //const string workingSetName = "workingSet";

            long workingSet = Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
        }

        // Send device serial number over property update.
        private async Task SendDeviceSerialNumberAsync(CancellationToken cancellationToken)
        {
            const string propertyName = "location";
            TwinCollection reportedProperties = PnpConvention.CreatePropertyPatch(propertyName, Location);
            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
            _logger.LogDebug($"Property: Update - {{ \"{propertyName}\": \"{Location}\" }} is complete.");
        }

        private async Task SendDevicePropertyAsync(CancellationToken cancellationToken, DevicePropery devicePropery)
        {
            const string propertyName = "deviceProperty";
            if(devicePropery != null)
            {
                DeviceProperty = devicePropery;
            }
            TwinCollection reportedProperties = PnpConvention.CreatePropertyPatch(propertyName, DeviceProperty);
            
            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
            
            _logger.LogDebug($"Property: Update - {{ \"{propertyName}\": \"{DeviceProperty}\" }} is complete.");
        }

        private async Task SendTemperatureAsync(string componentName, CancellationToken cancellationToken)
        {
            await SendTemperatureTelemetryAsync(componentName, cancellationToken);

            double maxTemp = _temperatureReadingsDateTimeOffset[componentName].Values.Max<double>();
            if (maxTemp > _maxTemp[componentName])
            {
                _maxTemp[componentName] = maxTemp;
                await UpdateMaxTemperatureSinceLastRebootAsync(componentName, cancellationToken);
            }
        }

        
        private async Task SendCameraTelemetryAsync(CameraTelemetry cameraTelemetry, CancellationToken cancellationToken)
        {
            string telemetryName = "CameraTelemey";
          

            Dictionary<string, CameraTelemetry> cameraInfo = new Dictionary<string, CameraTelemetry>();
            cameraInfo.Add(telemetryName, cameraTelemetry);


            CameraTelemetry currentCameraInfo = cameraInfo[telemetryName];
            //using Message msg = PnpConvention.CreateMessage(telemetryName, currentCameraInfo, telemetryName);


            //Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message to central: {myCount}, Body: [{dataBuffer}]");
            string messageString = "";

            try
            {

                messageString = JsonConvert.SerializeObject(cameraTelemetry);
                 var message = new Message(Encoding.ASCII.GetBytes(messageString));
                Console.WriteLine("test before send camera telemetry");
                await _deviceClient.SendEventAsync(message, cancellationToken);
                Console.WriteLine("test after send camera telemetry");
                _logger.LogDebug($"Telemetry: Sent - component=\"{telemetryName}\", {{ \"{telemetryName}\": {currentCameraInfo} }}.");



            }
            catch(Exception exc)
            {
                Console.WriteLine("json serialization failed");
                Console.WriteLine(exc.Message);
            }
            
                      
        }

        private async Task SendTemperatureTelemetryAsync(string componentName, CancellationToken cancellationToken)
        {
            const string telemetryName = "temperature";
            double currentTemperature = _temperature[componentName];
            using Message msg = PnpConvention.CreateMessage(telemetryName, currentTemperature, componentName);



            string dataBuffer = JsonConvert.SerializeObject(msg);
            var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
            //Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message to central: {myCount}, Body: [{dataBuffer}]");
            string messageString = "";
            messageString = JsonConvert.SerializeObject(msg);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));
            await _deviceClient.SendEventAsync(message, cancellationToken);
            _logger.LogDebug($"Telemetry: Sent - component=\"{componentName}\", {{ \"{telemetryName}\": {currentTemperature} }} in °C.");

            if (_temperatureReadingsDateTimeOffset.ContainsKey(componentName))
            {
                _temperatureReadingsDateTimeOffset[componentName].TryAdd(DateTimeOffset.UtcNow, currentTemperature);
            }
            else
            {
                _temperatureReadingsDateTimeOffset.TryAdd(
                    componentName,
                    new Dictionary<DateTimeOffset, double>
                    {
                        { DateTimeOffset.UtcNow, currentTemperature },
                    });
            }
        }

        private async Task UpdateMaxTemperatureSinceLastRebootAsync(string componentName, CancellationToken cancellationToken)
        {
            const string propertyName = "maxTempSinceLastReboot";
            double maxTemp = _maxTemp[componentName];
            TwinCollection reportedProperties = PnpConvention.CreateComponentPropertyPatch(componentName, propertyName, maxTemp);

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
            _logger.LogDebug($"Property: Update - component=\"{componentName}\", {{ \"{propertyName}\": {maxTemp} }} in °C is complete.");
        }
    }

}


