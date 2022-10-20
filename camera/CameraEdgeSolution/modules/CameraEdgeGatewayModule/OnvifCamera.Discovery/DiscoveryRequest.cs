// Decompiled with JetBrains decompiler
// Type: OnvifCamera.Discovery.DiscoveryRequest
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OnvifCamera.Discovery
{
    internal class DiscoveryRequest
    {
        private const int WSAEADDRINUSE = 10048;
        private const string xaddrPattern = "http[s]?://.*?device_service";
        private const string remoteIpaddrPattern = "[^:]*";
        private IPAddress m_address;
        private const string ipv4WsdMulticastAddress = "239.255.255.250";
        private const string ipv6WsdMulticastAddress = "FF02::C";
        private const int eventTimeOut = 300;
        private const int multiCastPort = 3702;

        public DiscoveryRequest(IPAddress address)
        {
            this.m_address = address;
            Utils.Logger.Instance.Log.Log(LogLevel.Information, "Running Probe on Adapter:" + this.m_address.ToString());
        }

        private static Thread ExcuteHelloAsyncInternal(
          UdpClient helloWaiter,
          object hashLock,
          Hashtable helloList,
          IProgress<DiscoveryResponse> progress)
        {
            Thread thread = new Thread((ThreadStart)(() =>
            {
                try
                {
                    while (true)
                    {
                        UdpReceiveResult result = helloWaiter.ReceiveAsync().Result;
                        Tuple<string, string, string> tuple = Utils.ScanHelloResponse(result.RemoteEndPoint.ToString(), result.Buffer);
                        string remoteAddress = tuple?.Item1;
                        string input = tuple?.Item2;
                        string Uuid = tuple?.Item3;
                        object obj = hashLock;
                        bool lockTaken = false;
                        try
                        {
                            Monitor.Enter(obj, ref lockTaken);
                            if (input != null)
                            {
                                if (Regex.IsMatch(input, ".*device_service.*"))
                                {
                                    if (!helloList.ContainsKey((object)input.GetHashCode(StringComparison.CurrentCultureIgnoreCase)))
                                    {
                                        string[] Xaddrs = Regex.Split(input, "http[s]?://.*?device_service", RegexOptions.IgnoreCase);
                                        DiscoveryResponse discoveryResponse = new DiscoveryResponse(remoteAddress, Xaddrs, Uuid, (ScopeAttributeItem[])null);
                                        progress?.Report(discoveryResponse);
                                        helloList.Add((object)input.GetHashCode(StringComparison.CurrentCultureIgnoreCase), (object)discoveryResponse);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (lockTaken)
                                Monitor.Exit(obj);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Utils.Logger.Instance.Log.Log(LogLevel.Error, "Exiting Hello with Native code:" + ex.NativeErrorCode.ToString() + " And " + ex.Message);
                }
                catch (ObjectDisposedException ex)
                {
                    Utils.Logger.Instance.Log.Log(LogLevel.Error, "Exiting Probe with Error code:" + ex.Message);
                }
                catch (AggregateException ex)
                {
                    ex.Handle((Func<Exception, bool>)(x => x is ObjectDisposedException));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }));
            thread.Start();
            return thread;
        }

        private static void ReceiveProbeResponse(
          UdpClient sender,
          object resultsLock,
          Hashtable hash,
          IProgress<DiscoveryResponse> progress)
        {
            new Thread((ThreadStart)(() =>
            {
                try
                {
                    while (true)
                    {
                        UdpReceiveResult result;
                        Tuple<string, string, string> tuple;
                        do
                        {
                            result = sender.ReceiveAsync().GetAwaiter().GetResult();
                        }
                        while ((tuple = Utils.ScanProbeResponse(result.RemoteEndPoint.ToString(), result.Buffer)) == null);
                        object obj = resultsLock;
                        bool lockTaken = false;
                        try
                        {
                            Monitor.Enter(obj, ref lockTaken);
                            string str = tuple?.Item1;
                            string input1 = tuple?.Item2;
                            string Uuid = tuple?.Item3;
                            if (tuple != null)
                            {
                                if (Regex.IsMatch(input1, ".*device_service.*"))
                                {
                                    if (!hash.ContainsKey((object)input1.GetHashCode(StringComparison.CurrentCultureIgnoreCase)))
                                    {
                                        string[] array = new Regex("http[s]?://.*?device_service").Matches(input1).Select<Match, string>((Func<Match, string>)(match => match.Value)).ToArray<string>();
                                        if (array.Length != 0)
                                        {
                                            Utils.Logger.Instance.Log.Log(LogLevel.Information, "Adding :" + string.Join(" and ", array));
                                            Regex regex = new Regex("[^:]*");
                                            ScopeAttributeItem[] scopes = Utils.ExtractScopes(result.Buffer);
                                            string input2 = str;
                                            MatchCollection source = regex.Matches(input2);
                                            DiscoveryResponse discoveryResponse = new DiscoveryResponse(source != null ? source.ElementAt<Match>(0).Value : (string)null, ((IEnumerable<string>)array).ToArray<string>(), Uuid, scopes);
                                            progress?.Report(discoveryResponse);
                                            hash.Add((object)input1.GetHashCode(StringComparison.CurrentCultureIgnoreCase), (object)discoveryResponse);
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (lockTaken)
                                Monitor.Exit(obj);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Utils.Logger.Instance.Log.Log(LogLevel.Error, "Exiting Probe with Native code:" + ex.NativeErrorCode.ToString() + " And " + ex.Message);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine(ex.Message);
                    Utils.Logger.Instance.Log.Log(LogLevel.Error, "Loop tries to receive on socket that has been closed:");
                    sender = (UdpClient)null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            })).Start();
        }

        public DiscoveryResponse[] Execute(
          uint timeout,
          IProgress<DiscoveryResponse> progress,
          object token)
        {
            Utils.Logger.Instance.Log.Log(LogLevel.Information, Utils.PrependAdapterAddress(this.m_address.ToString(), "Searching for cameras with: " + timeout.ToString() + " ms"));
            uint num1 = timeout > 300U ? 300U : timeout;
            Hashtable hashtable = new Hashtable();
            object obj = new object();
            UdpClient client = (UdpClient)null;
            UdpClient helloWaiter = (UdpClient)null;
            System.Timers.Timer timer = (System.Timers.Timer)null;
            Thread thread = (Thread)null;
            bool flag = false;
            AutoResetEvent eventReg = new AutoResetEvent(false);
            timeout -= timeout > num1 ? num1 : 0U;
            IPAddress ipAddress = this.m_address.AddressFamily != AddressFamily.InterNetwork ? IPAddress.Parse("FF02::C") : IPAddress.Parse("239.255.255.250");
            Utils.Logger.Instance.Log.Log(LogLevel.Debug, Utils.PrependAdapterAddress(this.m_address.ToString(), "Searching for cameras using multicast address: " + ipAddress?.ToString()));
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 3702);
            IPEndPoint localEP = new IPEndPoint(this.m_address, 0);
            try
            {
                client = new UdpClient(localEP);
                client.Client.EnableBroadcast = true;
                client.Client.MulticastLoopback = true;
                if (this.m_address.AddressFamily == AddressFamily.InterNetworkV6)
                    client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, Utils.GetIPAddressInterfaceIndex(this.m_address));
                else
                    client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, this.m_address.GetAddressBytes());
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.IpTimeToLive, true);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                client.JoinMulticastGroup(ipAddress);
                Utils.Logger.Instance.Log.Log(LogLevel.Debug, Utils.PrependAdapterAddress(this.m_address.ToString(), "Finished Setting up socket client:"));
                timer = new System.Timers.Timer();
                timer.Interval = (double)num1;
                timer.Elapsed += (ElapsedEventHandler)((setter, e) => eventReg?.Set());
                timer.AutoReset = false;
                try
                {
                    helloWaiter = new UdpClient(3702);
                    helloWaiter.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.IpTimeToLive, true);
                    helloWaiter.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                    helloWaiter.JoinMulticastGroup(ipAddress);
                    thread = DiscoveryRequest.ExcuteHelloAsyncInternal(helloWaiter, obj, hashtable, progress);
                }
                catch (SocketException ex)
                {
                    if (ex.NativeErrorCode == 10048)
                        Utils.Logger.Instance.Log.Log(LogLevel.Error, Utils.PrependAdapterAddress(this.m_address.ToString(), "Will not scan Hello as the socket is already in use"));
                    else
                        Utils.Logger.Instance.Log.Log(LogLevel.Error, Utils.PrependAdapterAddress(this.m_address.ToString(), "Will not scan Hello , Ignoring !!"));
                }
                Utils.Logger.Instance.Log.Log(LogLevel.Debug, Utils.PrependAdapterAddress(this.m_address.ToString(), "Setting up probe listener:"));
                DiscoveryRequest.ReceiveProbeResponse(client, obj, hashtable, progress);
                Utils.Logger.Instance.Log.Log(LogLevel.Debug, Utils.PrependAdapterAddress(this.m_address.ToString(), "Finished Setting up probe listener"));
                timer.Enabled = true;
                WaitHandle waitHandle = (WaitHandle)null;
                WaitHandle[] waitHandleArray = (WaitHandle[])null;
                if (token != null)
                {
                    waitHandle = token.GetType() == typeof(CancellationToken) ? ((CancellationToken)token).WaitHandle : (WaitHandle)null;
                    waitHandleArray = new WaitHandle[2]
                    {
            (WaitHandle) eventReg,
            waitHandle
                    };
                }
                string Msg1 = Utils.NVTMsg();
                string Msg2 = Utils.DeviceMsg();
                while (true)
                {
                    for (int index = 0; index < 3; ++index)
                    {
                        Utils.SendProbeMsg(ref client, ref ipEndPoint, Msg1);
                        Utils.SendProbeMsg(ref client, ref ipEndPoint, Msg2);
                    }
                    if (waitHandleArray != null)
                    {
                        if (WaitHandle.WaitAny(waitHandleArray) == Array.IndexOf<WaitHandle>(waitHandleArray, waitHandle))
                        {
                            Utils.Logger.Instance.Log.Log(LogLevel.Error, "Cancelled with: {0} ms left ", (object)timeout);
                            flag = true;
                        }
                    }
                    else
                        eventReg.WaitOne();
                    if (!(timeout == 0U | flag))
                    {
                        Utils.Logger.Instance.Log.Log(LogLevel.Debug, Utils.PrependAdapterAddress(this.m_address.ToString(), "Adjust Timeouts"));
                        uint num2 = num1 * 2U;
                        num1 = num2 < timeout ? num2 : timeout;
                        timer.Interval = (double)num1;
                        timer.Enabled = true;
                        timeout -= timeout > num1 ? num1 : timeout;
                    }
                    else
                        break;
                }
                Utils.Logger.Instance.Log.Log(LogLevel.Debug, "Cleanup");
                if (client != null)
                {
                    client.DropMulticastGroup(ipAddress);
                    client.Close();
                    client = (UdpClient)null;
                }
                if (helloWaiter != null)
                {
                    helloWaiter.Close();
                    helloWaiter = (UdpClient)null;
                }
                if (eventReg != null)
                {
                    eventReg.Close();
                    eventReg = (AutoResetEvent)null;
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException)
            {
                client?.Close();
                helloWaiter?.Close();
            }
            if (thread != null && thread.IsAlive)
                thread.Join();
            timer.Close();
            if (eventReg != null)
                eventReg.Dispose();
            Utils.Logger.Instance.Log.Log(LogLevel.Debug, Utils.PrependAdapterAddress(this.m_address.ToString(), "Deal with results"));
            DiscoveryResponse[] source = hashtable.Count > 0 ? hashtable.Values.Cast<DiscoveryResponse>().ToArray<DiscoveryResponse>() : (DiscoveryResponse[])null;
            if (source != null)
            {
                foreach (DiscoveryResponse discoveryResponse in source)
                    Utils.Logger.Instance.Log.Log(LogLevel.Information, Utils.PrependAdapterAddress(this.m_address.ToString(), "Found xaddrs " + string.Join(",", discoveryResponse.Xaddrs.Select<string, string>((Func<string, string>)(x => x.ToString((IFormatProvider)CultureInfo.CurrentCulture))).ToArray<string>())));
            }
            return source == null || !((IEnumerable<DiscoveryResponse>)source).Any<DiscoveryResponse>() ? (DiscoveryResponse[])null : ((IEnumerable<DiscoveryResponse>)source).ToArray<DiscoveryResponse>();
        }
    }
}
