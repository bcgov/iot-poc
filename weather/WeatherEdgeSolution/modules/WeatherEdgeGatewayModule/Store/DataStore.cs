

namespace Weather.Store
{
    using MySql.Data.MySqlClient;
using Serilog;

using Weather.Data;
using System.Collections.Generic;
using System;
    using System.IO;
    
// Data store is a class that interacts with the weather station database.
public class DataStore
{
    /////////////////////////////////////////////////////////////////
    // general purpose variables
    /////////////////////////////////////////////////////////////////

    // application settings
    private Settings settings;
    // database connection
    private MySqlConnection dbConnection;

    // create a new data store
    public DataStore(Settings settings)
    {
        this.settings = settings;
        this.dbConnection = new MySqlConnection(settings.Database.ConnectionString);
    }

    #region Database
    // open the database connection
    public void Open()
    {
        try
        {
                dbConnection.Open();
                Log.Debug($"connected to MySQL Server, version: {dbConnection.ServerVersion}");
                Console.WriteLine("my sql database connected and data retrieved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "error opening database connection");
            throw;
        }
    }

    // close the database connection
    public void Close()
    {
        try
        {
            dbConnection.Close();
            dbConnection.Dispose();
            Log.Debug("database connection closed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "error closing database connection");
        }
    }
    #endregion

    #region Station
    // add a new weather station to the database
    public void AddStation(Station station)
    {


        try
        {
            var query = "INSERT INTO Stations (`StationID`, `StationName`, `LastUploadTime`, `ConnectionString`) VALUES (@StationID, @StationName, @LastUploadTime, @ConnectionString)";
            using var cmd = new MySqlCommand(query, dbConnection);
            cmd.Parameters.AddWithValue("@StationID", station.StationID);
            cmd.Parameters.AddWithValue("@StationName", station.StationName);
            cmd.Parameters.AddWithValue("@LastUploadTime", station.LastUploadTime);
            cmd.Parameters.AddWithValue("@ConnectionString", station.ConnectionString);
            cmd.ExecuteNonQuery();

        }
        catch(Exception exc)
        {
            Console.WriteLine("insert to staions error - " + exc.Message);
        }

       
    }

    // update a weather station in the database
    public void UpdateStation(Station station)
    {
        var query = "UPDATE Stations SET `StationName` = @StationName, `LastUploadTime` = @LastUploadTime, `ConnectionString = @ConnectionString WHERE StationID = @StationID";
        using var cmd = new MySqlCommand(query, dbConnection);
        cmd.Parameters.AddWithValue("@StationID", station.StationID);
        cmd.Parameters.AddWithValue("@StationName", station.StationName);
        cmd.Parameters.AddWithValue("@LastUploadTime", station.LastUploadTime);
        cmd.Parameters.AddWithValue("@ConnectionString", station.ConnectionString);
        cmd.ExecuteNonQuery();
    }

    // update the last upload time of a list of weather stations in the database
    public void UpdateStationsLastUploadTime(List<Station> stations)
    {
        var query = "UPDATE Stations SET `LastUploadTime` = @LastUploadTime WHERE StationID = @StationID";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var station in stations)
        {
            cmd.Parameters.AddWithValue("@StationID", station.StationID);
            cmd.Parameters.AddWithValue("@LastUploadTime", station.LastUploadTime);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }

    // update the connection string of a list of weather stations in the database
    public void UpdateStationsConnectionString(List<Station> stations)
    {
        var query = "UPDATE Stations SET `ConnectionString` = @ConnectionString WHERE StationID = @StationID";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var station in stations)
        {
            cmd.Parameters.AddWithValue("@StationID", station.StationID);
            cmd.Parameters.AddWithValue("@ConnectionString", station.ConnectionString);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }
        public void CreateDbStations()
        {
            MySqlScript script = new MySqlScript(dbConnection, File.ReadAllText("database.sql"));
            script.Delimiter = "$$";
            script.Execute();
        }

            // get all the weather stations from the database
        public List<Station> ListStations(List<WeatherData1> allWeatherData, ref int existingCount)
        {
            List<string> tables = DataRetriever.GetTableNames(DataRetriever.ConnectionString);
            if(!tables.Contains("stations"))
            {                
                CreateDbStations();
            }


            var query = "SELECT `StationID`, `StationName`, `LastUploadTime`, `ConnectionString` FROM Stations ORDER BY `StationID`";
            using var cmd = new MySqlCommand(query, dbConnection);




            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                existingCount++;
            }


            var stations = new List<Station>();
            foreach (var stationData in allWeatherData)
            {
                stations.Add(new Station()
                {
                    StationID = stationData.deviceId,
                    StationName = "Station-" + stationData.deviceId,
                    LastUploadTime = DateTime.Now,
                    ConnectionString = ""
                });
            }
            reader.Close();
            return stations;
    }
    #endregion

    #region Air_Humidity
    // add a new air humidity reading to the database
    public void AddAirHumidity(AirHumidity airHumidity)
    {
        var query = "INSERT INTO Air_Humidity (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `MaxAirTemp1`, `CurAirTemp1`, `MinAirTemp1`, `AirTempQ`, `AirTemp2`, `AirTemp2Q`, `RH`, `Dew_Point`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @MaxAirTemp1, @CurAirTemp1, @MinAirTemp1, @AirTempQ, @AirTemp2, @AirTemp2Q, @RH, @Dew_Point)";
        using var cmd = new MySqlCommand(query, dbConnection);
        cmd.Parameters.AddWithValue("@TmStamp", airHumidity.TmStamp);
        cmd.Parameters.AddWithValue("@RecNum", airHumidity.RecNum);
        cmd.Parameters.AddWithValue("@StationID", airHumidity.StationID);
        cmd.Parameters.AddWithValue("@Identifier", airHumidity.Identifier);
        cmd.Parameters.AddWithValue("@MaxAirTemp1", airHumidity.MaxAirTemp1);
        cmd.Parameters.AddWithValue("@CurAirTemp1", airHumidity.CurAirTemp1);
        cmd.Parameters.AddWithValue("@MinAirTemp1", airHumidity.MinAirTemp1);
        cmd.Parameters.AddWithValue("@AirTempQ", airHumidity.AirTempQ);
        cmd.Parameters.AddWithValue("@AirTemp2", airHumidity.AirTemp2);
        cmd.Parameters.AddWithValue("@AirTemp2Q", airHumidity.AirTemp2Q);
        cmd.Parameters.AddWithValue("@RH", airHumidity.RH);
        cmd.Parameters.AddWithValue("@Dew_Point", airHumidity.Dew_Point);
        cmd.ExecuteNonQuery();
    }

    // add a list of air humidity readings to the database
    public void AddAirHumidity(List<AirHumidity> airHumidityList)
    {
        var query = "INSERT INTO Air_Humidity (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `MaxAirTemp1`, `CurAirTemp1`, `MinAirTemp1`, `AirTempQ`, `AirTemp2`, `AirTemp2Q`, `RH`, `Dew_Point`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @MaxAirTemp1, @CurAirTemp1, @MinAirTemp1, @AirTempQ, @AirTemp2, @AirTemp2Q, @RH, @Dew_Point)";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var airHumidity in airHumidityList)
        {
            cmd.Parameters.AddWithValue("@TmStamp", airHumidity.TmStamp);
            cmd.Parameters.AddWithValue("@RecNum", airHumidity.RecNum);
            cmd.Parameters.AddWithValue("@StationID", airHumidity.StationID);
            cmd.Parameters.AddWithValue("@Identifier", airHumidity.Identifier);
            cmd.Parameters.AddWithValue("@MaxAirTemp1", airHumidity.MaxAirTemp1);
            cmd.Parameters.AddWithValue("@CurAirTemp1", airHumidity.CurAirTemp1);
            cmd.Parameters.AddWithValue("@MinAirTemp1", airHumidity.MinAirTemp1);
            cmd.Parameters.AddWithValue("@AirTempQ", airHumidity.AirTempQ);
            cmd.Parameters.AddWithValue("@AirTemp2", airHumidity.AirTemp2);
            cmd.Parameters.AddWithValue("@AirTemp2Q", airHumidity.AirTemp2Q);
            cmd.Parameters.AddWithValue("@RH", airHumidity.RH);
            cmd.Parameters.AddWithValue("@Dew_Point", airHumidity.Dew_Point);
            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();
        }
    }
    #endregion

