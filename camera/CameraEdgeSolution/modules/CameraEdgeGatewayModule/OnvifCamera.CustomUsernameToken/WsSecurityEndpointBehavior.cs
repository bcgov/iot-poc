// Decompiled with JetBrains decompiler
// Type: OnvifCamera.CustomUsernameToken.WsSecurityEndpointBehavior
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OnvifCamera.CustomUsernameToken
{
    public sealed class WsSecurityEndpointBehavior : IEndpointBehavior
    {
        private readonly string _username;
        private readonly string _password;
        private readonly TimeOnDevice _time;

        public WsSecurityEndpointBehavior(string username, string password, TimeOnDevice time)
        {
            this._username = username;
            this._password = password;
            this._time = time;
        }

        void IEndpointBehavior.AddBindingParameters(
          ServiceEndpoint endpoint,
          BindingParameterCollection bindingParameters)
        {
            bindingParameters.Add((object)new Func<HttpClientHandler, HttpMessageHandler>(WsSecurityEndpointBehavior.GetHttpMessageHandler));
        }

        void IEndpointBehavior.ApplyClientBehavior(
          ServiceEndpoint endpoint,
          ClientRuntime clientRuntime)
        {
            if (this._time == null)
                return;
            clientRuntime.ClientMessageInspectors.Add((IClientMessageInspector)new WsSecurityMessageInspector(this._username, this._password, this._time));
        }

        void IEndpointBehavior.ApplyDispatchBehavior(
          ServiceEndpoint endpoint,
          EndpointDispatcher endpointDispatcher)
        {
            throw new NotImplementedException();
        }

        void IEndpointBehavior.Validate(ServiceEndpoint endpoint)
        {
        }

        private static HttpMessageHandler GetHttpMessageHandler(
          HttpClientHandler httpClientHandler)
        {
            return (HttpMessageHandler)new InterceptHttpMessageHandler((HttpMessageHandler)httpClientHandler);
        }
    }
}
