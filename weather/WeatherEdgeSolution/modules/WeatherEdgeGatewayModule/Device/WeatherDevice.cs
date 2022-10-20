

namespace Weather.Device
{
    using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using System;

using Weather.Data;
    
// Weather station device that connects to IoT Central over MQTT and sends telemetry data
// This device is always connected to send and receive data from IoT Central.
public class WeatherDevice
{
    /////////////////////////////////////////////////////////////////
    // general purpose variables
    /////////////////////////////////////////////////////////////////

    // device client for sending and receiving data from IoT Central
    private DeviceClient iotClient;
    // connection status of the IoT Central device
    private ConnectionStatus connectionStatus;
    private bool isConnecting;
    // application settings
    private Settings settings;

    /////////////////////////////////////////////////////////////////
    // Properties
    /////////////////////////////////////////////////////////////////

    // weather station that this device represents
    public Station Station { get; set; }
    // Is the device connected to IoT Central?
    public bool IsConnected { get { return connectionStatus == ConnectionStatus.Connected; } }
    // Event that is triggered when the device is Re-Provisioned in IoT Central
    public event EventHandler<ProvisioningResult> ProvisioningChanged;

    // Create a new WeatherDevice
    public WeatherDevice(Settings settings, Station station)
    {
        this.settings = settings;
        this.Station = station;
        this.connectionStatus = ConnectionStatus.Disconnected;
    }

    // Connect to IoT Central via Device Provisioning Servicee (DPS)
    public async Task<bool> ConnectAsync()
    {
        Log.Debug($"connecting device {Station.StationID}");
        try
        {
            // try to connect to IoT Central only if it is provisioned
            if (Station.ConnectionString == null || Station.ConnectionString == "")
            {
                Log.Error($"device {Station.StationID} is not provisioned, connection failed");
                return false;
            }

            this.isConnecting = true;
            // try to connect to IoT Hub few times with exponential backoff
            for (int retry = 0; retry < 2; retry++)
            {
                try
                {
                    // create a new IoT Hub client using the connection string that was created during DPS provisioning
                    iotClient = DeviceClient.CreateFromConnectionString(Station.ConnectionString, TransportType.Mqtt);
                    iotClient.SetConnectionStatusChangesHandler(ConnectionStatusChanges);

                    // cannot use the default retry logic built into the SDK as it will not fallback to DPS on reconnects
                    iotClient.SetRetryPolicy(new NoRetry());

                    // connect to the underlying IoT Hub
                    try
                    {
                        await iotClient.OpenAsync();

                    }
                    catch(Exception exc)
                    {
                        Console.WriteLine(exc.Message);

                    }
                    

                    if (retry > 0)
                    {
                        Log.Debug($"connected to IoT Hub, device {Station.StationID}, retry #{retry}");
                    }
                    this.isConnecting = false;
                    return true;
                }
                catch (Microsoft.Azure.Devices.Client.Exceptions.UnauthorizedException ue)
                {
                    // device has been moved to another hub or deleted from IoT Central, try to re-provision
                    Log.Error($"failed to connect to IoT Hub as device has been deleted or moved to another iot hub, device {Station.StationID} will be reprovisioned next time telemetry is sent. Error: {ue.Message}");
                    iotClient = null;

                    // clear the cached connection string so that the device will be re-provisioned
                    this.Station.ConnectionString = "";
                    ProvisioningChanged?.Invoke(this, new ProvisioningResult() { ConnectionString = "", DeviceId = Station.StationID, Success = false });

                    this.isConnecting = false;
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"failed to connect to IoT Hub, device {Station.StationID}, retry #{retry}");
                    iotClient = null;
                }

                // wait for a little bit before trying again
                Task.WaitAll(Task.Delay(2000 * (retry + 1)));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "error connecting to IoT Hub");
        }

        this.isConnecting = false;
        return false;
    }

