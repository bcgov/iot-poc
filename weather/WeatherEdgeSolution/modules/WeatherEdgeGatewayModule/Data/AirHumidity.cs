namespace Weather.Data
{
    using System;
// Air Humidity data
public class AirHumidity
{
    public DateTime TmStamp { get; set; }
    public int RecNum { get; set; }
    public string StationID { get; set; } = "";
    public int Identifier { get; set; }
    public float MaxAirTemp1 { get; set; }
    public float CurAirTemp1 { get; set; }
    public float MinAirTemp1 { get; set; }
    public float AirTempQ { get; set; }
    public float AirTemp2 { get; set; }
    public float AirTemp2Q { get; set; }
    public float RH { get; set; }
    public float Dew_Point { get; set; }
}

}
