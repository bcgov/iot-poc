namespace Weather.Device
{
    // Device provisioning result
public class ProvisioningResult
{
    // The unique ID of a weather station device that is provisioned
    public string DeviceId { get; set; } = "";

    // IoT Hub Connection string for the device
    public string ConnectionString { get; set; } = "";

    // Did the provisioning succeed?
    public bool Success { get; set; } = false;
}


}

