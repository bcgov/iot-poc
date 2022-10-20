using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using ServiceReference3;
using OnvifCamera.CustomUsernameToken;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Threading.Tasks;


namespace OnvifCamera
{
    public sealed class CameraManager : IDisposable
    {
        private readonly CameraLogger _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private DeviceClient _deviceClient;
        private MediaCapabilities _mediaCapabilities;
        private ImagingCapabilities _imagingCapabilities;
        private int _disposed;
        private TimeOnDevice _timeOnDevice;
        public string Host;
        public string User;
        public string Pass;

        public bool IsHttpDigestSupported { get; private set; }

        private CameraManager() => this._logger = CameraLoggerConfig.CreateCameraLoggerConfig().CreateLogger((object)this, typeof(CameraManager).ToString());

        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref this._disposed, 1) != 0 || !disposing)
                return;
            this._lock?.Dispose();
            this._logger?.Dispose();
            this._deviceClient?.Close();
        }

        private async Task InitializeCamera(Uri uri, string username, string password)
        {
            this._deviceClient = OnvifFactory.CreateOnvifObj<DeviceClient>(uri);
            OnvifFactory.SetCredentials(username, password, this._deviceClient.ClientCredentials);
            try
            {

                var deviceInfo = await this._deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest());


                DeviceServiceCapabilities serviceCapabilities = await this._deviceClient.GetServiceCapabilitiesAsync().ConfigureAwait(false);
                this.IsHttpDigestSupported = serviceCapabilities.Security.HttpDigestSpecified && serviceCapabilities.Security.HttpDigest;
            }
            catch (CommunicationException ex)
            {
                this._logger.LogObjectState(LogLevel.Warning, "GetServiceCapabilitiesAsync Error:", (object)null, (object)CameraLogger.GetExceptionString((Exception)ex));
                throw;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Warning, "GetServiceCapabilitiesAsync Error:", (object)null, (object)CameraLogger.GetExceptionString(ex));
            }
            if (!this.IsHttpDigestSupported)
            {
                this.ResetDeviceClientWithWSToken(uri, username, password);
                try
                {
                    SystemDateTime systemDateTime = await this._deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false);
                    this._logger.LogObjectState(LogLevel.Debug, "GetAndConvertTime - GetSystemDateAndTimeAsync", (object)null, (object)systemDateTime);
                    Tuple<System.DateTime, string> tuple = systemDateTime != null ? CameraManager.ConvertTime(systemDateTime, true) : throw new InvalidDataException();
                    System.DateTime dateTime = tuple.Item1;
                    if (dateTime.Kind == DateTimeKind.Unspecified)
                    {
                        CultureInfo invariantCulture = CultureInfo.InvariantCulture;
                        dateTime = tuple.Item1;
                        string longTimeString = dateTime.ToLongTimeString();
                        throw new InvalidDataException(string.Format((IFormatProvider)invariantCulture, "Device does not provide time or only localtime: {0}", (object)longTimeString));
                    }
                    this._timeOnDevice = new TimeOnDevice(tuple.Item1);
                }
                catch (CommunicationException ex)
                {
                    this._logger.LogObjectState(LogLevel.Warning, "GetSystemDateAndTimeAsync Error:", (object)null, (object)CameraLogger.GetExceptionString((Exception)ex));
                    throw;
                }
                catch (Exception ex)
                {
                    this._logger.LogObjectState(LogLevel.Warning, "Setup WS-TokenAuth GetSystemDateAndTimeAsync Error; Will use Client UTC time:", (object)null, (object)CameraLogger.GetExceptionString(ex));
                    this._timeOnDevice = new TimeOnDevice(System.DateTime.UtcNow);
                }
                this.ResetDeviceClientWithWSToken(uri, username, password);
            }
            await this.UpdateCapabilities().ConfigureAwait(false);
        }

        private void ResetDeviceClientWithWSToken(Uri uri, string username, string password)
        {
            this._deviceClient = OnvifFactory.CreateOnvifObj<DeviceClient>(uri);
            OnvifFactory.SetCredentials(username, password, this._deviceClient.ClientCredentials);
            this._deviceClient.Endpoint.EndpointBehaviors.Add((IEndpointBehavior)new WsSecurityEndpointBehavior(username, password, this._timeOnDevice));
        }

        private async Task UpdateCapabilities()
        {
            GetCapabilitiesResponse output = await this._deviceClient.GetCapabilitiesAsync(new CapabilityCategory[1]).ConfigureAwait(false);
            this._logger.LogObjectState(LogLevel.Debug, "UpdateCapabilities - GetCapabilitiesAsync", (object)new CapabilityCategory[1], (object)output);
            if (output.Capabilities.Media != null)
                this._mediaCapabilities = output.Capabilities.Media;
            if (output.Capabilities.Imaging == null)
                return;
            this._imagingCapabilities = output.Capabilities.Imaging;
        }

        ~CameraManager() => this.Dispose(false);




        public static async Task<CameraManager> CreateCameraManager(
          Uri uri,
          string username = null,
          string password = null)
        {
            CameraManager cm = new CameraManager();
            await cm.InitializeCamera(uri, username, password).ConfigureAwait(false);
            CameraManager cameraManager = cm;
            cm = (CameraManager)null;
            return cameraManager;
        }

        public async Task<ServiceReference3.NetworkZeroConfiguration[]> GetZeroConfigAsync()
        {
            List<ServiceReference3.NetworkZeroConfiguration> zeroConfigList = new List<ServiceReference3.NetworkZeroConfiguration>();
            try
            {
                ServiceReference3.NetworkZeroConfiguration zeroConfiguration1 = await this._deviceClient.GetZeroConfigurationAsync().ConfigureAwait(false);
                if (zeroConfiguration1 != null)
                {
                    zeroConfigList.Add((ServiceReference3.NetworkZeroConfiguration)zeroConfiguration1);
                    if (zeroConfiguration1.Extension?.Additional != null)
                    {
                        foreach (ServiceReference3.NetworkZeroConfiguration zeroConfiguration2 in zeroConfiguration1.Extension.Additional)
                            zeroConfigList.Add((ServiceReference3.NetworkZeroConfiguration)zeroConfiguration2);
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetZeroConfigAsync), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            ServiceReference3.NetworkZeroConfiguration[] array = zeroConfigList.ToArray();
            zeroConfigList = (List<ServiceReference3.NetworkZeroConfiguration>)null;
            return array;
        }

        public async Task SetZeroConfigAsync(string interfaceToken, bool enable)
        {
            try
            {
                await this._deviceClient.SetZeroConfigurationAsync(interfaceToken, enable).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetZeroConfigAsync), (object)(interfaceToken, enable), (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        private static Tuple<System.DateTime, string> ConvertTime(
          SystemDateTime onvifTime,
          bool fRequestsUTC = false)
        {
            System.DateTime dateTime = System.DateTime.Now;
            ref System.DateTime local1 = ref dateTime;
            int? nullable1 = onvifTime?.LocalDateTime?.Date?.Year;
            int year1 = nullable1 ?? 1;
            nullable1 = onvifTime?.LocalDateTime?.Date?.Month;
            int month1 = nullable1 ?? 1;
            nullable1 = onvifTime?.LocalDateTime?.Date?.Day;
            int day1 = nullable1 ?? 1;
            int? nullable2;
            if (onvifTime == null)
            {
                nullable1 = new int?();
                nullable2 = nullable1;
            }
            else
            {
                ServiceReference3.DateTime localDateTime = onvifTime.LocalDateTime;
                if (localDateTime == null)
                {
                    nullable1 = new int?();
                    nullable2 = nullable1;
                }
                else
                {
                    Time time = localDateTime.Time;
                    if (time == null)
                    {
                        nullable1 = new int?();
                        nullable2 = nullable1;
                    }
                    else
                        nullable2 = new int?(time.Hour);
                }
            }
            nullable1 = nullable2;
            int valueOrDefault1 = nullable1.GetValueOrDefault();
            int? nullable3;
            if (onvifTime == null)
            {
                nullable1 = new int?();
                nullable3 = nullable1;
            }
            else
            {
                ServiceReference3.DateTime localDateTime = onvifTime.LocalDateTime;
                if (localDateTime == null)
                {
                    nullable1 = new int?();
                    nullable3 = nullable1;
                }
                else
                {
                    Time time = localDateTime.Time;
                    if (time == null)
                    {
                        nullable1 = new int?();
                        nullable3 = nullable1;
                    }
                    else
                        nullable3 = new int?(time.Minute);
                }
            }
            nullable1 = nullable3;
            int valueOrDefault2 = nullable1.GetValueOrDefault();
            int? nullable4;
            if (onvifTime == null)
            {
                nullable1 = new int?();
                nullable4 = nullable1;
            }
            else
            {
                ServiceReference3.DateTime localDateTime = onvifTime.LocalDateTime;
                if (localDateTime == null)
                {
                    nullable1 = new int?();
                    nullable4 = nullable1;
                }
                else
                {
                    Time time = localDateTime.Time;
                    if (time == null)
                    {
                        nullable1 = new int?();
                        nullable4 = nullable1;
                    }
                    else
                        nullable4 = new int?(time.Second);
                }
            }
            nullable1 = nullable4;
            int valueOrDefault3 = nullable1.GetValueOrDefault();
            local1 = new System.DateTime(year1, month1, day1, valueOrDefault1, valueOrDefault2, valueOrDefault3, DateTimeKind.Unspecified);
            string str = onvifTime?.TimeZone?.TZ ?? (string)null;
            if (onvifTime != null && onvifTime.UTCDateTime != null && onvifTime.LocalDateTime == null | fRequestsUTC)
            {
                ref System.DateTime local2 = ref dateTime;
                nullable1 = onvifTime?.UTCDateTime?.Date?.Year;
                int year2 = nullable1 ?? 1;
                nullable1 = onvifTime?.UTCDateTime?.Date?.Month;
                int month2 = nullable1 ?? 1;
                nullable1 = onvifTime?.UTCDateTime?.Date?.Day;
                int day2 = nullable1 ?? 1;
                int? nullable5;
                if (onvifTime == null)
                {
                    nullable1 = new int?();
                    nullable5 = nullable1;
                }
                else
                {
                    ServiceReference3.DateTime utcDateTime = onvifTime.UTCDateTime;
                    if (utcDateTime == null)
                    {
                        nullable1 = new int?();
                        nullable5 = nullable1;
                    }
                    else
                    {
                        Time time = utcDateTime.Time;
                        if (time == null)
                        {
                            nullable1 = new int?();
                            nullable5 = nullable1;
                        }
                        else
                            nullable5 = new int?(time.Hour);
                    }
                }
                nullable1 = nullable5;
                int valueOrDefault4 = nullable1.GetValueOrDefault();
                int? nullable6;
                if (onvifTime == null)
                {
                    nullable1 = new int?();
                    nullable6 = nullable1;
                }
                else
                {
                    ServiceReference3.DateTime utcDateTime = onvifTime.UTCDateTime;
                    if (utcDateTime == null)
                    {
                        nullable1 = new int?();
                        nullable6 = nullable1;
                    }
                    else
                    {
                        Time time = utcDateTime.Time;
                        if (time == null)
                        {
                            nullable1 = new int?();
                            nullable6 = nullable1;
                        }
                        else
                            nullable6 = new int?(time.Minute);
                    }
                }
                nullable1 = nullable6;
                int valueOrDefault5 = nullable1.GetValueOrDefault();
                int? nullable7;
                if (onvifTime == null)
                {
                    nullable1 = new int?();
                    nullable7 = nullable1;
                }
                else
                {
                    ServiceReference3.DateTime utcDateTime = onvifTime.UTCDateTime;
                    if (utcDateTime == null)
                    {
                        nullable1 = new int?();
                        nullable7 = nullable1;
                    }
                    else
                    {
                        Time time = utcDateTime.Time;
                        if (time == null)
                        {
                            nullable1 = new int?();
                            nullable7 = nullable1;
                        }
                        else
                            nullable7 = new int?(time.Second);
                    }
                }
                nullable1 = nullable7;
                int valueOrDefault6 = nullable1.GetValueOrDefault();
                local2 = new System.DateTime(year2, month2, day2, valueOrDefault4, valueOrDefault5, valueOrDefault6, DateTimeKind.Utc);
                str = "GMT";
            }
            return new Tuple<System.DateTime, string>(dateTime, str);
        }

        public async Task<DeviceDateTime> GetCameraTime()
        {
            DeviceDateTime cameraTime;
            try
            {
                cameraTime = new DeviceDateTime();
                SystemDateTime onvifTime = await this._deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetCameraTime - GetSystemDateAndTimeAsync", (object)null, (object)onvifTime);
                Tuple<System.DateTime, string> tuple = CameraManager.ConvertTime(onvifTime);
                cameraTime.Time = tuple.Item1;
                cameraTime.TimeZone = tuple.Item2;
                SystemDateTime systemDateTime1 = onvifTime;
                if ((systemDateTime1 != null ? (int)systemDateTime1.DateTimeType : 0) == 0)
                {
                    cameraTime.NTPEnabled = false;
                    cameraTime.NTPSource = (IReadOnlyList<NetworkConfig>)null;
                }
                else
                {
                    cameraTime.NTPEnabled = true;
                    NTPInformation output = await this._deviceClient.GetNTPAsync().ConfigureAwait(false);
                    this._logger.LogObjectState(LogLevel.Debug, "GetCameraTime - GetNTPAsync", (object)null, (object)output);
                    cameraTime.DHCPEnabled = output.FromDHCP;
                    cameraTime.NTPSource = !output.FromDHCP ? GetNetworkConfig(output.NTPManual) : GetNetworkConfig(output.NTPFromDHCP);
                }
                 DeviceDateTime local =  cameraTime;
                SystemDateTime systemDateTime2 = onvifTime;
                int num = systemDateTime2 != null ? (systemDateTime2.DaylightSavings ? 1 : 0) : 0;
                local.DaylightSavingsEnabled = num != 0;
                onvifTime = (SystemDateTime)null;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetCameraTime), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            DeviceDateTime cameraTime1 = cameraTime;
            cameraTime = new DeviceDateTime();
            return cameraTime1;

            static IReadOnlyList<NetworkConfig> GetNetworkConfig(
              NetworkHost[] hosts)
            {
                List<NetworkConfig> networkConfigList = new List<NetworkConfig>();
                if (hosts != null)
                {
                    foreach (NetworkHost host in hosts)
                    {
                        NetworkConfig networkConfig = new NetworkConfig();
                        networkConfig.DnsName = host.DNSname ?? (string)null;
                        networkConfig.IPAddresses = host?.IPv4Address ?? host?.IPv6Address ?? (string)null;
                        if (!string.IsNullOrEmpty(networkConfig.DnsName) || !string.IsNullOrEmpty(networkConfig.IPAddresses))
                            networkConfigList.Add(networkConfig);
                    }
                }
                return networkConfigList.Count <= 0 ? (IReadOnlyList<NetworkConfig>)null : (IReadOnlyList<NetworkConfig>)networkConfigList.AsReadOnly();
            }
        }

        public async Task SetCameraTime(DeviceDateTime timeToSet)
        {
            ServiceReference3.TimeZone timeZone = (ServiceReference3.TimeZone)null;
            ServiceReference3.DateTime onvifDateTime = (ServiceReference3.DateTime)null;
            try
            {
                if (!string.IsNullOrEmpty(timeToSet.TimeZone))
                {
                    timeZone = new ServiceReference3.TimeZone();
                    timeZone.TZ = timeToSet.TimeZone;
                }
                SetDateTimeType timeType;
                if (timeToSet.NTPEnabled)
                {
                    timeType = SetDateTimeType.NTP;
                    NetworkHost[] onvifNtp = timeToSet.DHCPEnabled ? (NetworkHost[])null : GetNetworkHosts(timeToSet.NTPSource);
                    SetNTPResponse setNtpResponse = await this._deviceClient.SetNTPAsync(timeToSet.DHCPEnabled, onvifNtp).ConfigureAwait(false);
                    this._logger.LogObjectState(LogLevel.Debug, "SetCameraTime - SetNTPAsync", (object)(timeToSet.DHCPEnabled, onvifNtp), (object)null);
                    onvifNtp = (NetworkHost[])null;
                }
                else
                {
                    timeType = SetDateTimeType.Manual;
                    System.DateTime time1 = timeToSet.Time;
                    onvifDateTime = new ServiceReference3.DateTime()
                    {
                        Time = new Time(),
                        Date = new Date()
                    };
                    onvifDateTime.Date.Day = timeToSet.Time.Day;
                    onvifDateTime.Date.Month = timeToSet.Time.Month;
                    onvifDateTime.Date.Year = timeToSet.Time.Year;
                    Time time2 = onvifDateTime.Time;
                    System.DateTime time3 = timeToSet.Time;
                    int hour = time3.Hour;
                    time2.Hour = hour;
                    Time time4 = onvifDateTime.Time;
                    time3 = timeToSet.Time;
                    int minute = time3.Minute;
                    time4.Minute = minute;
                    Time time5 = onvifDateTime.Time;
                    time3 = timeToSet.Time;
                    int second = time3.Second;
                    time5.Second = second;
                }
                await this._deviceClient.SetSystemDateAndTimeAsync(timeType, timeToSet.DaylightSavingsEnabled, timeZone, onvifDateTime).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "SetCameraTime - SetSystemDateAndTimeAsync", (object)(timeType, timeToSet.DaylightSavingsEnabled, timeZone, onvifDateTime), (object)null);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetCameraTime), (object)timeToSet, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            timeZone = (ServiceReference3.TimeZone)null;
            onvifDateTime = (ServiceReference3.DateTime)null;

            static NetworkHost[] GetNetworkHosts(IReadOnlyList<NetworkConfig> networkConfigs)
            {
                List<NetworkHost> networkHostList = new List<NetworkHost>();
                if (networkConfigs != null)
                {
                    foreach (NetworkConfig networkConfig in (IEnumerable<NetworkConfig>)networkConfigs)
                    {
                        NetworkHost networkHost = new NetworkHost();
                        if (string.IsNullOrEmpty(networkConfig.DnsName))
                        {
                            System.Net.IPAddress address;
                            if (System.Net.IPAddress.TryParse(networkConfig.IPAddresses, out address))
                            {
                                switch (address.AddressFamily)
                                {
                                    case AddressFamily.InterNetwork:
                                        networkHost.IPv4Address = networkConfig.IPAddresses;
                                        networkHost.Type = NetworkHostType.IPv4;
                                        break;
                                    case AddressFamily.InterNetworkV6:
                                        networkHost.IPv6Address = networkConfig.IPAddresses;
                                        networkHost.Type = NetworkHostType.IPv6;
                                        break;
                                }
                            }
                        }
                        else
                        {
                            networkHost.DNSname = networkConfig.DnsName;
                            networkHost.Type = NetworkHostType.DNS;
                        }
                        if (!string.IsNullOrEmpty(networkHost.DNSname) || !string.IsNullOrEmpty(networkHost.IPv4Address) || !string.IsNullOrEmpty(networkHost.IPv6Address))
                            networkHostList.Add(networkHost);
                    }
                }
                return networkHostList.Count <= 0 ? (NetworkHost[])null : networkHostList.ToArray();
            }
        }

        public async Task SetDNS(DNSInfo dnsInfo)
        {
            try
            {
                ServiceReference3.IPAddress[] deviceDnsIpAddresses = ConvertToDeviceDNSIpAddresses(dnsInfo.DNSIPAddress.ToArray<string>());
                DeviceClient deviceClient = this._deviceClient;
                int num = dnsInfo.DHCPEnabled ? 1 : 0;
                IReadOnlyList<string> searchDomain = dnsInfo.SearchDomain;
                string[] SearchDomain = searchDomain != null ? searchDomain.ToArray<string>() : (string[])null;
                ServiceReference3.IPAddress[] DNSManual = deviceDnsIpAddresses;
                SetDNSResponse setDnsResponse = await deviceClient.SetDNSAsync(num != 0, SearchDomain, DNSManual).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, nameof(SetDNS), (object)dnsInfo, (object)null);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetDNS), (object)dnsInfo, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }

            static ServiceReference3.IPAddress[] ConvertToDeviceDNSIpAddresses(string[] ipAddresses)
            {
                List<ServiceReference3.IPAddress> ipAddressList = (List<ServiceReference3.IPAddress>)null;
                if (ipAddresses != null)
                {
                    ipAddressList = new List<ServiceReference3.IPAddress>();
                    foreach (string ipAddress1 in ipAddresses)
                    {
                        System.Net.IPAddress address;
                        if (System.Net.IPAddress.TryParse(ipAddress1, out address))
                        {
                            switch (address.AddressFamily)
                            {
                                case AddressFamily.InterNetwork:
                                    ServiceReference3.IPAddress ipAddress2 = new ServiceReference3.IPAddress()
                                    {
                                        IPv4Address = ipAddress1,
                                        Type = IPType.IPv4
                                    };
                                    ipAddressList.Add(ipAddress2);
                                    continue;
                                case AddressFamily.InterNetworkV6:
                                    ServiceReference3.IPAddress ipAddress3 = new ServiceReference3.IPAddress()
                                    {
                                        IPv6Address = ipAddress1,
                                        Type = IPType.IPv6
                                    };
                                    ipAddressList.Add(ipAddress3);
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                }
                return ipAddressList?.ToArray();
            }
        }

        public async Task<DNSInfo> GetDNS()
        {
            DNSInfo dnsInfo = new DNSInfo();
            try
            {
                DNSInformation output = await this._deviceClient.GetDNSAsync().ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, nameof(GetDNS), (object)null, (object)output);
                string[] searchDomain = output.SearchDomain;
                if ((searchDomain != null ? ((uint)searchDomain.Length > 0U ? 1 : 0) : 0) != 0)
                    dnsInfo.SearchDomain = (IReadOnlyList<string>)output.SearchDomain;
                if (output.FromDHCP)
                {
                    dnsInfo.DNSIPAddress = (IReadOnlyList<string>)ConvertToSystemIpAddresses(output.DNSFromDHCP);
                    dnsInfo.DHCPEnabled = true;
                }
                else
                {
                    dnsInfo.DNSIPAddress = (IReadOnlyList<string>)ConvertToSystemIpAddresses(output.DNSManual);
                    dnsInfo.DHCPEnabled = false;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetDNS), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            DNSInfo dns = dnsInfo;
            dnsInfo = new DNSInfo();
            return dns;

            static string[] ConvertToSystemIpAddresses(ServiceReference3.IPAddress[] ipAddresses)
            {
                List<string> stringList = (List<string>)null;
                if (ipAddresses != null)
                {
                    stringList = new List<string>();
                    foreach (ServiceReference3.IPAddress ipAddress in ipAddresses)
                    {
                        System.Net.IPAddress address;
                        if (System.Net.IPAddress.TryParse(ipAddress.IPv4Address, out address))
                            stringList.Add(ipAddress.IPv4Address);
                        else if (System.Net.IPAddress.TryParse(ipAddress.IPv6Address, out address))
                            stringList.Add(ipAddress.IPv6Address);
                    }
                }
                return stringList?.ToArray();
            }
        }

        public async Task<DeviceInformation> GetCameraInfo()
        {
            DeviceInformation apiDeviceInfo;
            try
            {
                apiDeviceInfo = new DeviceInformation();
                GetDeviceInformationResponse output1 = await this._deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest()).ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetCameraInfo - GetDeviceInformationAsync", (object)null, (object)output1);
                apiDeviceInfo.Firmware = output1.FirmwareVersion;
                apiDeviceInfo.HardwareId = output1.HardwareId;
                apiDeviceInfo.Manufacturer = output1.Manufacturer;
                apiDeviceInfo.Model = output1.Model;
                apiDeviceInfo.SerialNumber = output1.SerialNumber;
                HostnameInformation output2 = await this._deviceClient.GetHostnameAsync().ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetCameraInfo - GetHostnameAsync", (object)null, (object)output2);
                apiDeviceInfo.Hostname = output2.Name;
                apiDeviceInfo.DHCPEnabled = output2.FromDHCP;
                DNSInfo dnsInfo = await this.GetDNS().ConfigureAwait(false);
                apiDeviceInfo.DNSIPAddress = dnsInfo.DNSIPAddress;
                apiDeviceInfo.DHCPEnabled = dnsInfo.DHCPEnabled;
                GetNetworkInterfacesResponse output3 = await this._deviceClient.GetNetworkInterfacesAsync().ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetCameraInfo - GetNetworkInterfacesAsync", (object)null, (object)output3);
                List<string> stringList = new List<string>();
                foreach (NetworkInterface networkInterface in output3.NetworkInterfaces)
                {
                    string str = networkInterface?.Info?.HwAddress ?? (string)null;
                    if (!string.IsNullOrEmpty(str))
                        stringList.Add(str);
                }
                apiDeviceInfo.MACAddresses = stringList.Count > 0 ? (IReadOnlyList<string>)stringList.AsReadOnly() : (IReadOnlyList<string>)null;
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetCameraInfo), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            DeviceInformation cameraInfo = apiDeviceInfo;
            apiDeviceInfo = new DeviceInformation();
            return cameraInfo;
        }

        public async Task<OnvifNetworkCapabilities> GetNetworkCapabilitiesAsync()
        {
            OnvifNetworkCapabilities capabilities = new OnvifNetworkCapabilities();
            try
            {
                DiscoveryMode discoveryMode = await this._deviceClient.GetDiscoveryModeAsync().ConfigureAwait(false);
                this._logger.LogObjectState(LogLevel.Debug, "GetNetworkCapabilitiesAsync - GetDiscoveryModeAsync", (object)null, (object)null);
                capabilities.WSDDiscoveryEnabled = discoveryMode == DiscoveryMode.Discoverable;
                try
                {
                    DeviceServiceCapabilities output = await this._deviceClient.GetServiceCapabilitiesAsync().ConfigureAwait(false);
                    this._logger.LogObjectState(LogLevel.Debug, "GetNetworkCapabilitiesAsync - GetServiceCapabilitiesAsync", (object)null, (object)output);
                    if (output.Network != null)
                    {
                        capabilities.DynamicDNS = output.Network.DynDNS;
                        capabilities.DynamicDNSEnabled = output.Network.DynDNSSpecified;
                        capabilities.DHCPv6 = output.Network.DHCPv6;
                        capabilities.DHCPv6Enabled = output.Network.DHCPv6Specified;
                        capabilities.HostnameFromDHCP = output.Network.HostnameFromDHCP;
                        capabilities.IPVersion6 = output.Network.IPVersion6;
                        capabilities.IPVersion6Enabled = output.Network.IPVersion6Specified;
                        capabilities.NTPCount = output.Network.NTP;
                        capabilities.NTPEnabled = output.Network.NTPSpecified;
                        capabilities.ZeroConfiguration = output.Network.ZeroConfiguration;
                        capabilities.ZeroConfigurationEnabled = output.Network.ZeroConfigurationSpecified;
                    }
                }
                catch (Exception ex) when (ex is FaultException || ex is ProtocolException)
                {
                    GetCapabilitiesResponse output = await this._deviceClient.GetCapabilitiesAsync(new CapabilityCategory[1]
                    {
            CapabilityCategory.Device
                    }).ConfigureAwait(false);
                    this._logger.LogObjectState(LogLevel.Debug, "GetNetworkCapabilitiesAsync - GetCapabilitiesAsync", (object)new CapabilityCategory[1], (object)output);
                    if (output.Capabilities.Device != null)
                    {
                        if (output.Capabilities.Device.Network != null)
                        {
                            capabilities.DynamicDNS = output.Capabilities.Device.Network.DynDNS;
                            capabilities.DynamicDNSEnabled = output.Capabilities.Device.Network.DynDNSSpecified;
                            capabilities.IPVersion6 = output.Capabilities.Device.Network.IPVersion6;
                            capabilities.IPVersion6Enabled = output.Capabilities.Device.Network.IPVersion6Specified;
                            capabilities.ZeroConfiguration = output.Capabilities.Device.Network.ZeroConfiguration;
                            capabilities.ZeroConfigurationEnabled = output.Capabilities.Device.Network.ZeroConfigurationSpecified;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(GetNetworkCapabilitiesAsync), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return capabilities;
        }

        public async Task<string> RebootAsync()
        {
            string str;
            try
            {
                str = await this._deviceClient.SystemRebootAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(RebootAsync), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return str;
        }

        public async Task SetFactoryDefaultAsync(FactoryDefaultType type)
        {
            try
            {
                await this._deviceClient.SetSystemFactoryDefaultAsync(type == FactoryDefaultType.Hard ? ServiceReference3.FactoryDefaultType.Hard : ServiceReference3.FactoryDefaultType.Soft).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(SetFactoryDefaultAsync), (object)type.ToString(), (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public MediaClientController CreateMediaController()
        {
            try
            {
                return MediaClientController.CreateMediaClientController(new Uri(this._mediaCapabilities.XAddr), this._deviceClient.ClientCredentials.UserName.UserName, this._deviceClient.ClientCredentials.UserName.Password, this.IsHttpDigestSupported, this._timeOnDevice);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(CreateMediaController), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
        }

        public bool IsImagingServiceSupported() => this._imagingCapabilities != null && !string.IsNullOrEmpty(this._imagingCapabilities.XAddr);

        public async Task<ImagingClientController> CreateImagingControllerAsync(
          string videosourcetoken)
        {
            ImagingClientController imagingControllerAsync;
            try
            {
                if (this._imagingCapabilities == null)
                    throw new InvalidOperationException("Imaging Service is unsupported");
                imagingControllerAsync = await ImagingClientController.CreateImagingClientController(new Uri(this._imagingCapabilities.XAddr), this._deviceClient.ClientCredentials.UserName.UserName, this._deviceClient.ClientCredentials.UserName.Password, videosourcetoken, this.IsHttpDigestSupported, this._timeOnDevice).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogObjectState(LogLevel.Error, nameof(CreateImagingControllerAsync), (object)null, (object)CameraLogger.GetExceptionString(ex));
                throw;
            }
            return imagingControllerAsync;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }
    }
}

