
namespace Weather.Data
{
    using System.Text.Json;
using System.Text.Json.Serialization;
    using System;

    
// Air humidity telemetry data
public class AirHumidityTelemetry
{
    public float MaxAirTemp1 { get; set; }
    public float CurAirTemp1 { get; set; }
    public float MinAirTemp1 { get; set; }
    public float AirTempQ { get; set; }
    public float AirTemp2 { get; set; }
    public float AirTemp2Q { get; set; }
    public float RH { get; set; }
    public float Dew_Point { get; set; }
}

// Atmosphereic pressure telemetry data
public class AtmosPressureTelemetry
{
    public float AtmPressure { get; set; }
}

// Pavement telemetry data
public class PavementTelemetry
{
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

public class PrecipitationTelemetry
{
    public float GaugeTot { get; set; }
    public float NewPrecip { get; set; }
    public float HrlyPrecip { get; set; }
    public float PrecipGaugeQ { get; set; }
    public float PrecipDetRatio { get; set; }
    public float PrecipDetQ { get; set; }
}

// Snow telemetry data
public class SnowTelemetry
{
    public float HS { get; set; }
    public float HStd { get; set; }
    public float HrlySnow { get; set; }
    public float SnowQ { get; set; }
}

// Wind telemetry data
public class WindTelemetry
{
    public float MaxWindSpd { get; set; }
    public float MeanWindSpd { get; set; }
    public float WindSpd { get; set; }
    public float WindSpdQ { get; set; }
    public float MeanWindDir { get; set; }
    public float StDevWind { get; set; }
    public float WindDir { get; set; }
    public float DerimeStat { get; set; }
}

// Weather telemetry data
public class WeatherTelemetry
{
    // Date time when this data was collected. This is not sent in the message, instead it is sent in the message header
    public DateTime TmStamp { get; set; }

    // The station for which collected this data. This is not sent in the message. Device connection already is specific to a station.
    [JsonIgnore]
    public string StationID { get; set; } = "";

    // Air humidity telemetry data
    public AirHumidityTelemetry AirHumidity { get; set; }

    // Atmosphereic pressure telemetry data
    public AtmosPressureTelemetry AtmosPressure { get; set; }

    // Pavement telemetry data
    public PavementTelemetry Pavement { get; set; }

    // Precipitation telemetry data
    public PrecipitationTelemetry Precipitation { get; set; }

    // Snow telemetry data
    public SnowTelemetry Snow { get; set; }

    // Wind telemetry data
    public WindTelemetry Wind { get; set; }
}

}
