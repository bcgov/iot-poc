using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.ServiceModel;
using System.Threading;

namespace OnvifCamera
{
    internal class CameraLogger : ILogger, IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private int _disposed;
        private ILogger _logger;

        internal CameraLogger(ILogger logger) => this._logger = logger;

        ~CameraLogger() => this.Dispose(false);

        internal static string GetTypeAndValues(object obj) => string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}:  {1}", (object)obj.GetType(), (object)JsonConvert.SerializeObject(obj));

        internal static string GetExceptionString(Exception ex)
        {
            return string.Format((IFormatProvider)CultureInfo.InvariantCulture, "HResult: {0} Message: {1} Type: {2}\nSource: {3}\nStack: {4}{5}", (object)ex.HResult, (object)ex.Message, (object)ex.GetType()?.FullName, (object)ex?.Source, (object)ex?.StackTrace, (object)GetExceptionInfo(ex));

            static string GetExceptionInfo(Exception ex)
            {
                if (!(ex is FaultException))
                    return "";
                FaultException faultException = ex as FaultException;
                return string.Format((IFormatProvider)CultureInfo.InvariantCulture, "\nIsPredefinedFault: {0} IsReceiverFault: {1} IsSenderFault: {2} Name: {3} NameSpace: {4} SubCode: {5} {6}", (object)faultException.Code.IsPredefinedFault, (object)faultException.Code.IsReceiverFault, (object)faultException.Code.IsSenderFault, (object)faultException.Code.Name, (object)faultException.Code.Namespace, (object)faultException.Code.SubCode, (object)faultException.Code.ToString());
            }
        }

        internal void LogObjectState(LogLevel level, string state, object input, object output)
        {
            if (!this.IsEnabled(level))
                return;
            string message = string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}\n{1}{2}{3}", (object)DateTime.Now.ToString("hh:mm:ss.FFF", (IFormatProvider)CultureInfo.InvariantCulture), state != null ? (object)string.Format((IFormatProvider)CultureInfo.InvariantCulture, "State: {0}\n", (object)state) : (object)"", input != null ? (object)string.Format((IFormatProvider)CultureInfo.InvariantCulture, "PreCondition: {0}\n", (object)CameraLogger.GetTypeAndValues(input)) : (object)"", output != null ? (object)string.Format((IFormatProvider)CultureInfo.InvariantCulture, "PostCondition: {0}\n", (object)CameraLogger.GetTypeAndValues(output)) : (object)"");
            switch (level)
            {
                case LogLevel.Trace:
                    this.LogTrace(message);
                    break;
                case LogLevel.Debug:
                    this.LogDebug(message);
                    break;
                case LogLevel.Information:
                    this.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    this.LogWarning(message);
                    break;
                case LogLevel.Error:
                    this.LogError(message);
                    break;
                case LogLevel.Critical:
                    this.LogCritical(message);
                    break;
            }
        }

        internal void UpdateLogger(ILogger logger)
        {
            this.LogWarning(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Logger is being Changed"));
            this._lock.EnterWriteLock();
            try
            {
                this._logger = logger;
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            this._lock.EnterReadLock();
            try
            {
                return this._logger.BeginScope<TState>(state);
            }
            finally
            {
                this._lock.ExitReadLock();
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            this._lock.EnterReadLock();
            try
            {
                return this._logger.IsEnabled(logLevel);
            }
            finally
            {
                this._lock.ExitReadLock();
            }
        }

        public void Log<TState>(
          LogLevel logLevel,
          EventId eventId,
          TState state,
          Exception exception,
          Func<TState, Exception, string> formatter)
        {
            this._lock.EnterReadLock();
            try
            {
                this._logger.Log<TState>(logLevel, eventId, state, exception, formatter);
            }
            finally
            {
                this._lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref this._disposed, 1) != 0 || !disposing)
                return;
            this._lock.Dispose();
        }
    }
}
