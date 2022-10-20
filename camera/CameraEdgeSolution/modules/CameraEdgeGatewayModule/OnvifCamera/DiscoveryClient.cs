// Decompiled with JetBrains decompiler
// Type: OnvifCamera.DiscoveryClient
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using Microsoft.Extensions.Logging;
using OnvifCamera.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OnvifCamera
{
    public static class DiscoveryClient
    {
        internal static DiscoveryResponse[] FindInternal(
          uint timeout,
          IProgress<DiscoveryResponse> progress,
          object token)
        {
            Dictionary<string, DiscoveryResponse> dictionary = new Dictionary<string, DiscoveryResponse>();
            List<IPAddress> ipAddressList = DiscoveryClient.RetrieveListOfValidAdapterIPs();
            if (ipAddressList.Count == 0)
            {
                Utils.Logger.Instance.Log.Log(LogLevel.Information, "No suitable Adapters found, Exiting!!!");
                return (DiscoveryResponse[])null;
            }
            List<Task<DiscoveryResponse[]>> taskList = new List<Task<DiscoveryResponse[]>>();
            foreach (IPAddress address in ipAddressList)
            {
                Utils.Logger.Instance.Log.Log(LogLevel.Information, "Sending probe for this adapter address: " + address.ToString());
                DiscoveryRequest request = new DiscoveryRequest(address);
                taskList.Add(Task.Run<DiscoveryResponse[]>((Func<DiscoveryResponse[]>)(() => request.Execute(timeout, progress, token))));
            }
            try
            {
                Task.WaitAll((Task[])taskList.ToArray());
            }
            catch (AggregateException ex)
            {
                bool flag = true;
                foreach (Task task in taskList)
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                    throw ex.Flatten();
            }
            foreach (Task<DiscoveryResponse[]> task in taskList)
            {
                if (task.IsCompletedSuccessfully)
                {
                    DiscoveryResponse[] result = task.Result;
                    if (result != null && result.Length != 0)
                    {
                        foreach (DiscoveryResponse discoveryResponse in result)
                        {
                            if (!dictionary.ContainsKey(discoveryResponse.RemoteAddress))
                            {
                                Utils.Logger.Instance.Log.Log(LogLevel.Information, "Adding " + discoveryResponse.RemoteAddress + " to final ipaddress set");
                                dictionary.Add(discoveryResponse.RemoteAddress, discoveryResponse);
                            }
                            else
                                Utils.Logger.Instance.Log.Log(LogLevel.Debug, "Duplicate found camera address: " + discoveryResponse.RemoteAddress);
                        }
                    }
                }
                else
                    Utils.Logger.Instance.Log.Log(LogLevel.Error, "Probe ran into issue");
            }
            return dictionary.Count <= 0 ? (DiscoveryResponse[])null : dictionary.Values.ToArray<DiscoveryResponse>();
        }

        internal static List<IPAddress> RetrieveListOfValidAdapterIPs()
        {
            List<IPAddress> ipAddressList = new List<IPAddress>();
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                Utils.Logger.Instance.Log.Log(LogLevel.Information, networkInterface.Description);
                if (networkInterface.Supports(NetworkInterfaceComponent.IPv4))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !networkInterface.GetIPProperties().MulticastAddresses.Any<MulticastIPAddressInformation>())
                        Utils.Logger.Instance.Log.Log(LogLevel.Information, "Multicast fail");
                    else if (!networkInterface.SupportsMulticast)
                        Utils.Logger.Instance.Log.Log(LogLevel.Information, "No support multicast");
                    else if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ppp)
                        Utils.Logger.Instance.Log.Log(LogLevel.Information, "PPP interface, ignore");
                    else if (OperationalStatus.Up != networkInterface.OperationalStatus)
                    {
                        Utils.Logger.Instance.Log.Log(LogLevel.Information, "Adapter not up");
                    }
                    else
                    {
                        foreach (IPAddressInformation unicastAddress in ipProperties.UnicastAddresses)
                        {
                            IPAddress ipAddress = IPAddress.Parse(unicastAddress.Address.ToString());
                            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                            {
                                Utils.Logger.Instance.Log.Log(LogLevel.Information, "Adding Ipv4 address: " + ipAddress.ToString());
                                ipAddressList.Add(ipAddress);
                                break;
                            }
                        }
                    }
                }
            }
            return ipAddressList;
        }

        public static DiscoveryResponse[] Find(uint timeout = 5000) => DiscoveryClient.FindInternal(timeout, (IProgress<DiscoveryResponse>)null, (object)null);

        public static async Task<DiscoveryResponse[]> FindTaskAsync(
          uint timeout = 5000,
          IProgress<DiscoveryResponse> progress = null,
          CancellationToken token = default(CancellationToken))
        {
            return await Task.Run<DiscoveryResponse[]>((Func<DiscoveryResponse[]>)(() => DiscoveryClient.FindInternal(timeout, progress, (object)token)), token).ConfigureAwait(false);
        }
    }
}
