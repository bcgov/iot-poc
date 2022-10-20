namespace Weather.Data
{
        using System;
    
// Weather station data
public class Station
{
    // Unique Weather Station ID. This is the Device ID (in IoT Central) for the Weather Station.
    public string StationID { get; set; } = "";

    // The name of the Weather Station.
    public string StationName { get; set; } = "";

    // The last time when this device sent data to IoT Central.
    public DateTime LastUploadTime { get; set; } = DateTime.MinValue;

    // IoT Central connection string. IoT Central may move devices among underlying IoT Hubs. So, this will be updated when it changes.
    public string ConnectionString { get; set; } = "";
}


}
