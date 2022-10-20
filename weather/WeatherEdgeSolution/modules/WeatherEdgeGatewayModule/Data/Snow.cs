namespace Weather.Data
{
        using System;
    
// Snow data
public class Snow
{
    public DateTime TmStamp { get; set; }
    public int RecNum { get; set; }
    public string StationID { get; set; } = "";
    public int Identifier { get; set; }
    public float HS { get; set; }
    public float HStd { get; set; }
    public float HrlySnow { get; set; }
    public float SnowQ { get; set; }
}


}
