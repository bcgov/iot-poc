namespace Weather.Data
{
        using System;
    
// Wind data
public class Wind
{
    public DateTime TmStamp { get; set; }
    public int RecNum { get; set; }
    public string StationID { get; set; } = "";
    public int Identifier { get; set; }
    public float MaxWindSpd { get; set; }
    public float MeanWindSpd { get; set; }
    public float WindSpd { get; set; }
    public float WindSpdQ { get; set; }
    public float MeanWindDir { get; set; }
    public float StDevWind { get; set; }
    public float WindDir { get; set; }
    public float DerimeStat { get; set; }
}

}