    #region Atmos_Pressure
    // add a new atmospheric pressure reading to the database
    public void AddAtmosPressure(AtmosPressure atmosPressure)
    {
        var query = "INSERT INTO Atmos_Pressure (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `AtmPressure`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @AtmPressure)";
        using var cmd = new MySqlCommand(query, dbConnection);
        cmd.Parameters.AddWithValue("@TmStamp", atmosPressure.TmStamp);
        cmd.Parameters.AddWithValue("@RecNum", atmosPressure.RecNum);
        cmd.Parameters.AddWithValue("@StationID", atmosPressure.StationID);
        cmd.Parameters.AddWithValue("@Identifier", atmosPressure.Identifier);
        cmd.Parameters.AddWithValue("@AtmPressure", atmosPressure.AtmPressure);
        cmd.ExecuteNonQuery();
    }

    // add a list of atmospheric pressure readings to the database
    public void AddAtmosPressure(List<AtmosPressure> atmosPressureList)
    {
        var query = "INSERT INTO Atmos_Pressure (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `AtmPressure`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @AtmPressure)";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var atmosPressure in atmosPressureList)
        {
            cmd.Parameters.AddWithValue("@TmStamp", atmosPressure.TmStamp);
            cmd.Parameters.AddWithValue("@RecNum", atmosPressure.RecNum);
            cmd.Parameters.AddWithValue("@StationID", atmosPressure.StationID);
            cmd.Parameters.AddWithValue("@Identifier", atmosPressure.Identifier);
            cmd.Parameters.AddWithValue("@AtmPressure", atmosPressure.AtmPressure);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }
    #endregion

