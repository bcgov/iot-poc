namespace IotCommon
{
    public static class Constants
    {
        public const string VancouverTimeZone = "America/Vancouver";
        public const string PacificTimeZone = "Pacific Standard Time";

        public const string WeatherDB = "WeatherDB";
        public const string WeatherDataContainer = "WeatherDataContainer";
        public const string WeatherDBConnString = "Weather";

        public const string CameraDB = "CameraDB";
        public const string CameraDataContainer = "CameraDataContainer";
        public const string CameraDBConnString = "Camera";

        public const string BlobConnString = "Blob";
    }

    public enum Position
    {
        Top, Bottom
    }
}
