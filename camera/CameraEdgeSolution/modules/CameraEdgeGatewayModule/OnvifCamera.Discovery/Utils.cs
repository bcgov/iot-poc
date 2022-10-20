// Decompiled with JetBrains decompiler
// Type: OnvifCamera.Discovery.Utils
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace OnvifCamera.Discovery
{
    internal class Utils
    {
        private static XNamespace xns = (XNamespace)"http://schemas.xmlsoap.org/ws/2005/04/discovery";
        private static XNamespace wsa = (XNamespace)"http://schemas.xmlsoap.org/ws/2004/08/addressing";

        public static IPAddress randAddress(bool v4)
        {
            byte[] numArray = new byte[v4 ? 4 : 16];
            new Random().NextBytes(numArray);
            return new IPAddress(numArray);
        }

        public static string DeviceMsg()
        {
            string str = "</wsa:MessageID></soap:Header><soap:Body><wsd:Probe><wsd:Types>wsdp:Device</wsd:Types></wsd:Probe></soap:Body></soap:Envelope>";
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap = \"http://www.w3.org/2003/05/soap-envelope\" xmlns:wsa=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\" xmlns:wsd=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:wsdp=\"http://schemas.xmlsoap.org/ws/2006/02/devprof\" > <soap:Header><wsa:To>urn:schemas-xmlsoap-org:ws:2005:04:discovery </wsa:To><wsa:Action> http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</wsa:Action><wsa:MessageID>" + "uuid:" + Guid.NewGuid().ToString() + str;
        }

        public static string NVTMsg()
        {
            string str = "</a:MessageID><a:ReplyTo><a:Address>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</a:Address></a:ReplyTo><a:To s:mustUnderstand=\"1\">urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To></s:Header><s:Body><Probe xmlns=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\"><d:Types xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:dp0=\"http://www.onvif.org/ver10/network/wsdl\">dp0:NetworkVideoTransmitter</d:Types></Probe></s:Body></s:Envelope>";
            return "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\"><s:Header><a:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action><a:MessageID>" + "uuid:" + Guid.NewGuid().ToString() + str;
        }

        public static int SendProbeMsg(ref UdpClient client, ref IPEndPoint ipEndPoint, string Msg)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Msg);
            return client.Send(bytes, bytes.Length, ipEndPoint);
        }

        public static string PrependAdapterAddress(string address, string message) => string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Adapter Address: {0}: {1}", (object)address, (object)message);

        public static bool BytesEqual(byte[] a1, byte[] a2) => a1.GetHashCode() == a2.GetHashCode();

        public static bool IpAddressIsEqual(IPAddress ip1, IPAddress ip2) => ip1.AddressFamily == ip2.AddressFamily && (ip1.AddressFamily != AddressFamily.InterNetworkV6 || ip1.ScopeId == ip2.ScopeId) && Utils.BytesEqual(ip1.GetAddressBytes(), ip2.GetAddressBytes());

        public static int GetIPAddressInterfaceIndex(IPAddress ipAddress)
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                foreach (IPAddressInformation unicastAddress in ipProperties.UnicastAddresses)
                {
                    if (Utils.IpAddressIsEqual(unicastAddress.Address, ipAddress))
                        return ipAddress.AddressFamily == AddressFamily.InterNetwork ? ipProperties.GetIPv4Properties().Index : ipProperties.GetIPv6Properties().Index;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(ipAddress), "ipAddress is not an address for any local interface");
        }

        public static ScopeAttributeItem[] ExtractScopes(byte[] payload)
        {
            using (XmlReader reader = XmlReader.Create((Stream)new MemoryStream(payload), (XmlReaderSettings)null))
            {
                XElement xelement = XElement.Load(reader);
                XNamespace xnamespace = (XNamespace)"http://schemas.xmlsoap.org/ws/2005/04/discovery";
                if (xelement.Descendants(xnamespace + "Scopes").Any<XElement>())
                {
                    IEnumerable<string> source = xelement.Descendants(xnamespace + "Scopes").Where<XElement>((Func<XElement, bool>)(item => item != null)).Select<XElement, string>((Func<XElement, string>)(item => (string)item));
                    if (source != null)
                    {
                        Regex scopeNonOnvif = new Regex("^(?!.*.*onvif.*org.*).*");
                        Regex scopeRegex = new Regex("^.*onvif.*org/(?<name>[^/]*)/(?<token>\\b[A-Za-z0-9\\-._~\\%]+\\b)?$");
                        return ((IEnumerable<ScopeAttributeItem>)((IEnumerable<string>)source.First<string>().Split(' ')).Select(item => new
                        {
                            item = item,
                            matches = scopeRegex.Matches(item)
                        }).Where(_param1 => _param1.matches.Count > 0).Select(_param1 => new ScopeAttributeItem()
                        {
                            Full = _param1.item,
                            Token = _param1.matches.Select<Match, string>((Func<Match, string>)(match => match.Groups["name"].Value)).First<string>(),
                            Name = _param1.matches.Select<Match, string>((Func<Match, string>)(match => match.Groups["token"].Value)).First<string>()
                        }).ToArray<ScopeAttributeItem>()).Concat<ScopeAttributeItem>((IEnumerable<ScopeAttributeItem>)((IEnumerable<string>)source.First<string>().Split(' ')).Select(item => new
                        {
                            item = item,
                            matches = scopeNonOnvif.Matches(item)
                        }).Where(_param1 => _param1.matches.Count > 0).Select(_param1 => new ScopeAttributeItem()
                        {
                            Full = _param1.item,
                            Token = (string)null,
                            Name = (string)null
                        }).Where<ScopeAttributeItem>((Func<ScopeAttributeItem, bool>)(u => !string.IsNullOrEmpty(u.Full))).ToList<ScopeAttributeItem>()).ToArray<ScopeAttributeItem>();
                    }
                }
            }
            return (ScopeAttributeItem[])null;
        }

        public static Tuple<string, string, string> ScanProbeResponse(
          string address,
          byte[] ProbeResponse)
        {
            string str1 = (string)null;
            string str2 = (string)null;
            XmlReader reader = XmlReader.Create((Stream)new MemoryStream(ProbeResponse), (XmlReaderSettings)null);
            XElement xelement = XElement.Load(reader);
            if (xelement.HasElements && xelement.Descendants(Utils.xns + "ProbeMatches").Any<XElement>())
            {
                IEnumerable<string> source = xelement.Descendants(Utils.xns + "XAddrs").Select<XElement, string>((Func<XElement, string>)(item => (string)item));
                str1 = source.Any<string>() ? source.First<string>() : (string)null;
            }
            if (xelement.HasElements && xelement.Descendants(Utils.wsa + "EndpointReference").Any<XElement>())
            {
                IEnumerable<string> source = xelement.Descendants(Utils.wsa + "Address").Select<XElement, string>((Func<XElement, string>)(item => (string)item));
                str2 = source.Any<string>() ? source.First<string>() : (string)null;
            }
            reader.Close();
            return str1 != null || str2 != null ? new Tuple<string, string, string>(address, str1, str2) : (Tuple<string, string, string>)null;
        }

        public static Tuple<string, string, string> ScanHelloResponse(
          string address,
          byte[] HelloResponse)
        {
            string str1 = (string)null;
            string str2 = (string)null;
            XmlReader reader = XmlReader.Create((Stream)new MemoryStream(HelloResponse), (XmlReaderSettings)null);
            XElement xelement = XElement.Load(reader);
            if (xelement.HasElements && xelement.Descendants(Utils.xns + "Hello").Any<XElement>())
            {
                IEnumerable<string> source1 = xelement.Descendants(Utils.xns + "XAddrs").Select<XElement, string>((Func<XElement, string>)(item => (string)item));
                str1 = source1.Any<string>() ? source1.First<string>() : (string)null;
                IEnumerable<string> source2 = xelement.Descendants(Utils.wsa + "Address").Select<XElement, string>((Func<XElement, string>)(item => (string)item));
                str2 = source2.Any<string>() ? source2.First<string>() : (string)null;
            }
            reader.Close();
            return str1 != null || str2 != null ? new Tuple<string, string, string>(address, str1, str2) : (Tuple<string, string, string>)null;
        }

        public sealed class Logger
        {
            private readonly CameraLogger _logger;
            public static readonly Utils.Logger Instance = new Utils.Logger();

            private Logger() => this._logger = CameraLoggerConfig.CreateCameraLoggerConfig().CreateLogger((object)this, typeof(DiscoveryRequest).ToString());

            public CameraLogger Log => this._logger;
        }
    }
}
