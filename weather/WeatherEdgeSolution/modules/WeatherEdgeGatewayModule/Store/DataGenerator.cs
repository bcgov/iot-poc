

namespace Weather.Store
{
    using System.Collections.Generic;
    using System.Diagnostics;
using Serilog;
using System.Threading;
using System;

using Weather.Data;
    
// Data Generator generates weather station data and stores it in a database.
public class DataGenerator
{
    /////////////////////////////////////////////////////////////////
    // general purpose variables
    /////////////////////////////////////////////////////////////////

    // application settings
    private Settings settings;
    // data store to interact with the database
    private DataStore dataStore;
    // weather stations to generate data for
    private List<Station> stations;
    // generator thread
    private Thread genThread;
    // cancellation token for the generator thread
    private CancellationTokenSource genToken;
    // random number generator
    private Random rand;

    // create a new data generator
    public DataGenerator(Settings settings)
    {
        this.settings = settings;
        this.dataStore = new DataStore(settings);
        stations = new List<Station>();
        rand = new Random();

        // initialize data generation thread
        genToken = new CancellationTokenSource();
        genThread = new Thread(StartDataGenerationPump);
        genThread.IsBackground = true;
        genThread.Name = "DataGeneratorThread";
    }

    // start the data generator thread
    public void Start()
    {
        // if the data generator is not enabled, do not start it
        if (!settings.DataGenerator.Enabled)
        {
            Log.Debug("dataGenerator is disabled, not starting");
            return;
        }

        Log.Debug("data generator starting");

        // open the database connection
        dataStore.Open();

        // create weather stations
        CreateWeatherStations(Program.AllWeatherData);

        // start data generator thread
        genThread.Start(genToken.Token);

        Log.Information("data generator started");
    }

    // Stop the data generator thread
    public void Stop()
    {
        // if the data generator is not enabled, nothing to do here
        if (!settings.DataGenerator.Enabled)
        {
            return;
        }

        Log.Debug("data generator stopping");

        // stop the data generator pump and cleanup other resources
        genToken.Cancel();
        genThread.Join();
        dataStore.Close();
        Log.Information("data generator stopped");
    }

    // Start the data generator pump that generates the weather data for all stations and stores it in the database.
    private void StartDataGenerationPump(object obj)
    {
        CancellationToken token = (obj == null) ? CancellationToken.None : (CancellationToken)obj;
        while (!token.IsCancellationRequested)
        {
            if (settings.DataGenerator.Enabled)
            {
                try
                {
                    // generate data
                    GenerateData();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "error generating data");
                }
            }

            // induce some sleep before we generate more data
            var cancelled = token.WaitHandle.WaitOne(1000 * settings.DataGenerator.GenerationInterval);
            if (cancelled)
            {
                break;
            }
        }
    }