    // Provision this device in IoT Central using Device Provisioning Service (DPS)
    public async Task<ProvisioningResult> ProvisionAsync()
    {
        try
        {
            Log.Debug("initializing the device provisioning client");

            // calculate device symmetric key from group symmetric key
            string deviceKey = ComputeDerivedSymmetricKey(settings.IoTCentral.GroupSASKey, Station.StationID);

            // DPS provision with device symmetric key
            using var security = new SecurityProviderSymmetricKey(
                Station.StationID,
                deviceKey,
                null);
            using var transportHandler = new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly);
            ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                settings.IoTCentral.GlobalDeviceEndpoint,
                settings.IoTCentral.IDScope,
                security,
                transportHandler);

            Log.Debug($"initialized for registration Id {security.GetRegistrationID()}.");

            // set the device model so that the device is registered as a "Weather Station" device type
            var modelPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = $"{{ \"modelId\": \"{settings.IoTCentral.ModelID}\" }}",
            };


            Log.Debug("registering with the device provisioning service");
            // register the device and get the hub host name
            DeviceRegistrationResult result = await provClient.RegisterAsync(modelPayload);
            Log.Debug($"registration status: {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                Log.Error($"device registration failed for {Station.StationID}, DPS status: {result.Status}");
                return new ProvisioningResult() { DeviceId = Station.StationID, ConnectionString = "", Success = false };
            }
            else
            {
                Log.Debug($"device {result.DeviceId} registered to {result.AssignedHub}");
                return new ProvisioningResult()
                {
                    DeviceId = Station.StationID,
                    ConnectionString = $"HostName={result.AssignedHub};DeviceId={Station.StationID};SharedAccessKey={deviceKey}",
                    Success = true
                };
            }
            
        }
        catch (Exception ex)
        {
            Log.Error(ex, "error connecting to IoT Hub");
        }

        return new ProvisioningResult() { DeviceId = Station.StationID, ConnectionString = "", Success = false };
    }

    // send telemetry to IoT Central
    public async Task<TelemetryResult> SendTelemetry(WeatherTelemetry telemetry, CancellationToken token)
    {
        try
        {
            if (this.isConnecting)
            {
                return new TelemetryResult() { DeviceId = Station.StationID, TmStamp = telemetry.TmStamp, Success = false };
            }

            if (!this.IsConnected)
            {
                await ProvisionAndConnectAsync();
            }

            // send telemetry only if the device is connected to IoT Central
            // devices are reconnected when they disconnected using the connection status change handler
            if (this.IsConnected)
            {
                //string telemetryPayload = Newtonsoft.Json.JsonConvert.SerializeObject(telemetry, Newtonsoft.Json.Formatting.None);
                string telemetryPayload = JsonSerializer.Serialize<WeatherTelemetry>(telemetry);
                using var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload));
                message.ContentType = "application/json";
                message.ContentEncoding = Encoding.UTF8.ToString();
                message.CreationTimeUtc = telemetry.TmStamp;    // set the message timestamp to when the data is generated; make sure that this stored as UTC timestamp in Weather database
                if (iotClient != null)
                {
                    Log.Debug("sending telemetry {@telemetry}", telemetryPayload);

                    // send telemetry event to IoT Central
                    await iotClient.SendEventAsync(message, token);
                    return new TelemetryResult() { DeviceId = Station.StationID, TmStamp = telemetry.TmStamp, Success = true };
                }
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, "error sending telemetry");
        }
        return new TelemetryResult() { DeviceId = Station.StationID, TmStamp = telemetry.TmStamp, Success = false };
    }

    // send telemetry to IoT Central
    public async Task<TelemetryResult> SendTelemetryNew(WeatherData1 telemetry, CancellationToken token)
    {
        DateTime tempTmStamp = DateTime.Parse(telemetry.measurements.timestamp);
        try
        {
            
            if (this.isConnecting)
            {
                
                return new TelemetryResult() { DeviceId = Station.StationID, TmStamp = tempTmStamp, Success = false };
            }

            if (!this.IsConnected)
            {
                await ProvisionAndConnectAsync();
            }

            // send telemetry only if the device is connected to IoT Central
            // devices are reconnected when they disconnected using the connection status change handler
            if (this.IsConnected)
            {
                string telemetryPayload = Newtonsoft.Json.JsonConvert.SerializeObject(telemetry, Newtonsoft.Json.Formatting.None);
                //string telemetryPayload = JsonSerializer.Serialize<WeatherData1>(telemetry);
                using var message = new Message(Encoding.UTF8.GetBytes(telemetryPayload));
                message.ContentType = "application/json";
                message.ContentEncoding = Encoding.UTF8.ToString();
                message.CreationTimeUtc = tempTmStamp;    // set the message timestamp to when the data is generated; make sure that this stored as UTC timestamp in Weather database
                if (iotClient != null)
                {
                    Log.Debug("sending telemetry {@telemetry}", telemetryPayload);

                    // send telemetry event to IoT Central
                    await iotClient.SendEventAsync(message, token);
                    return new TelemetryResult() { DeviceId = Station.StationID, TmStamp = tempTmStamp, Success = true };
                }
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, "error sending telemetry");
        }
        return new TelemetryResult() { DeviceId = Station.StationID, TmStamp = tempTmStamp, Success = false };
    }

    // handler for disconnects, reconnect via DPS
    private async void ConnectionStatusChanges(ConnectionStatus status, ConnectionStatusChangeReason reason)
    {
        Log.Debug($"connection status changed for {Station.StationID}, isConnecting: {isConnecting}, status: {status}, rason: {reason}");
        if (status == ConnectionStatus.Connected)
        {
            // device is connected
            connectionStatus = status;
        }
        else if (status == ConnectionStatus.Disabled || status == ConnectionStatus.Disconnected_Retrying)
        {
            // don't retry disabled devices
            connectionStatus = status;
        }
        else if (status == ConnectionStatus.Disconnected)
        {
            if (!this.isConnecting)
            {
                await ProvisionAndConnectAsync();
            }
        }
    }

    private async Task ProvisionAndConnectAsync()
    {
        Log.Information($"provision and connecting to IoT Hub for device {Station.StationID}");
        // device which was connected got disconnected
        this.connectionStatus = ConnectionStatus.Disconnected;

        // clean up old connection
        if (iotClient != null)
        {
            iotClient.SetConnectionStatusChangesHandler(null);
            try
            {
                await iotClient.CloseAsync();
            }
            catch (Exception)
            {
                //ignore errors
            }

            iotClient = null;
        }

        // try to connect using same connection string with a few retries
        Log.Information($"disconnected from IoT Hub, retrying connection {Station.StationID}");
        bool reconnected = await ConnectAsync();
        if (!reconnected)
        {
            // failed to connect using existing connection string, device might have been moved to another IoT Hub
            // reprovision device and get a new connection string
            var pr = await ProvisionAsync();
            if (pr.ConnectionString != "")
            {
                Station.ConnectionString = pr.ConnectionString;
                var res = await ConnectAsync();
                if (res)
                {
                    Log.Information($"reconnected to IoT Hub, device {Station.StationID}");
                    this.connectionStatus = ConnectionStatus.Connected;

                    // fire an event to notify the Gateway that the device has been moved i.e. connection string is changed
                    // gateway will presist this new connection string in the database
                    ProvisioningChanged?.Invoke(this, pr);
                }
            }
        }
    }

    // calculate the device key using the symetric group key
    private string ComputeDerivedSymmetricKey(string enrollmentKey, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(enrollmentKey))
        {
            return enrollmentKey;
        }

        using var hmac = new HMACSHA256(Convert.FromBase64String(enrollmentKey));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceId)));
    }
}

}