    #region Pavement
    // add a new pavement reading to the database
    public void AddPavement(Pavement pavement)
    {
        var query = "INSERT INTO Pavement (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `PvmntTemp1`, `PavementQ1`, `AltPaveTemp1`, `FrzPntTemp1`, `FrzPntTemp1Q`, `PvmntCond1Q`, `SbAsphltTemp`, `PvBaseTemp1`, `PvBaseTemp1Q`, `PvmntSrfCvTh`, `PvmntSrfCvThQ`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @PvmntTemp1, @PavementQ1, @AltPaveTemp1, @FrzPntTemp1, @FrzPntTemp1Q, @PvmntCond1Q, @SbAsphltTemp, @PvBaseTemp1, @PvBaseTemp1Q, @PvmntSrfCvTh, @PvmntSrfCvThQ)";
        using var cmd = new MySqlCommand(query, dbConnection);
        cmd.Parameters.AddWithValue("@TmStamp", pavement.TmStamp);
        cmd.Parameters.AddWithValue("@RecNum", pavement.RecNum);
        cmd.Parameters.AddWithValue("@StationID", pavement.StationID);
        cmd.Parameters.AddWithValue("@Identifier", pavement.Identifier);
        cmd.Parameters.AddWithValue("@PvmntTemp1", pavement.PvmntTemp1);
        cmd.Parameters.AddWithValue("@PavementQ1", pavement.PavementQ1);
        cmd.Parameters.AddWithValue("@AltPaveTemp1", pavement.AltPaveTemp1);
        cmd.Parameters.AddWithValue("@FrzPntTemp1", pavement.FrzPntTemp1);
        cmd.Parameters.AddWithValue("@FrzPntTemp1Q", pavement.FrzPntTemp1Q);
        cmd.Parameters.AddWithValue("@PvmntCond1Q", pavement.PvmntCond1Q);
        cmd.Parameters.AddWithValue("@SbAsphltTemp", pavement.SbAsphltTemp);
        cmd.Parameters.AddWithValue("@PvBaseTemp1", pavement.PvBaseTemp1);
        cmd.Parameters.AddWithValue("@PvBaseTemp1Q", pavement.PvBaseTemp1Q);
        cmd.Parameters.AddWithValue("@PvmntSrfCvTh", pavement.PvmntSrfCvTh);
        cmd.Parameters.AddWithValue("@PvmntSrfCvThQ", pavement.PvmntSrfCvThQ);
        cmd.ExecuteNonQuery();
    }