    // Generate data for all weather stations with the same timestamp.
    private void GenerateData()
    {
        List<WeatherData1> allWeatherData = Program.AllWeatherData;

        List<AirHumidity> airHumidityList = new List<AirHumidity>();
        List<AtmosPressure> atmosPressureList = new List<AtmosPressure>();
        List<Pavement> pavementList = new List<Pavement>();
        List<Precipitation> precipitationList = new List<Precipitation>();
        List<Snow> snowList = new List<Snow>();
        List<Wind> windList = new List<Wind>();
        DateTime now = DateTime.Now;
        Log.Information("generating data");
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < allWeatherData.Count; i++)
        {
            // Air_Humidity
            airHumidityList.Add(new AirHumidity()
            {
                TmStamp = now,
                RecNum = 0,
                StationID = stations[i].StationID,
                Identifier = 131,
                MaxAirTemp1 = allWeatherData[i].measurements.maxAirTemp,
                CurAirTemp1 = allWeatherData[i].measurements.currentAirTemp,
                MinAirTemp1 = allWeatherData[i].measurements.minAirTemp,
                AirTempQ = allWeatherData[i].measurements.airTempQuality,
                AirTemp2 = allWeatherData[i].measurements.airTempAlternate,
                AirTemp2Q = allWeatherData[i].measurements.airTempAlternateQuality,
                RH = allWeatherData[i].measurements.relativeHumidity,
                Dew_Point = allWeatherData[i].measurements.dewPoint,
            });

            // Atmos_Pressure
            atmosPressureList.Add(new AtmosPressure()
            {
                TmStamp = now,
                RecNum = 0,
                StationID = allWeatherData[i].deviceId,
                Identifier = 131,
                AtmPressure = allWeatherData[i].measurements.atmospherePressure,
            });

            // Pavement
            float pvmntTemp1 = GetRandomFloat(6.0f, 15.0f);
            pavementList.Add(new Pavement()
            {
                TmStamp = now,
                RecNum = 0,
                StationID = allWeatherData[i].deviceId,
                Identifier = 137,
                PvmntTemp1 = allWeatherData[i].measurements.pavementTemp,
                PavementQ1 = allWeatherData[i].measurements.pavementTempQuality,
                AltPaveTemp1 = allWeatherData[i].measurements.alternatePavementTemp,
                FrzPntTemp1 = allWeatherData[i].measurements.freezePointTemp,
                FrzPntTemp1Q = allWeatherData[i].measurements.freezePointTempQuality,
                PvmntCond = allWeatherData[i].measurements.pavementCondition,
                PvmntCond1Q = allWeatherData[i].measurements.pavementConditionQuality,
                SbAsphltTemp = allWeatherData[i].measurements.subAsphaltTemp,
                PvBaseTemp1 = allWeatherData[i].measurements.pavementBaseTemp,
                PvBaseTemp1Q = allWeatherData[i].measurements.pavementBaseTempQuality,
                PvmntSrfCvTh = allWeatherData[i].measurements.pavementSurfaceConductivity,
                PvmntSrfCvThQ = allWeatherData[i].measurements.pavementSurfaceConductivityQuality,
            });

            // Precipitation
            precipitationList.Add(new Precipitation()
            {
                TmStamp = now,
                RecNum = 0,
                StationID = allWeatherData[i].deviceId,
                Identifier = 132,
                GaugeTot = 0,
                NewPrecip = 0,
                HrlyPrecip = GetRandomFloat(0.0f, 3.0f),
                PrecipGaugeQ = 500,
                PrecipDetRatio = 0,
                PrecipDetQ = 500
            });

            // Snow
            snowList.Add(new Snow()
            {
                TmStamp = now,
                RecNum = 0,
                StationID = allWeatherData[i].deviceId,
                Identifier = 132,
                HS = 0,
                HStd = 0,
                HrlySnow = 0,
                SnowQ = 500,
            });

            // Wind
            windList.Add(new Wind()
            {
                TmStamp = now,
                RecNum = 0,
                StationID = allWeatherData[i].deviceId,
                Identifier = 134,
                MaxWindSpd = allWeatherData[i].measurements.maxWindSpeed,
                MeanWindSpd = allWeatherData[i].measurements.meanWindSpeed,
                WindSpd = allWeatherData[i].measurements.windSpeed,
                WindSpdQ = allWeatherData[i].measurements.windSpeedQuality,
                MeanWindDir = allWeatherData[i].measurements.meanWindDirection,
                StDevWind = allWeatherData[i].measurements.standardWindDeviation,
                WindDir = allWeatherData[i].measurements.windDirection,
                DerimeStat = -6999,
            });
        }

        sw.Stop();
        Log.Information($"generated {stations.Count} stations data in {sw.ElapsedMilliseconds} ms");
    }

    // create weather stations in the database
    private void CreateWeatherStations(List<WeatherData1> allWeatherData)
    {
        // get existing number of weather stations
        int existingCount = 0;
        stations = dataStore.ListStations(allWeatherData, ref existingCount);
        if (existingCount > settings.DataGenerator.StationCount)
        {
            stations.RemoveRange(settings.DataGenerator.StationCount, existingCount - settings.DataGenerator.StationCount);
        }
        else
        {
            // add new weather stations only if needed
            for (int i = 0; i < allWeatherData.Count; i++)
            {
                var station = new Station()
                {
                    StationID = allWeatherData[i].deviceId,
                    StationName = "Weather Station " + allWeatherData[i].deviceId,
                    LastUploadTime = DateTime.MinValue,
                };
                dataStore.AddStation(station);
                stations.Add(station);
            }
        }
    }

    // get random float between min and max
    private float GetRandomFloat(float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }
}

}
