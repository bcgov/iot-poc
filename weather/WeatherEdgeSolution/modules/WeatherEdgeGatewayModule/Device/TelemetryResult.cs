namespace Weather.Device
{
    using System;
    
// Result of sending telemetry data to IoT Central
public class TelemetryResult
{
    // The unique ID of a weather station device that is sending telemetry data
    public string DeviceId { get; set; } = "";

    // Timestamp of when the data was captred on weather station
    public DateTime TmStamp { get; set; }

    // Did the telemetry send succeed?
    public bool Success { get; set; } = false;
}


}
