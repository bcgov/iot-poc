namespace Weather.Data
{
        using System;
    // Atomospheric pressure data
    public class AtmosPressure
    {
        public DateTime TmStamp { get; set; }
        public int RecNum { get; set; }
        public string StationID { get; set; } = "";
        public int Identifier { get; set; }
        public float AtmPressure { get; set; }
    }

}

