using System.Net.Http.Headers;

namespace IotApis.HttpClients
{
    public static class HttpClientsServiceCollectionExtensions
    {
        public static void AddHttpClients(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient<IIotCentralApi, IotCentralApi>(client =>
            {
                client.BaseAddress = new Uri(config.GetValue<string>("IotCentral:Url"));
                client.Timeout = GetTimeout(config, "IotCentral:Timeout");
                client.DefaultRequestHeaders.Clear();
                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", config.GetValue<string>("IotCentral:Sas"));
            });
        }

        private static TimeSpan GetTimeout(IConfiguration config, string section)
        {
            var seconds = config.GetValue<int>(section);
            if (seconds <= 0)
            {
                Console.WriteLine($"Config - Invalid {section} value: {seconds}");
                seconds = 90;
            }

            return TimeSpan.FromSeconds(seconds);
        }
    }
}
