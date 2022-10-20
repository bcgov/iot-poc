
namespace Weather
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using Weather.Device;
    using Weather.Store;
    using System;

    using System.Collections.Generic;
    using Newtonsoft.Json;
    using MySql.Data.MySqlClient;
    using System.Linq;
    using System.Threading.Tasks;
        using Microsoft.Azure.Cosmos;
    
    public class Program
    {
        public static List<WeatherData1> AllWeatherData;
        public static string ConnectionString;


        public static async Task RunApp()
        {
            //while (true)
            {
                var env = Environment.GetEnvironmentVariable("Environment");
                if (env == null)
                {
                    env = "development";
                }

                // load configuration from appsettings.json
                // override default values with appsettings.{Environment}.json
                // override default values with environment variables
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{env}.json", true, true)
                    .AddEnvironmentVariables()
                    .Build();

                // configure logging
                Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.Console()
                        .WriteTo.File("logs/weather.log", rollingInterval: RollingInterval.Day)
                        .CreateLogger();

                // Get values from the config given their key and their target type.
                Settings settings = config.GetRequiredSection("Settings").Get<Settings>();
                Log.Information($"starting application in {env} mode");
                Settings testSettings = config.GetRequiredSection("Settings").Get<Settings>();
                string connectionString = testSettings.Database.ConnectionString;
                AllWeatherData = DataRetriever.GetData(connectionString);
                ConnectionString = connectionString;


                // start data generator that will generate data for the devices and store them in the database
                DataGenerator generator = new DataGenerator(settings);
                generator.Start();

                // start the gateway that reads data from database and send data to IoT Central through IoT devices
                Gateway gateway = new Gateway(settings);
                gateway.Start();

                foreach(var di in Program.AllWeatherData)
                {
                    await Program.SaveToCosmosDb(di);
                }
                
                // wait until the user presses a key
                // TODO: make this better!!
                //Console.ReadLine();
                while(true);

                // // stop all services
                // generator.Stop();
                // gateway.Stop();
                // Log.Information($"stopped application");

            }

        }



        public static int Main() => MainAsync().Result;
        static async Task<int> MainAsync()
        {
            await RunApp();
            return 0;
        }


        // public static void Main(string[] args)
        // {
        //     RunApp();
        // }

                /// <summary>
        /// Creates the items used in this Sample
        /// </summary>
        /// <param name="container">The selfLink property for the CosmosContainer where items will be created.</param>
        /// <returns>None</returns>
        // <CreateItems>
        private static async Task CreateItems(Container container, WeatherData1 di)
        {

            //string url = @"https://imageribhedg6dsazu.blob.core.windows.net/images/";

            // WeatherData1 testDi = new WeatherData1();
            // testDi.deviceId = di.deviceId;
            // testDi.id = "1";
            string timestamp = DateTime.Now.ToString("u");
            di.id = di.deviceId + "-" + timestamp;
            

            await container.UpsertItemAsync<WeatherData1>(di, new PartitionKey(di.deviceId));

        }

                /// <summary>
        /// Get a DocuemntContainer by id, or create a new one if one with the id provided doesn't exist.
        /// </summary>
        /// <param name="id">The id of the CosmosContainer to search for, or create.</param>
        /// <returns>The matched, or created, CosmosContainer object</returns>
        // <GetOrCreateContainerAsync>
        private static async Task<Container> GetOrCreateContainerAsync(Database database, string containerId)
        {
            ContainerProperties containerProperties = new ContainerProperties(id: containerId, partitionKeyPath: "/deviceId");

            return await database.CreateContainerIfNotExistsAsync(
                containerProperties: containerProperties,
                throughput: 400);
        }

        private static async Task RunDemoAsync(CosmosClient client, WeatherData1 di)
        {
            //Read configuration
            //for Jeremy's subscription cosmos db
            string CosmosDatabaseId = "WeatherObsData2";
            string containerId = "WeatherObsContainer2";

            CosmosDatabaseId = "WeatherDB";
            containerId = "WeatherDataContainer";

            DatabaseResponse cosmosDatabase = await client.CreateDatabaseIfNotExistsAsync(CosmosDatabaseId);
            Container container = await Program.GetOrCreateContainerAsync(cosmosDatabase, containerId);

            await Program.CreateItems(container, di);

            //await Program.ItemFeed(container);

            //await Program.ItemStreamFeed(container);

            //await Program.QueryItemsInPartitionAsStreams(container);

            //await Program.QueryPartitionedContainerInParallelAsync(container);

            //await Program.QueryWithSqlParameters(container);

            //await Program.QueryWithContinuationTokens(container);

            // Uncomment to Cleanup
            //await cosmosDatabase.DeleteAsync();
        }

        public static async Task<TelemetryResult> SaveToCosmosDb(WeatherData1 di)
        {
            try
            {
                string endpointUrl = @"<change to real cosmos db url>";
               
                //string endpoint = configuration["EndPointUrl"];
                string endpoint = endpointUrl;
                if (string.IsNullOrEmpty(endpoint))
                {
                    throw new ArgumentNullException("Please specify a valid endpoint in the appSettings.json");
                }
                string authKey = configuration["AuthorizationKey"];
                if (string.IsNullOrEmpty(authKey) || string.Equals(authKey, "Super secret key"))
                {
                    throw new ArgumentException("Please specify a valid AuthorizationKey in the appSettings.json");
                }

                //Read the Cosmos endpointUrl and authorisationKeys from configuration
                //These values are available from the Azure Management Portal on the Cosmos Account Blade under "Keys"
                //NB > Keep these values in a safe & secure location. Together they provide Administrative access to your Cosmos account
                using (CosmosClient client = new CosmosClient(endpoint, authKey))
                {

                    await Program.RunDemoAsync(client, di);
                }
            }
            catch (CosmosException cre)
            {
                Console.WriteLine(cre.ToString());
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                //Console.ReadKey();
            }

            var result = new TelemetryResult();
            return result;

            

            


        }
    }

    class DataRetriever
    {
        public static List<WeatherData1> jsonList;
        public static string ConnectionString;
        public static List<WeatherData1> GetData(string connectionString)
        {
            var tns = GetTableNames(connectionString);
            var stationData = PullData(tns, connectionString);
            jsonList = ConverToJson(stationData, tns);
            ConnectionString = connectionString;
            return jsonList;
        }


        static List<List<StationDatas>> PullData(List<string> tableNames, string connectionString)
        {
            List<List<StationDatas>> result = new List<List<StationDatas>>();
            string cs = @"server=localhost;userid=dbuser;password=s$cret;database=testdb";

            cs = connectionString;// @"Server=pocmysql.mysql.database.azure.com;UserID=PoCAdminSQL;Password=WQZ2c6sQmtH3i6r;Database=lndb";

            using var con = new MySqlConnection(cs);

            foreach (var tableName in tableNames)
            {
                if (
                    (tableName.ToLower() != "stations")
                    && (tableName.ToLower() != "air_humidity")
                    && (tableName.ToLower() != "atmos_pressure")
                    && (tableName.ToLower() != "pavement")
                && (tableName.ToLower() != "precipitation")
                && (tableName.ToLower() != "snow")
                && (tableName.ToLower() != "wind")
                    )
                {
                    var data = GetDataFromTable(con, tableName);
                    result.Add(data);
                }


            }
            return result;
        }

        static List<StationDatas> GetDataFromTable(MySqlConnection con, string tableName)
        {
            var result = new List<StationDatas>();
            if (tableName != "stations")
            {
                con.Open();
                //Console.WriteLine($"MySQL version : {con.ServerVersion}");
                string sql = "SELECT * FROM weather." + tableName + " order by RecNum desc limit 1";
                using var cmd = new MySqlCommand(sql, con);
                using MySqlDataReader rdr = cmd.ExecuteReader();
                if (tableName.Contains("humidity"))
                {
                    var ah_list = GetAirHumidityList(con, rdr);
                    result = ah_list;
                }
                if (tableName.Contains("pressure"))
                {
                    var ah_list = GetAtmosPressureList(con, rdr);
                    result = ah_list;
                }
                if (tableName.Contains("pavement"))
                {
                    var ah_list = GetPavementList(con, rdr);
                    result = ah_list;
                }
                if (tableName.Contains("precipitation"))
                {
                    var ah_list = GetPrecipitationList(con, rdr);
                    result = ah_list;
                }
                if (tableName.Contains("snow"))
                {

                }
                if (tableName.Contains("wind"))
                {
                    var ah_list = GetWindList(con, rdr);
                    result = ah_list;
                }

            }

            con.Close();
            return result;
        }

        static List<StationDatas> GetAirHumidityList(MySqlConnection con, MySqlDataReader rdr)
        {
            string tmStamp;
            string recNum;
            int stationId;
            float identifier;
            float maxAirTemp1;
            float curAirTemp1;
            float minAirTemp1;
            float airTempQ;
            float airTemp2;
            float airTemp2Q;
            float rh;
            float dewPoint;
            List<StationDatas> result = new List<StationDatas>();
            try
            {
                while (rdr.Read())
                {
                    tmStamp = rdr.GetString(0);
                    recNum = rdr.GetString(1);
                    stationId = rdr.GetInt32(2);
                    identifier = rdr.GetFloat(3);
                    maxAirTemp1 = rdr.GetFloat(4);
                    curAirTemp1 = rdr.GetFloat(5);
                    minAirTemp1 = rdr.GetFloat(6);
                    airTempQ = rdr.GetFloat(7);
                    airTemp2 = rdr.GetFloat(8);
                    airTemp2Q = rdr.GetFloat(9);
                    rh = rdr.GetFloat(10);
                    dewPoint = rdr.GetFloat(11);

                    var airHumid = new AirHumidities();
                    airHumid.deviceId = rdr.GetInt32(2).ToString();
                    airHumid.measurements = new AirHumidityMeasurements();

                    airHumid.measurements.TmStamp = new List<string>();
                    airHumid.measurements.TmStamp.Add(rdr.GetString(0));

                    airHumid.measurements.RecNum = new List<string>();
                    airHumid.measurements.RecNum.Add(rdr.GetString(1));



                    airHumid.measurements.Identifier = new List<float>();
                    airHumid.measurements.Identifier.Add(rdr.GetFloat(3));

                    airHumid.measurements.MaxAirTemp1 = new List<float>();
                    airHumid.measurements.MaxAirTemp1.Add(rdr.GetFloat(4));

                    airHumid.measurements.CurAirTemp1 = new List<float>();
                    airHumid.measurements.CurAirTemp1.Add(rdr.GetFloat(5));

                    airHumid.measurements.MinAirTemp1 = new List<float>();
                    airHumid.measurements.MinAirTemp1.Add(rdr.GetFloat(6));

                    airHumid.measurements.AirTempQ = new List<float>();
                    airHumid.measurements.AirTempQ.Add(rdr.GetFloat(7));

                    airHumid.measurements.AirTemp2 = new List<float>();
                    airHumid.measurements.AirTemp2.Add(rdr.GetFloat(8));

                    airHumid.measurements.AirTemp2Q = new List<float>();
                    airHumid.measurements.AirTemp2Q.Add(rdr.GetFloat(9));

                    airHumid.measurements.RH = new List<float>();
                    airHumid.measurements.RH.Add(rdr.GetFloat(10));

                    airHumid.measurements.Dew_Point = new List<float>();
                    airHumid.measurements.Dew_Point.Add(rdr.GetFloat(11));



                    result.Add(airHumid);

                    //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", tmStamp, recNum,
                    //        stationId, identifier, maxAirTemp1, curAirTemp1, minAirTemp1, airTempQ, airTemp2, airTemp2Q, rh, dewPoint);
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                //rdr.Close();
            }
            return result;

        }

        static List<StationDatas> GetAtmosPressureList(MySqlConnection con, MySqlDataReader rdr)
        {
            string tmStamp;
            string recNum;
            int stationId;
            float identifier;
            float atmPressure;

            List<StationDatas> result = new List<StationDatas>();
            try
            {
                while (rdr.Read())
                {
                    tmStamp = rdr.GetString(0);
                    recNum = rdr.GetString(1);
                    stationId = rdr.GetInt32(2);
                    try
                    {
                        identifier = rdr.GetFloat(3);
                        atmPressure = rdr.GetFloat(4);

                    }
                    catch(Exception exc1)
                    {
                        Console.WriteLine(exc1.Message);
                        identifier = 0;
                        atmPressure = rdr.GetFloat(6);

                    }
                    
                    
                    var atmosPressure = new AtmosPressures();
                    atmosPressure.deviceId = rdr.GetInt32(2).ToString();

                    atmosPressure.measurements = new AtmosPressureMeasurements();


                    atmosPressure.measurements.TmStamp = new List<string>();
                    atmosPressure.measurements.TmStamp.Add(rdr.GetString(0));

                    atmosPressure.measurements.RecNum = new List<string>();
                    atmosPressure.measurements.RecNum.Add(rdr.GetString(1));



                    atmosPressure.measurements.Identifier = new List<float>();
                    atmosPressure.measurements.Identifier.Add(identifier);

                    atmosPressure.measurements.AtmPressure = new List<float>();
                    atmosPressure.measurements.AtmPressure.Add(atmPressure);


                    result.Add(atmosPressure);

                    //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", tmStamp, recNum,
                    //        stationId, identifier, maxAirTemp1, curAirTemp1, minAirTemp1, airTempQ, airTemp2, airTemp2Q, rh, dewPoint);
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                //rdr.Close();
            }
            return result;

        }

        static List<StationDatas> GetPavementList(MySqlConnection con, MySqlDataReader rdr)
        {
            string tmStamp;
            string recNum;
            int stationId;
            float identifier;


            float pvmntTemp1;
            float pavementQ;
            float altPaveTemp1;
            float frzPntTemp1;
            float frzPntTemp1Q;
            float pvmnCond1;
            float pvmntCond1Q;
            float sbAsphltTemp;
            float pvBaseTemp1;
            float pvBaseTemp1Q;
            float pvmntSrfCvTh;
            float pvmntSrfCvThQ;

            List<StationDatas> result = new List<StationDatas>();
            try
            {
                while (rdr.Read())
                {
                    tmStamp = rdr.GetString(0);
                    recNum = rdr.GetString(1);
                    stationId = rdr.GetInt32(2);
                    identifier = rdr.GetFloat(3);

                    pvmntTemp1 = rdr.GetFloat(4);
                    pavementQ = rdr.GetFloat(5);
                    altPaveTemp1 = rdr.GetFloat(6);
                    frzPntTemp1 = rdr.GetFloat(7);
                    frzPntTemp1Q = rdr.GetFloat(8);
                    pvmnCond1 = rdr.GetFloat(9);
                    pvmntCond1Q = rdr.GetFloat(10);
                    sbAsphltTemp = rdr.GetFloat(11);
                    pvBaseTemp1 = rdr.GetFloat(12);
                    pvBaseTemp1Q = rdr.GetFloat(13);
                    pvmntSrfCvTh = rdr.GetFloat(14);
                    pvmntSrfCvThQ = rdr.GetFloat(15);

                    var airHumid = new Pavements();

                    airHumid.deviceId = rdr.GetInt32(2).ToString();
                    airHumid.measurements = new PavementMeasurements();

                    airHumid.measurements.TmStamp = new List<string>();
                    airHumid.measurements.TmStamp.Add(rdr.GetString(0));

                    airHumid.measurements.RecNum = new List<string>();
                    airHumid.measurements.RecNum.Add(rdr.GetString(1));



                    airHumid.measurements.Identifier = new List<float>();
                    airHumid.measurements.Identifier.Add(rdr.GetFloat(3));

                    airHumid.measurements.PvmntTemp1 = new List<float>();
                    airHumid.measurements.PvmntTemp1.Add(rdr.GetFloat(4));

                    airHumid.measurements.PavementQ = new List<float>();
                    airHumid.measurements.PavementQ.Add(rdr.GetFloat(5));

                    airHumid.measurements.AltPaveTemp1 = new List<float>();
                    airHumid.measurements.AltPaveTemp1.Add(rdr.GetFloat(6));

                    airHumid.measurements.FrzPntTemp1 = new List<float>();
                    airHumid.measurements.FrzPntTemp1.Add(rdr.GetFloat(7));

                    airHumid.measurements.FrzPntTemp1Q = new List<float>();
                    airHumid.measurements.FrzPntTemp1Q.Add(rdr.GetFloat(8));

                    airHumid.measurements.PvmnCond1 = new List<float>();
                    airHumid.measurements.PvmnCond1.Add(rdr.GetFloat(9));

                    airHumid.measurements.PvmntCond1Q = new List<float>();
                    airHumid.measurements.PvmntCond1Q.Add(rdr.GetFloat(10));

                    airHumid.measurements.SbAsphltTemp = new List<float>();
                    airHumid.measurements.SbAsphltTemp.Add(rdr.GetFloat(11));

                    airHumid.measurements.PvBaseTemp1 = new List<float>();
                    airHumid.measurements.PvBaseTemp1.Add(rdr.GetFloat(11));

                    airHumid.measurements.PvBaseTemp1Q = new List<float>();
                    airHumid.measurements.PvBaseTemp1Q.Add(rdr.GetFloat(11));

                    airHumid.measurements.PvmntSrfCvTh = new List<float>();
                    airHumid.measurements.PvmntSrfCvTh.Add(rdr.GetFloat(11));

                    airHumid.measurements.PvmntSrfCvThQ = new List<float>();
                    airHumid.measurements.PvmntSrfCvThQ.Add(rdr.GetFloat(11));


                    result.Add(airHumid);

                    //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", tmStamp, recNum,
                    //        stationId, identifier, maxAirTemp1, curAirTemp1, minAirTemp1, airTempQ, airTemp2, airTemp2Q, rh, dewPoint);
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                //rdr.Close();
            }
            return result;

        }

        static List<StationDatas> GetPrecipitationList(MySqlConnection con, MySqlDataReader rdr)
        {
            string tmStamp;
            string recNum;
            int stationId;
            float identifier;


            float gaugeTot;
            float newPrecip;
            float hrlyPrecip;
            float precipGaugeQ;
            float precipDetRatio;
            float precipDetQ;

            List<StationDatas> result = new List<StationDatas>();
            try
            {
                while (rdr.Read())
                {
                    tmStamp = rdr.GetString(0);
                    recNum = rdr.GetString(1);
                    stationId = rdr.GetInt32(2);
                    identifier = rdr.GetFloat(3);
                    gaugeTot = rdr.GetFloat(4);
                    newPrecip = rdr.GetFloat(5);
                    hrlyPrecip = rdr.GetFloat(6);
                    precipGaugeQ = rdr.GetFloat(7);
                    precipDetRatio = rdr.GetFloat(8);
                    precipDetQ = rdr.GetFloat(9);


                    var airHumid = new Precipitations();


                    airHumid.deviceId = rdr.GetInt32(2).ToString();
                    airHumid.measurements = new PrecipitationMeasurements();

                    airHumid.measurements.TmStamp = new List<string>();
                    airHumid.measurements.TmStamp.Add(rdr.GetString(0));

                    airHumid.measurements.RecNum = new List<string>();
                    airHumid.measurements.RecNum.Add(rdr.GetString(1));



                    airHumid.measurements.Identifier = new List<float>();
                    airHumid.measurements.Identifier.Add(rdr.GetFloat(3));

                    airHumid.measurements.GaugeTot = new List<float>();
                    airHumid.measurements.GaugeTot.Add(rdr.GetFloat(4));

                    airHumid.measurements.NewPrecip = new List<float>();
                    airHumid.measurements.NewPrecip.Add(rdr.GetFloat(5));

                    airHumid.measurements.HrlyPrecip = new List<float>();
                    airHumid.measurements.HrlyPrecip.Add(rdr.GetFloat(6));

                    airHumid.measurements.PrecipGaugeQ = new List<float>();
                    airHumid.measurements.PrecipGaugeQ.Add(rdr.GetFloat(7));

                    airHumid.measurements.PrecipDetRatio = new List<float>();
                    airHumid.measurements.PrecipDetRatio.Add(rdr.GetFloat(8));

                    airHumid.measurements.PrecipDetQ = new List<float>();
                    airHumid.measurements.PrecipDetQ.Add(rdr.GetFloat(9));


                    result.Add(airHumid);

                    //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", tmStamp, recNum,
                    //        stationId, identifier, maxAirTemp1, curAirTemp1, minAirTemp1, airTempQ, airTemp2, airTemp2Q, rh, dewPoint);
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                //rdr.Close();
            }
            return result;

        }

        static List<StationDatas> GetSnowList(MySqlConnection con, MySqlDataReader rdr)
        {
            string tmStamp;
            string recNum;
            int stationId;
            float identifier;
            float hS;
            float hStd;
            float hrlySnow;
            float snowQ;

            List<StationDatas> result = new List<StationDatas>();
            try
            {
                while (rdr.Read())
                {
                    tmStamp = rdr.GetString(0);
                    recNum = rdr.GetString(1);
                    stationId = rdr.GetInt32(2);
                    identifier = rdr.GetFloat(3);
                    hS = rdr.GetFloat(4);
                    hStd = rdr.GetFloat(5);
                    hrlySnow = rdr.GetFloat(6);
                    snowQ = rdr.GetFloat(7);


                    var airHumid = new Snows();

                    airHumid.deviceId = rdr.GetInt32(2).ToString();
                    airHumid.measurements = new SnowMeasurements();

                    airHumid.measurements.TmStamp = new List<string>();
                    airHumid.measurements.TmStamp.Add(rdr.GetString(0));

                    airHumid.measurements.RecNum = new List<string>();
                    airHumid.measurements.RecNum.Add(rdr.GetString(1));



                    airHumid.measurements.Identifier = new List<float>();
                    airHumid.measurements.Identifier.Add(rdr.GetFloat(3));

                    airHumid.measurements.HS = new List<float>();
                    airHumid.measurements.HS.Add(rdr.GetFloat(4));

                    airHumid.measurements.HStd = new List<float>();
                    airHumid.measurements.HStd.Add(rdr.GetFloat(5));

                    airHumid.measurements.HrlySnow = new List<float>();
                    airHumid.measurements.HrlySnow.Add(rdr.GetFloat(6));

                    airHumid.measurements.SnowQ = new List<float>();
                    airHumid.measurements.SnowQ.Add(rdr.GetFloat(7));


                    result.Add(airHumid);

                    //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", tmStamp, recNum,
                    //        stationId, identifier, maxAirTemp1, curAirTemp1, minAirTemp1, airTempQ, airTemp2, airTemp2Q, rh, dewPoint);
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                //rdr.Close();
            }
            return result;

        }

        static List<StationDatas> GetWindList(MySqlConnection con, MySqlDataReader rdr)
        {
            string tmStamp;
            string recNum;
            int stationId;
            float identifier;

            float maxWindSpd;
            float meanWindSpd;
            float windSpd;
            float windSpdQ;
            float meanWindDir;
            float stDevWind;
            float windDir;
            float derimeStat;


            List<StationDatas> result = new List<StationDatas>();
            try
            {
                while (rdr.Read())
                {
                    tmStamp = rdr.GetString(0);
                    recNum = rdr.GetString(1);
                    stationId = rdr.GetInt32(2);
                    identifier = rdr.GetFloat(3);
                    maxWindSpd = rdr.GetFloat(4);
                    meanWindSpd = rdr.GetFloat(5);
                    windSpd = rdr.GetFloat(6);
                    windSpdQ = rdr.GetFloat(7);
                    meanWindDir = rdr.GetFloat(8);
                    stDevWind = rdr.GetFloat(9);
                    windDir = rdr.GetFloat(10);
                    derimeStat = rdr.GetFloat(11);

                    var airHumid = new Winds();

                    airHumid.deviceId = rdr.GetInt32(2).ToString();
                    airHumid.measurements = new WindMeasurements();

                    airHumid.measurements.TmStamp = new List<string>();
                    airHumid.measurements.TmStamp.Add(rdr.GetString(0));

                    airHumid.measurements.RecNum = new List<string>();
                    airHumid.measurements.RecNum.Add(rdr.GetString(1));



                    airHumid.measurements.Identifier = new List<float>();
                    airHumid.measurements.Identifier.Add(rdr.GetFloat(3));

                    airHumid.measurements.MaxWindSpd = new List<float>();
                    airHumid.measurements.MaxWindSpd.Add(rdr.GetFloat(4));

                    airHumid.measurements.MeanWindSpd = new List<float>();
                    airHumid.measurements.MeanWindSpd.Add(rdr.GetFloat(5));

                    airHumid.measurements.WindSpd = new List<float>();
                    airHumid.measurements.WindSpd.Add(rdr.GetFloat(6));

                    airHumid.measurements.WindSpdQ = new List<float>();
                    airHumid.measurements.WindSpdQ.Add(rdr.GetFloat(7));

                    airHumid.measurements.MeanWindDir = new List<float>();
                    airHumid.measurements.MeanWindDir.Add(rdr.GetFloat(8));

                    airHumid.measurements.StDevWind = new List<float>();
                    airHumid.measurements.StDevWind.Add(rdr.GetFloat(9));

                    airHumid.measurements.WindDir = new List<float>();
                    airHumid.measurements.WindDir.Add(rdr.GetFloat(10));

                    airHumid.measurements.DerimeStat = new List<float>();
                    airHumid.measurements.DerimeStat.Add(rdr.GetFloat(11));


                    result.Add(airHumid);

                    //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", tmStamp, recNum,
                    //        stationId, identifier, maxAirTemp1, curAirTemp1, minAirTemp1, airTempQ, airTemp2, airTemp2Q, rh, dewPoint);
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                //rdr.Close();
            }
            return result;

        }

        public static List<string> GetTableNames(string connectionString)
        {
            string tableName = "";
            var result = new List<string>();
            //string cs = @"server=localhost;userid=dbuser;password=s$cret;database=testdb";
            //cs = @"Server=pocmysql.mysql.database.azure.com;UserID=PoCAdminSQL;Password=WQZ2c6sQmtH3i6r;Database=lndb";
            string cs = connectionString;
            using var con = new MySqlConnection(cs);
            con.Open();
            Console.WriteLine($"Get Table Names...");

            string sql = "SELECT * FROM weather.nels_irishman35094_air_humidity order by RecNum desc limit 1";


            sql = @"SELECT TABLE_NAME
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_TYPE = 'BASE TABLE' and table_schema = 'weather'";

            using var cmd = new MySqlCommand(sql, con);
            using MySqlDataReader rdr = cmd.ExecuteReader();
            try
            {
                while (rdr.Read())
                {
                    tableName = rdr.GetString(0);
                    if (!tableName.Contains("meta"))
                    {
                        result.Add(tableName);
                    }
                }
            }
            catch (Exception exe)
            {
                Console.WriteLine(exe.Message);
            }
            finally
            {
                rdr.Close();
            }
            con.Close();

            return result;
        }
        static List<WeatherData1> ConverToJson(List<List<StationDatas>> stationData, List<string> tableNames)
        {

            tableNames = tableNames.Where(x => x.ToLower() != "stations"
            && x.ToLower() != "air_humidity"
            && x.ToLower() != "atmos_pressure"
            && x.ToLower() != "pavement"
            && x.ToLower() != "precipitation"
            && x.ToLower() != "snow"
            && x.ToLower() != "wind"
            ).ToList();
            List<StationDatas> result = new List<StationDatas>();

            for (var i = 0; i < stationData.Count; i++)
            {
                if (tableNames[i].Contains("humidity"))
                {
                    var ahs = new AirHumidities();
                    ahs.measurements = new AirHumidityMeasurements();
                    
                    //ahs.device.DeviceId = "nels_irishman35094_air_humidity";
                    int stationId = 0;
                    foreach (var airHumidity in stationData[i])
                    {
                        //var tempData = airHumidity as AirHumidity;
                        //ahs.measurements.TmStamp.Add(tempData.TmStamp);
                        //ahs.measurements.RecNum.Add(tempData.RecNum);
                        //ahs.measurements.StationID.Add(tempData.StationId);
                        //ahs.measurements.Identifier.Add(tempData.Identifier);
                        //ahs.measurements.MaxAirTemp1.Add(tempData.MaxAirTemp1);
                        //ahs.measurements.CurAirTemp1.Add(tempData.CurAirTemp1);
                        //ahs.measurements.MinAirTemp1.Add(tempData.MinAirTemp1);
                        //ahs.measurements.AirTempQ.Add(tempData.AirTempQ);
                        //ahs.measurements.AirTemp2.Add(tempData.AirTemp2);
                        //ahs.measurements.AirTemp2Q.Add(tempData.AirTemp2Q);
                        //ahs.measurements.RH.Add(tempData.Rh);
                        //ahs.measurements.Dew_Point.Add(tempData.DewPoint);
                        //stationId = tempData.StationId;

                        

                        ahs = airHumidity as AirHumidities;
                        
                        stationId = Convert.ToInt32(ahs.deviceId);



                    }
                    ahs.deviceId = stationId.ToString();
                    string obj = JsonConvert.SerializeObject(ahs);

                    result.Add(ahs as StationDatas);



                }

                if (tableNames[i].Contains("pressure"))
                {
                    var ahs = new AtmosPressures();
                    ahs.measurements = new AtmosPressureMeasurements();
                    int stationId = 0;
                    foreach (var atmosPressure in stationData[i])
                    {
                        //var tempData = atmosPressure as AtmosPressure;
                        //ahs.measurements.TmStamp.Add(tempData.TmStamp);
                        //ahs.measurements.RecNum.Add(tempData.RecNum);
                        //ahs.measurements.StationID.Add(tempData.StationId);
                        //ahs.measurements.Identifier.Add(tempData.Identifier);
                        //ahs.measurements.AtmPressure.Add(tempData.AtmPressure);
                        //stationId = tempData.StationId;

                        ahs = atmosPressure as AtmosPressures;
                        stationId = Convert.ToInt32(ahs.deviceId);
                    }
                    ahs.deviceId = stationId.ToString();
                    string obj = JsonConvert.SerializeObject(ahs);

                    result.Add(ahs as StationDatas);

                }

                if (tableNames[i].Contains("pavement"))
                {
                    var ahs = new Pavements();
                    ahs.measurements = new PavementMeasurements();
                    //ahs.device.DeviceId = "nels_irishman35094_air_humidity";
                    int stationId = 0;
                    foreach (var airHumidity in stationData[i])
                    {
                        //var tempData = airHumidity as Pavement;
                        //ahs.measurements.TmStamp.Add(tempData.TmStamp);
                        //ahs.measurements.RecNum.Add(tempData.RecNum);
                        //ahs.measurements.StationID.Add(tempData.StationId);
                        //ahs.measurements.Identifier.Add(tempData.Identifier);
                        //ahs.measurements.PvmntTemp1.Add(tempData.PvmntTemp1);
                        //ahs.measurements.PavementQ.Add(tempData.PavementQ);
                        //ahs.measurements.AltPaveTemp1.Add(tempData.AltPaveTemp1);
                        //ahs.measurements.FrzPntTemp1.Add(tempData.FrzPntTemp1);
                        //ahs.measurements.FrzPntTemp1Q.Add(tempData.FrzPntTemp1Q);
                        //ahs.measurements.PvmnCond1.Add(tempData.PvmnCond1);
                        //ahs.measurements.PvmntCond1Q.Add(tempData.PvmntCond1Q);
                        //ahs.measurements.SbAsphltTemp.Add(tempData.SbAsphltTemp);
                        //ahs.measurements.PvBaseTemp1.Add(tempData.PvBaseTemp1);
                        //ahs.measurements.PvBaseTemp1Q.Add(tempData.PvBaseTemp1Q);
                        //ahs.measurements.PvmntSrfCvTh.Add(tempData.PvmntSrfCvTh);
                        //ahs.measurements.PvmntSrfCvThQ.Add(tempData.PvmntSrfCvThQ);
                        //stationId = tempData.StationId;

                        ahs = airHumidity as Pavements;
                        stationId = Convert.ToInt32(ahs.deviceId);
                    }
                    ahs.deviceId = stationId.ToString();
                    string obj = JsonConvert.SerializeObject(ahs);

                    result.Add(ahs as StationDatas);

                }

                if (tableNames[i].Contains("precipitation"))
                {
                    var ahs = new Precipitations();
                    ahs.measurements = new PrecipitationMeasurements();
                    //ahs.device.DeviceId = "nels_irishman35094_air_humidity";
                    int stationId = 0;
                    foreach (var airHumidity in stationData[i])
                    {
                        //var tempData = airHumidity as Precipitation;
                        //ahs.measurements.TmStamp.Add(tempData.TmStamp);
                        //ahs.measurements.RecNum.Add(tempData.RecNum);
                        //ahs.measurements.StationID.Add(tempData.StationId);
                        //ahs.measurements.Identifier.Add(tempData.Identifier);
                        //ahs.measurements.GaugeTot.Add(tempData.GaugeTot);
                        //ahs.measurements.NewPrecip.Add(tempData.NewPrecip);
                        //ahs.measurements.HrlyPrecip.Add(tempData.HrlyPrecip);
                        //ahs.measurements.PrecipGaugeQ.Add(tempData.PrecipGaugeQ);
                        //ahs.measurements.PrecipDetRatio.Add(tempData.PrecipDetRatio);
                        //ahs.measurements.PrecipDetQ.Add(tempData.PrecipDetQ);

                        //stationId = tempData.StationId;

                        ahs = airHumidity as Precipitations;
                        stationId = Convert.ToInt32(ahs.deviceId);
                    }
                    ahs.deviceId = stationId.ToString();
                    string obj = JsonConvert.SerializeObject(ahs);

                    result.Add(ahs as StationDatas);

                }

                if (tableNames[i].Contains("snow"))
                {
                    var ahs = new Snows();
                    ahs.measurements = new SnowMeasurements();
                    //ahs.device.DeviceId = "nels_irishman35094_air_humidity";
                    int stationId = 0;
                    foreach (var airHumidity in stationData[i])
                    {
                        //var tempData = airHumidity as Snow;
                        //ahs.measurements.TmStamp.Add(tempData.TmStamp);
                        //ahs.measurements.RecNum.Add(tempData.RecNum);
                        //ahs.measurements.StationID.Add(tempData.StationId);
                        //ahs.measurements.Identifier.Add(tempData.Identifier);
                        //ahs.measurements.HS.Add(tempData.HS);
                        //ahs.measurements.HStd.Add(tempData.HStd);
                        //ahs.measurements.HrlySnow.Add(tempData.HrlySnow);
                        //ahs.measurements.SnowQ.Add(tempData.SnowQ);

                        //stationId = tempData.StationId;

                        ahs = airHumidity as Snows;
                        stationId = Convert.ToInt32(ahs.deviceId);
                    }
                    ahs.deviceId = stationId.ToString();
                    string obj = JsonConvert.SerializeObject(ahs);

                    result.Add(ahs as StationDatas);

                }

                if (tableNames[i].Contains("wind"))
                {
                    var ahs = new Winds();
                    ahs.measurements = new WindMeasurements();
                    //ahs.device.DeviceId = "nels_irishman35094_air_humidity";
                    int stationId = 0;
                    foreach (var airHumidity in stationData[i])
                    {
                        //var tempData = airHumidity as Wind;
                        //ahs.measurements.TmStamp.Add(tempData.TmStamp);
                        //ahs.measurements.RecNum.Add(tempData.RecNum);
                        //ahs.measurements.StationID.Add(tempData.StationId);
                        //ahs.measurements.Identifier.Add(tempData.Identifier);
                        //ahs.measurements.MaxWindSpd.Add(tempData.MaxWindSpd);
                        //ahs.measurements.MeanWindSpd.Add(tempData.MeanWindSpd);
                        //ahs.measurements.WindSpd.Add(tempData.WindSpd);
                        //ahs.measurements.WindSpdQ.Add(tempData.WindSpdQ);
                        //ahs.measurements.MeanWindDir.Add(tempData.MeanWindDir);
                        //ahs.measurements.StDevWind.Add(tempData.StDevWind);
                        //ahs.measurements.WindDir.Add(tempData.WindDir);
                        //ahs.measurements.DerimeStat.Add(tempData.DerimeStat);

                        //stationId = tempData.StationId;

                        ahs = airHumidity as Winds;
                        stationId = Convert.ToInt32(ahs.deviceId);
                    }
                    ahs.deviceId = stationId.ToString();
                    string obj = JsonConvert.SerializeObject(ahs);

                    result.Add(ahs as StationDatas);

                }



            }

             int count = (tableNames.Count) / 6;
            List<WeatherData> wdl = new List<WeatherData>();
            List<WeatherData1> wdl1 = new List<WeatherData1>();
            for (var i = 0; i < result.Count; i += 6)
            {
                WeatherData wd = new WeatherData();
                WeatherData1 wd1 = new WeatherData1();

                wd.airHumidity = result[i] as StationDatas;
                wd.atmosPressure = result[i + 1] as StationDatas;
                wd.pavement = result[i + 2] as StationDatas;
                wd.precipitation = result[i + 3] as StationDatas;
                wd.snow = result[i + 4] as StationDatas;
                wd.wind = result[i + 5] as StationDatas;



                var temp = wd.airHumidity as AirHumidities;
                wd1.deviceId = temp.deviceId;
                wd1.measurements = new Measurements();
                wd1.measurements.timestamp = temp.measurements.TmStamp[0];
                wd1.measurements.maxAirTemp = temp.measurements.MaxAirTemp1[0];
                wd1.measurements.currentAirTemp = temp.measurements.CurAirTemp1[0];
                wd1.measurements.minAirTemp = temp.measurements.MinAirTemp1[0];
                wd1.measurements.airTempQuality = temp.measurements.AirTempQ[0];
                wd1.measurements.airTempAlternate = temp.measurements.AirTemp2[0];
                wd1.measurements.airTempAlternateQuality = temp.measurements.AirTemp2Q[0];
                wd1.measurements.relativeHumidity = temp.measurements.RH[0];
                wd1.measurements.dewPoint = temp.measurements.Dew_Point[0];

                var temp1 = wd.atmosPressure as AtmosPressures;
                if(temp1.measurements.AtmPressure.Count()>0)
                {
                    wd1.measurements.atmospherePressure = temp1.measurements.AtmPressure[0];

                }
                


                var temp2 = wd.pavement as Pavements;

                wd1.measurements.pavementTemp = temp2.measurements.PvmntTemp1[0];
                wd1.measurements.pavementTempQuality = temp2.measurements.PavementQ[0];
                wd1.measurements.alternatePavementTemp = temp2.measurements.AltPaveTemp1[0];
                wd1.measurements.freezePointTemp = temp2.measurements.FrzPntTemp1[0];
                wd1.measurements.freezePointTempQuality = temp2.measurements.FrzPntTemp1Q[0];
                wd1.measurements.pavementCondition = temp2.measurements.PvmnCond1[0];
                wd1.measurements.pavementConditionQuality = temp2.measurements.PvmntCond1Q[0];
                wd1.measurements.subAsphaltTemp = temp2.measurements.SbAsphltTemp[0];
                wd1.measurements.pavementBaseTemp = temp2.measurements.PvBaseTemp1[0];
                wd1.measurements.pavementBaseTempQuality = temp2.measurements.PvBaseTemp1Q[0];
                wd1.measurements.pavementSurfaceConductivity = temp2.measurements.PvmntSrfCvTh[0];
                wd1.measurements.pavementSurfaceConductivityQuality = temp2.measurements.PvmntSrfCvThQ[0];


                var temp3 = wd.wind as Winds;
                wd1.measurements.maxWindSpeed = temp3.measurements.MaxWindSpd[0];
                wd1.measurements.meanWindSpeed = temp3.measurements.MeanWindSpd[0];
                wd1.measurements.windSpeed = temp3.measurements.WindSpd[0];
                wd1.measurements.windSpeedQuality = temp3.measurements.WindSpdQ[0];
                wd1.measurements.meanWindDirection = temp3.measurements.MeanWindDir[0];
                wd1.measurements.standardWindDeviation = temp3.measurements.StDevWind[0];
                wd1.measurements.windDirection = temp3.measurements.WindDir[0];
                wd1.timestamp = GetJavascriptTimestamp(temp.measurements.TmStamp[0]);

                wdl.Add(wd);
                wdl1.Add(wd1);
            }



            //return wdl;
            return wdl1;



        }


        static double GetJavascriptTimestamp(string datetime)
        {
            DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime dt = DateTime.Parse(datetime); ;
            TimeSpan duration = dt.Subtract(UnixEpoch);
            return duration.TotalMilliseconds;

        }



    }


    public class AirHumidity : StationData
    {
        public string TmStamp;
        public string RecNum;
        public int StationId;
        public float Identifier;
        public float MaxAirTemp1;
        public float CurAirTemp1;
        public float MinAirTemp1;
        public float AirTempQ;
        public float AirTemp2;
        public float AirTemp2Q;
        public float Rh;
        public float DewPoint;
    }

    public class AtmosPressure : StationData
    {
        public string TmStamp;
        public string RecNum;
        public int StationId;
        public float Identifier;
        public float AtmPressure;
    }

    public class Pavement : StationData
    {
        public string TmStamp;
        public string RecNum;
        public int StationId;
        public float Identifier;
        public float PvmntTemp1;
        public float PavementQ;
        public float AltPaveTemp1;
        public float FrzPntTemp1;
        public float FrzPntTemp1Q;
        public float PvmnCond1;
        public float PvmntCond1Q;
        public float SbAsphltTemp;
        public float PvBaseTemp1;
        public float PvBaseTemp1Q;
        public float PvmntSrfCvTh;
        public float PvmntSrfCvThQ;
    }

    public class Precipitation : StationData
    {
        public string TmStamp;
        public string RecNum;
        public int StationId;
        public float Identifier;
        public float GaugeTot;
        public float NewPrecip;
        public float HrlyPrecip;
        public float PrecipGaugeQ;
        public float PrecipDetRatio;
        public float PrecipDetQ;
    }

    public class Snow : StationData
    {
        public string TmStamp;
        public string RecNum;
        public int StationId;
        public float Identifier;
        public float HS;
        public float HStd;
        public float HrlySnow;
        public float SnowQ;
    }

    public class Wind : StationData
    {
        public string TmStamp;
        public string RecNum;
        public int StationId;
        public float Identifier;
        public float MaxWindSpd;
        public float MeanWindSpd;
        public float WindSpd;
        public float WindSpdQ;
        public float MeanWindDir;
        public float StDevWind;
        public float WindDir;
        public float DerimeStat;
    }


    public class AirHumidities : StationDatas
    {
        public string deviceId;
        public AirHumidityMeasurements measurements;

    }

    public class AtmosPressures : StationDatas
    {
        public string deviceId;
        public AtmosPressureMeasurements measurements;

    }

    public class Pavements : StationDatas
    {
        public string deviceId;
        public PavementMeasurements measurements;

    }

    public class Precipitations : StationDatas
    {
        public string deviceId;
        public PrecipitationMeasurements measurements;

    }

    public class Snows : StationDatas
    {
        public string deviceId;
        public SnowMeasurements measurements;

    }

    public class Winds : StationDatas
    {
        public string deviceId;
        public WindMeasurements measurements;

    }


    public class MessageBody
    {
        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; set; }




    }

    public class AirHumidityMessageBody : MessageBody
    {
        [JsonProperty(PropertyName = "measurements")]
        public AirHumidityMeasurements Measurements { get; set; }

    }

    public class AtmosPressureMessageBody : MessageBody
    {
        [JsonProperty(PropertyName = "measurements")]
        public AtmosPressureMeasurements Measurements { get; set; }

    }

    public class PavementMessageBody : MessageBody
    {
        [JsonProperty(PropertyName = "measurements")]
        public PavementMeasurements Measurements { get; set; }

    }

    public class PrecipitationMessageBody : MessageBody
    {
        [JsonProperty(PropertyName = "measurements")]
        public PrecipitationMeasurements Measurements { get; set; }

    }

    public class SnowMessageBody : MessageBody
    {
        [JsonProperty(PropertyName = "measurements")]
        public SnowMeasurements Measurements { get; set; }

    }

    public class WindMessageBody : MessageBody
    {
        [JsonProperty(PropertyName = "measurements")]
        public WindMeasurements Measurements { get; set; }

    }





    public class AirHumidityMeasurements
    {
        [JsonProperty(PropertyName = "tmStamp")]
        public List<string> TmStamp = new List<string>();
        [JsonProperty(PropertyName = "recNum")]
        public List<string> RecNum = new List<string>();
        [JsonProperty(PropertyName = "stationId")]
        public List<int> StationID = new List<int>();
        [JsonProperty(PropertyName = "identifier")]
        public List<float> Identifier = new List<float>();
        [JsonProperty(PropertyName = "maxAirTemp1")]
        public List<float> MaxAirTemp1 = new List<float>();
        [JsonProperty(PropertyName = "curAirTemp1")]
        public List<float> CurAirTemp1 = new List<float>();
        [JsonProperty(PropertyName = "minAirTemp1")]
        public List<float> MinAirTemp1 = new List<float>();
        [JsonProperty(PropertyName = "airTempQ")]
        public List<float> AirTempQ = new List<float>();
        [JsonProperty(PropertyName = "airTemp2")]
        public List<float> AirTemp2 = new List<float>();
        [JsonProperty(PropertyName = "airTemp2Q")]
        public List<float> AirTemp2Q = new List<float>();
        [JsonProperty(PropertyName = "rh")]
        public List<float> RH = new List<float>();
        [JsonProperty(PropertyName = "dewPoint")]
        public List<float> Dew_Point = new List<float>();
    }

    public class AtmosPressureMeasurements
    {
        [JsonProperty(PropertyName = "tmStamp")]
        public List<string> TmStamp = new List<string>();
        [JsonProperty(PropertyName = "recNum")]
        public List<string> RecNum = new List<string>();
        [JsonProperty(PropertyName = "stationId")]
        public List<int> StationID = new List<int>();
        [JsonProperty(PropertyName = "identifier")]
        public List<float> Identifier = new List<float>();
        [JsonProperty(PropertyName = "atmPressure")]
        public List<float> AtmPressure = new List<float>();

    }

    public class PavementMeasurements
    {
        [JsonProperty(PropertyName = "tmStamp")]
        public List<string> TmStamp = new List<string>();
        [JsonProperty(PropertyName = "recNum")]
        public List<string> RecNum = new List<string>();
        [JsonProperty(PropertyName = "stationId")]
        public List<int> StationID = new List<int>();
        [JsonProperty(PropertyName = "identifier")]
        public List<float> Identifier = new List<float>();
        [JsonProperty(PropertyName = "pvmntTemp1")]
        public List<float> PvmntTemp1 = new List<float>();
        [JsonProperty(PropertyName = "pavementQ")]
        public List<float> PavementQ = new List<float>();
        [JsonProperty(PropertyName = "altPaveTemp1")]
        public List<float> AltPaveTemp1 = new List<float>();
        [JsonProperty(PropertyName = "frzPntTemp1")]
        public List<float> FrzPntTemp1 = new List<float>();
        [JsonProperty(PropertyName = "frzPntTemp1Q")]
        public List<float> FrzPntTemp1Q = new List<float>();
        [JsonProperty(PropertyName = "pvmnCond1")]
        public List<float> PvmnCond1 = new List<float>();
        [JsonProperty(PropertyName = "pvmntCond1Q")]
        public List<float> PvmntCond1Q = new List<float>();
        [JsonProperty(PropertyName = "sbAsphltTemp")]
        public List<float> SbAsphltTemp = new List<float>();
        [JsonProperty(PropertyName = "pvBaseTemp1")]
        public List<float> PvBaseTemp1 = new List<float>();
        [JsonProperty(PropertyName = "pvBaseTemp1Q")]
        public List<float> PvBaseTemp1Q = new List<float>();
        [JsonProperty(PropertyName = "pvmntSrfCvTh")]
        public List<float> PvmntSrfCvTh = new List<float>();
        [JsonProperty(PropertyName = "pvmntSrfCvThQ")]
        public List<float> PvmntSrfCvThQ = new List<float>();
    }

    public class PrecipitationMeasurements
    {
        [JsonProperty(PropertyName = "tmStamp")]
        public List<string> TmStamp = new List<string>();
        [JsonProperty(PropertyName = "recNum")]
        public List<string> RecNum = new List<string>();
        [JsonProperty(PropertyName = "stationId")]
        public List<int> StationID = new List<int>();
        [JsonProperty(PropertyName = "identifier")]
        public List<float> Identifier = new List<float>();
        [JsonProperty(PropertyName = "gaugeTot")]
        public List<float> GaugeTot = new List<float>();
        [JsonProperty(PropertyName = "newPrecip")]
        public List<float> NewPrecip = new List<float>();
        [JsonProperty(PropertyName = "hrlyPrecip")]
        public List<float> HrlyPrecip = new List<float>();
        [JsonProperty(PropertyName = "precipGaugeQ")]
        public List<float> PrecipGaugeQ = new List<float>();
        [JsonProperty(PropertyName = "precipDetRatio")]
        public List<float> PrecipDetRatio = new List<float>();
        [JsonProperty(PropertyName = "precipDetQ")]
        public List<float> PrecipDetQ = new List<float>();

    }

    public class SnowMeasurements
    {
        [JsonProperty(PropertyName = "tmStamp")]
        public List<string> TmStamp = new List<string>();
        [JsonProperty(PropertyName = "recNum")]
        public List<string> RecNum = new List<string>();
        [JsonProperty(PropertyName = "stationId")]
        public List<int> StationID = new List<int>();
        [JsonProperty(PropertyName = "identifier")]
        public List<float> Identifier = new List<float>();
        [JsonProperty(PropertyName = "hS")]
        public List<float> HS = new List<float>();
        [JsonProperty(PropertyName = "hStd")]
        public List<float> HStd = new List<float>();
        [JsonProperty(PropertyName = "hrlySnow")]
        public List<float> HrlySnow = new List<float>();
        [JsonProperty(PropertyName = "nnowQ")]
        public List<float> SnowQ = new List<float>();

    }

    public class WindMeasurements
    {
        [JsonProperty(PropertyName = "tmStamp")]
        public List<string> TmStamp = new List<string>();
        [JsonProperty(PropertyName = "recNum")]
        public List<string> RecNum = new List<string>();
        [JsonProperty(PropertyName = "stationId")]
        public List<int> StationID = new List<int>();
        [JsonProperty(PropertyName = "identifier")]
        public List<float> Identifier = new List<float>();
        [JsonProperty(PropertyName = "maxWindSpd")]
        public List<float> MaxWindSpd = new List<float>();
        [JsonProperty(PropertyName = "meanWindSpd")]
        public List<float> MeanWindSpd = new List<float>();
        [JsonProperty(PropertyName = "windSpd")]
        public List<float> WindSpd = new List<float>();
        [JsonProperty(PropertyName = "windSpdQ")]
        public List<float> WindSpdQ = new List<float>();
        [JsonProperty(PropertyName = "meanWindDir")]
        public List<float> MeanWindDir = new List<float>();
        [JsonProperty(PropertyName = "stDevWind")]
        public List<float> StDevWind = new List<float>();
        [JsonProperty(PropertyName = "windDir")]
        public List<float> WindDir = new List<float>();
        [JsonProperty(PropertyName = "derimeStat")]
        public List<float> DerimeStat = new List<float>();
    }

    public class Measurements
    {
        [JsonProperty(PropertyName = "timestamp")]
        public string timestamp;

        [JsonProperty(PropertyName = "maxAirTemp")]
        public float maxAirTemp;

        [JsonProperty(PropertyName = "currentAirTemp")]
        public float currentAirTemp;

        [JsonProperty(PropertyName = "minAirTemp")]
        public float minAirTemp;

        [JsonProperty(PropertyName = "airTempQuality")]
        public float airTempQuality;


        [JsonProperty(PropertyName = "airTempAlternate")]
        public float airTempAlternate;



        [JsonProperty(PropertyName = "airTempAlternateQuality")]
        public float airTempAlternateQuality;


        [JsonProperty(PropertyName = "relativeHumidity")]
        public float relativeHumidity;


        [JsonProperty(PropertyName = "dewPoint")]
        public float dewPoint;



        [JsonProperty(PropertyName = "atmospherePressure")]
        public float atmospherePressure;


        [JsonProperty(PropertyName = "pavementTemp")]
        public float pavementTemp;




        [JsonProperty(PropertyName = "pavementTempQuality")]
        public float pavementTempQuality;



        [JsonProperty(PropertyName = "alternatePavementTemp")]
        public float alternatePavementTemp;


        [JsonProperty(PropertyName = "freezePointTemp")]
        public float freezePointTemp;



        [JsonProperty(PropertyName = "freezePointTempQuality")]
        public float freezePointTempQuality;


        [JsonProperty(PropertyName = "pavementCondition")]
        public float pavementCondition;


        [JsonProperty(PropertyName = "pavementConditionQuality")]
        public float pavementConditionQuality;


        [JsonProperty(PropertyName = "subAsphaltTemp")]
        public float subAsphaltTemp;


        [JsonProperty(PropertyName = "pavementBaseTemp")]
        public float pavementBaseTemp;


        [JsonProperty(PropertyName = "pavementBaseTempQuality")]
        public float pavementBaseTempQuality;


        [JsonProperty(PropertyName = "pavementSurfaceConductivity")]
        public float pavementSurfaceConductivity;


        [JsonProperty(PropertyName = "pavementSurfaceConductivityQuality")]
        public float pavementSurfaceConductivityQuality;


        [JsonProperty(PropertyName = "maxWindSpeed")]
        public float maxWindSpeed;

        [JsonProperty(PropertyName = "meanWindSpeed")]
        public float meanWindSpeed;


        [JsonProperty(PropertyName = "windSpeed")]
        public float windSpeed;


        [JsonProperty(PropertyName = "windSpeedQuality")]
        public float windSpeedQuality;


        [JsonProperty(PropertyName = "meanWindDirection")]
        public float meanWindDirection;

        [JsonProperty(PropertyName = "standardWindDeviation")]
        public float standardWindDeviation;

        [JsonProperty(PropertyName = "windDirection")]
        public float windDirection;



    }


    public interface StationData { }

    public interface StationDatas { }

    public class WeatherData
    {
        public StationDatas airHumidity;
        public StationDatas atmosPressure;
        public StationDatas pavement;
        public StationDatas precipitation;
        public StationDatas snow;
        public StationDatas wind;

        public string deviceId;
        public Measurements measurements;
    }

    public class WeatherData1
    {
        public string id;
        public string deviceId;
        public double timestamp;
        public Measurements measurements;

        
    }
}
