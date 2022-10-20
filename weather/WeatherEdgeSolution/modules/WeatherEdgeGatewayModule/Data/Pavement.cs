namespace Weather.Data
{
        using System;
    
// Pavement data
public class Pavement
{
    public DateTime TmStamp { get; set; }
    public int RecNum { get; set; }
    public string StationID { get; set; } = "";
    public int Identifier { get; set; }
    public float PvmntTemp1 { get; set; }
    public float PavementQ1 { get; set; }
    public float AltPaveTemp1 { get; set; }
    public float FrzPntTemp1 { get; set; }
    public float FrzPntTemp1Q { get; set; }
    public float PvmntCond { get; set; }
    public float PvmntCond1Q { get; set; }
    public float SbAsphltTemp { get; set; }
    public float PvBaseTemp1 { get; set; }
    public float PvBaseTemp1Q { get; set; }
    public float PvmntSrfCvTh { get; set; }
    public float PvmntSrfCvThQ { get; set; }
}

}