    // add a list of pavement readings to the database
    public void AddPavement(List<Pavement> pavementList)
    {
        var query = "INSERT INTO Pavement (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `PvmntTemp1`, `PavementQ1`, `AltPaveTemp1`, `FrzPntTemp1`, `FrzPntTemp1Q`, `PvmntCond1Q`, `SbAsphltTemp`, `PvBaseTemp1`, `PvBaseTemp1Q`, `PvmntSrfCvTh`, `PvmntSrfCvThQ`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @PvmntTemp1, @PavementQ1, @AltPaveTemp1, @FrzPntTemp1, @FrzPntTemp1Q, @PvmntCond1Q, @SbAsphltTemp, @PvBaseTemp1, @PvBaseTemp1Q, @PvmntSrfCvTh, @PvmntSrfCvThQ)";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var pavement in pavementList)
        {
            cmd.Parameters.AddWithValue("@TmStamp", pavement.TmStamp);
            cmd.Parameters.AddWithValue("@RecNum", pavement.RecNum);
            cmd.Parameters.AddWithValue("@StationID", pavement.StationID);
            cmd.Parameters.AddWithValue("@Identifier", pavement.Identifier);
            cmd.Parameters.AddWithValue("@PvmntTemp1", pavement.PvmntTemp1);
            cmd.Parameters.AddWithValue("@PavementQ1", pavement.PavementQ1);
            cmd.Parameters.AddWithValue("@AltPaveTemp1", pavement.AltPaveTemp1);
            cmd.Parameters.AddWithValue("@FrzPntTemp1", pavement.FrzPntTemp1);
            cmd.Parameters.AddWithValue("@FrzPntTemp1Q", pavement.FrzPntTemp1Q);
            cmd.Parameters.AddWithValue("@PvmntCond1Q", pavement.PvmntCond1Q);
            cmd.Parameters.AddWithValue("@SbAsphltTemp", pavement.SbAsphltTemp);
            cmd.Parameters.AddWithValue("@PvBaseTemp1", pavement.PvBaseTemp1);
            cmd.Parameters.AddWithValue("@PvBaseTemp1Q", pavement.PvBaseTemp1Q);
            cmd.Parameters.AddWithValue("@PvmntSrfCvTh", pavement.PvmntSrfCvTh);
            cmd.Parameters.AddWithValue("@PvmntSrfCvThQ", pavement.PvmntSrfCvThQ);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }
    #endregion

    #region Precipitation
    // add a new precipitation reading to the database
    public void AddPrecipitation(Precipitation precipitation)
    {
        var query = "INSERT INTO Precipitation (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `GaugeTot`, `NewPrecip`, `HrlyPrecip`, `PrecipGaugeQ`, `PrecipDetRatio`, `PrecipDetQ`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @GaugeTot, @NewPrecip, @HrlyPrecip, @PrecipGaugeQ, @PrecipDetRatio, @PrecipDetQ)";
        using var cmd = new MySqlCommand(query, dbConnection);
        cmd.Parameters.AddWithValue("@TmStamp", precipitation.TmStamp);
        cmd.Parameters.AddWithValue("@RecNum", precipitation.RecNum);
        cmd.Parameters.AddWithValue("@StationID", precipitation.StationID);
        cmd.Parameters.AddWithValue("@Identifier", precipitation.Identifier);
        cmd.Parameters.AddWithValue("@GaugeTot", precipitation.GaugeTot);
        cmd.Parameters.AddWithValue("@NewPrecip", precipitation.NewPrecip);
        cmd.Parameters.AddWithValue("@HrlyPrecip", precipitation.HrlyPrecip);
        cmd.Parameters.AddWithValue("@PrecipGaugeQ", precipitation.PrecipGaugeQ);
        cmd.Parameters.AddWithValue("@PrecipDetRatio", precipitation.PrecipDetRatio);
        cmd.Parameters.AddWithValue("@PrecipDetQ", precipitation.PrecipDetQ);
        cmd.ExecuteNonQuery();
    }

