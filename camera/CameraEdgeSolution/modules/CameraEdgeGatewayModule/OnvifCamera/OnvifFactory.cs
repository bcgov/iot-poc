// Decompiled with JetBrains decompiler
// Type: OnvifCamera.OnvifFactory
// Assembly: OnvifCameraManager, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FB196F96-143B-47AF-9F44-FC597C41213A
// Assembly location: C:\Users\BRWANG\Downloads\OnvifCMDProj\OnvifCMDProj\OnvifCameraManager.dll

using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace OnvifCamera
{
    internal static class OnvifFactory
    {
        public static T CreateOnvifObj<T>(Uri uri)
        {
            EndpointAddress endpointAddress = new EndpointAddress(uri.ToString());
            HttpTransportBindingElement transportBindingElement = uri.Scheme == "https" ? (HttpTransportBindingElement)new HttpsTransportBindingElement() : new HttpTransportBindingElement();

            
            
            transportBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
            TextMessageEncodingBindingElement encodingBindingElement = new TextMessageEncodingBindingElement();
            encodingBindingElement.MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None);
            return (T)Activator.CreateInstance(typeof(T), (object)new CustomBinding(new BindingElement[2]
            {
        (BindingElement) encodingBindingElement,
        (BindingElement) transportBindingElement
            }), (object)endpointAddress);
        }

        public static void SetCredentials(string username, string password, ClientCredentials creds)
        {
            if (username == null || password == null)
                return;
            creds.UserName.UserName = username;
            creds.UserName.Password = password;
            creds.HttpDigest.ClientCredential.UserName = username;
            creds.HttpDigest.ClientCredential.Password = password;
        }
    }
}
