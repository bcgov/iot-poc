namespace Weather.Data
{
        using System;
    // Precipitation data
    public class Precipitation
    {
        public DateTime TmStamp { get; set; }
        public int RecNum { get; set; }
        public string StationID { get; set; } = "";
        public int Identifier { get; set; }
        public float GaugeTot { get; set; }
        public float NewPrecip { get; set; }
        public float HrlyPrecip { get; set; }
        public float PrecipGaugeQ { get; set; }
        public float PrecipDetRatio { get; set; }
        public float PrecipDetQ { get; set; }
    }

}