    // add a list of precipitation readings to the database
    public void AddPrecipitation(List<Precipitation> precipitationList)
    {
        var query = "INSERT INTO Precipitation (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `GaugeTot`, `NewPrecip`, `HrlyPrecip`, `PrecipGaugeQ`, `PrecipDetRatio`, `PrecipDetQ`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @GaugeTot, @NewPrecip, @HrlyPrecip, @PrecipGaugeQ, @PrecipDetRatio, @PrecipDetQ)";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var precipitation in precipitationList)
        {
            cmd.Parameters.AddWithValue("@TmStamp", precipitation.TmStamp);
            cmd.Parameters.AddWithValue("@RecNum", precipitation.RecNum);
            cmd.Parameters.AddWithValue("@StationID", precipitation.StationID);
            cmd.Parameters.AddWithValue("@Identifier", precipitation.Identifier);
            cmd.Parameters.AddWithValue("@GaugeTot", precipitation.GaugeTot);
            cmd.Parameters.AddWithValue("@NewPrecip", precipitation.NewPrecip);
            cmd.Parameters.AddWithValue("@HrlyPrecip", precipitation.HrlyPrecip);
            cmd.Parameters.AddWithValue("@PrecipGaugeQ", precipitation.PrecipGaugeQ);
            cmd.Parameters.AddWithValue("@PrecipDetRatio", precipitation.PrecipDetRatio);
            cmd.Parameters.AddWithValue("@PrecipDetQ", precipitation.PrecipDetQ);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }
    #endregion

    #region Snow
    // add a new snow reading to the database
    public void AddSnow(Snow snow)
    {
        var query = "INSERT INTO Snow (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `HS`, `HStd`, `HrlySnow`, `SnowQ`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @HS, @HStd, @HrlySnow, @SnowQ)";
        using var cmd = new MySqlCommand(query, dbConnection);
        cmd.Parameters.AddWithValue("@TmStamp", snow.TmStamp);
        cmd.Parameters.AddWithValue("@RecNum", snow.RecNum);
        cmd.Parameters.AddWithValue("@StationID", snow.StationID);
        cmd.Parameters.AddWithValue("@Identifier", snow.Identifier);
        cmd.Parameters.AddWithValue("@HS", snow.HS);
        cmd.Parameters.AddWithValue("@HStd", snow.HStd);
        cmd.Parameters.AddWithValue("@HrlySnow", snow.HrlySnow);
        cmd.Parameters.AddWithValue("@SnowQ", snow.SnowQ);
        cmd.ExecuteNonQuery();
    }

    // add a list of snow readings to the database
    public void AddSnow(List<Snow> snowList)
    {
        var query = "INSERT INTO Snow (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `HS`, `HStd`, `HrlySnow`, `SnowQ`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @HS, @HStd, @HrlySnow, @SnowQ)";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var snow in snowList)
        {
            cmd.Parameters.AddWithValue("@TmStamp", snow.TmStamp);
            cmd.Parameters.AddWithValue("@RecNum", snow.RecNum);
            cmd.Parameters.AddWithValue("@StationID", snow.StationID);
            cmd.Parameters.AddWithValue("@Identifier", snow.Identifier);
            cmd.Parameters.AddWithValue("@HS", snow.HS);
            cmd.Parameters.AddWithValue("@HStd", snow.HStd);
            cmd.Parameters.AddWithValue("@HrlySnow", snow.HrlySnow);
            cmd.Parameters.AddWithValue("@SnowQ", snow.SnowQ);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }
    #endregion

    #region Wind
    // add a new wind reading to the database
    public void AddWind(Wind wind)
    {
        var query = "INSERT INTO Wind (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `MaxWindSpd`, `MeanWindSpd`, `WindSpd`, `WindSpdQ`, `MeanWindDir`, `StDevWind`, `WindDir`, `DerimeStat`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @MaxWindSpd, @MeanWindSpd, @WindSpd, @WindSpdQ, @MeanWindDir, @StDevWind, @WindDir, @DerimeStat)";
        using var cmd = new MySqlCommand(query, dbConnection);
        cmd.Parameters.AddWithValue("@TmStamp", wind.TmStamp);
        cmd.Parameters.AddWithValue("@RecNum", wind.RecNum);
        cmd.Parameters.AddWithValue("@StationID", wind.StationID);
        cmd.Parameters.AddWithValue("@Identifier", wind.Identifier);
        cmd.Parameters.AddWithValue("@MaxWindSpd", wind.MaxWindSpd);
        cmd.Parameters.AddWithValue("@MeanWindSpd", wind.MeanWindSpd);
        cmd.Parameters.AddWithValue("@WindSpd", wind.WindSpd);
        cmd.Parameters.AddWithValue("@WindSpdQ", wind.WindSpdQ);
        cmd.Parameters.AddWithValue("@MeanWindDir", wind.MeanWindDir);
        cmd.Parameters.AddWithValue("@StDevWind", wind.StDevWind);
        cmd.Parameters.AddWithValue("@WindDir", wind.WindDir);
        cmd.Parameters.AddWithValue("@DerimeStat", wind.DerimeStat);
        cmd.ExecuteNonQuery();
    }

