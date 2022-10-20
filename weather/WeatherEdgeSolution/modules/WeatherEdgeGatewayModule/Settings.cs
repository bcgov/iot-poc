namespace Weather
{
    // Database settings
    public class DatabaseSettings
    {
        // MySQL Database connection string
        public string ConnectionString { get; set; } = "";
    }

    // IoT Central application settings
    public class IoTCentralSettings
    {
        // DPS global endpoint
        public string GlobalDeviceEndpoint { get; set; } = "global.azure-devices-provisioning.net";
        
        // IoT Central application DPS IDScope. You can get it from IoT Central -> Administration -> Device connection -> ID Scope
        public string IDScope { get; set; } = "";

        // IoT Central application group SAS key. You can get it from IoT Central -> Administration -> Device connection -> Enrollment groups -> SAS-IoT-Devices -> Primary Key
        public string GroupSASKey { get; set; } = "";

        // Device template model ID. You can get it from IoT Central -> Device Templates -> your device template -> Edit Identity -> Interface @id
        public string ModelID { get; set; } = "";
    }

    // Data generator settings
    public class DataGeneratorSettings
    {
        // Is the data generator enabled
        public bool Enabled { get; set; } = false;

        // How often (in seconds) should the data generator generate data?
        public int GenerationInterval { get; set; } = 60;

        // Number of weather stations to generate data for
        public int StationCount { get; set; } = 5;//300;
    }

    // Gateway settings
    public class GatewaySettings
    {
        // Is the gateway enabled
        public bool Enabled { get; set; } = true;

        // How often (in seconds) should the gateway scan for telemetry in the database to be sent data to IoT Central?
        public int RefreshInterval { get; set; } = 60;

        // Number of concurrent connections to be opened to IoT Central
        public int ConcurrentConnectionLimit { get; set; } = 100;

        // Number of concurrent telemetry messages to be sent to IoT Central
        public int ConcurrentMessageLimit { get; set; } = 100;
    }

    // Overall application settings
    public class Settings
    {
        // Database settings
        public DatabaseSettings Database { get; set; } = new DatabaseSettings();

        // IoT Central Application settings
        public IoTCentralSettings IoTCentral { get; set; } = new IoTCentralSettings();

        // Data generator settings
        public DataGeneratorSettings DataGenerator { get; set; } = new DataGeneratorSettings();

        // Gateway settings
        public GatewaySettings Gateway { get; set; } = new GatewaySettings();

    }
}