    // add a list of wind readings to the database
    public void AddWind(List<Wind> windList)
    {
        var query = "INSERT INTO Wind (`TmStamp`, `RecNum`, `StationID`, `Identifier`, `MaxWindSpd`, `MeanWindSpd`, `WindSpd`, `WindSpdQ`, `MeanWindDir`, `StDevWind`, `WindDir`, `DerimeStat`) VALUES (@TmStamp, @RecNum, @StationID, @Identifier, @MaxWindSpd, @MeanWindSpd, @WindSpd, @WindSpdQ, @MeanWindDir, @StDevWind, @WindDir, @DerimeStat)";
        using var cmd = new MySqlCommand(query, dbConnection);
        foreach (var wind in windList)
        {
            cmd.Parameters.AddWithValue("@TmStamp", wind.TmStamp);
            cmd.Parameters.AddWithValue("@RecNum", wind.RecNum);
            cmd.Parameters.AddWithValue("@StationID", wind.StationID);
            cmd.Parameters.AddWithValue("@Identifier", wind.Identifier);
            cmd.Parameters.AddWithValue("@MaxWindSpd", wind.MaxWindSpd);
            cmd.Parameters.AddWithValue("@MeanWindSpd", wind.MeanWindSpd);
            cmd.Parameters.AddWithValue("@WindSpd", wind.WindSpd);
            cmd.Parameters.AddWithValue("@WindSpdQ", wind.WindSpdQ);
            cmd.Parameters.AddWithValue("@MeanWindDir", wind.MeanWindDir);
            cmd.Parameters.AddWithValue("@StDevWind", wind.StDevWind);
            cmd.Parameters.AddWithValue("@WindDir", wind.WindDir);
            cmd.Parameters.AddWithValue("@DerimeStat", wind.DerimeStat);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }

    // add a list of wind readings to the database
    // public void BulkAddWind(List<Wind> windList)
    // {
    //     string csvContent = "";
    //     foreach (var wind in windList)
    //     {
    //         csvContent += $"{wind.TmStamp},{wind.RecNum},{wind.StationID},{wind.Identifier},{wind.MaxWindSpd},{wind.MeanWindSpd},{wind.WindSpd},{wind.WindSpdQ},{wind.MeanWindDir},{wind.StDevWind},{wind.WindDir},{wind.DerimeStat}\n";
    //     }
    //     string fileName = "Wind.csv";
    //     File.WriteAllText(fileName, csvContent);
    //     MySqlBulkLoader loader = new MySqlBulkLoader(this.dbConnection);
    //     loader.TableName = "wind";
    //     loader.FieldTerminator = ";";
    //     loader.LineTerminator = "\n";
    //     loader.FileName = fileName;
    //     loader.NumberOfLinesToSkip = 0;
    //     loader.Load();
    //     File.Delete(fileName);
    // }
    #endregion

    #region Query
    // get the new weather readings from the database
    // get only the data that have not been processed based on the station last upload time and all 6 types of data is present
    public List<WeatherTelemetry> GetWeatherTelemetry()
    {
        List<WeatherTelemetry> weatherTelemetries = new List<WeatherTelemetry>();

        var query = @"SELECT ah.`TmStamp`, ah.`StationID`, ah.`MaxAirTemp1`, ah.`CurAirTemp1`, ah.`MinAirTemp1`, ah.`AirTempQ`, ah.`AirTemp2`, ah.`AirTemp2Q`, ah.`RH`, ah.`Dew_Point`,
		                ap.`AtmPressure`, pv.`PvmntTemp1`, pv.`PavementQ1`, pv.`AltPaveTemp1`, pv.`FrzPntTemp1`, pv.`FrzPntTemp1Q`, pv.`PvmntCond1Q`, pv.`SbAsphltTemp`, pv.`PvBaseTemp1`, pv.`PvBaseTemp1Q`, pv.`PvmntSrfCvTh`, pv.`PvmntSrfCvThQ`,
				        pc.`GaugeTot`, pc.`NewPrecip`, pc.`HrlyPrecip`, pc.`PrecipGaugeQ`, pc.`PrecipDetRatio`, pc.`PrecipDetQ`,
				        sn.`HS`, sn.`HStd`, sn.`HrlySnow`, sn.`SnowQ`, wn.`MaxWindSpd`, wn.`MeanWindSpd`, wn.`WindSpd`, wn.`WindSpdQ`, wn.`MeanWindDir`, wn.`StDevWind`, wn.`WindDir`, wn.`DerimeStat`
                    FROM stations st, air_humidity ah, atmos_pressure ap, pavement pv, precipitation pc, snow sn, wind wn
                    WHERE st.StationID = ah.StationID 
                        AND st.LastUploadTime < ah.TmStamp 
                        AND ah.StationID = ap.StationID AND ah.TmStamp = ap.TmStamp 
                        AND ap.StationID = pv.StationID AND ap.TmStamp = pv.TmStamp 
                        AND pv.StationID = pc.StationID AND pv.TmStamp = pc.TmStamp 
                        AND pc.StationID = sn.StationID AND pc.TmStamp = sn.TmStamp 
                        AND sn.StationID = wn.StationID AND sn.TmStamp = wn.TmStamp
                        ORDER BY ah.TmStamp, ah.StationID";
        using var cmd = new MySqlCommand(query, dbConnection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            weatherTelemetries.Add(new WeatherTelemetry()
            {
                TmStamp = reader.GetDateTime(0),
                StationID = reader.GetString(1),
                AirHumidity = new AirHumidityTelemetry()
                {
                    MaxAirTemp1 = reader.GetFloat(2),
                    CurAirTemp1 = reader.GetFloat(3),
                    MinAirTemp1 = reader.GetFloat(4),
                    AirTempQ = reader.GetFloat(5),
                    AirTemp2 = reader.GetFloat(6),
                    AirTemp2Q = reader.GetFloat(7),
                    RH = reader.GetFloat(8),
                    Dew_Point = reader.GetFloat(9)
                },
                AtmosPressure = new AtmosPressureTelemetry()
                {
                    AtmPressure = reader.GetFloat(10)
                },
                Pavement = new PavementTelemetry()
                {
                    PvmntTemp1 = reader.GetFloat(11),
                    PavementQ1 = reader.GetFloat(12),
                    AltPaveTemp1 = reader.GetFloat(13),
                    FrzPntTemp1 = reader.GetFloat(14),
                    FrzPntTemp1Q = reader.GetFloat(15),
                    PvmntCond1Q = reader.GetFloat(16),
                    SbAsphltTemp = reader.GetFloat(17),
                    PvBaseTemp1 = reader.GetFloat(18),
                    PvBaseTemp1Q = reader.GetFloat(19),
                    PvmntSrfCvTh = reader.GetFloat(20),
                    PvmntSrfCvThQ = reader.GetFloat(21)
                },
                Precipitation = new PrecipitationTelemetry()
                {
                    GaugeTot = reader.GetFloat(22),
                    NewPrecip = reader.GetFloat(23),
                    HrlyPrecip = reader.GetFloat(24),
                    PrecipGaugeQ = reader.GetFloat(25),
                    PrecipDetRatio = reader.GetFloat(26),
                    PrecipDetQ = reader.GetFloat(27)
                },
                Snow = new SnowTelemetry()
                {
                    HS = reader.GetFloat(28),
                    HStd = reader.GetFloat(29),
                    HrlySnow = reader.GetFloat(30),
                    SnowQ = reader.GetFloat(31)
                },
                Wind = new WindTelemetry()
                {
                    MaxWindSpd = reader.GetFloat(32),
                    MeanWindSpd = reader.GetFloat(33),
                    WindSpd = reader.GetFloat(34),
                    WindSpdQ = reader.GetFloat(35),
                    MeanWindDir = reader.GetFloat(36),
                    StDevWind = reader.GetFloat(37),
                    WindDir = reader.GetFloat(38),
                    DerimeStat = reader.GetFloat(39)
                }
            });
        }
        return weatherTelemetries;
    }
    #endregion
}


}
