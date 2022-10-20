using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace OnvifCamera
{
    public class CameraLoggerConfig
    {
        private const string InvalidFactory = "Invalid Factory";
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private ILoggerFactory _factory;
        private readonly Dictionary<string, CameraLogger> _cameraLoggers = new Dictionary<string, CameraLogger>();
        private static readonly CameraLoggerConfig _cameraLoggerConfig = new CameraLoggerConfig();

        private CameraLoggerConfig() => this._factory = (ILoggerFactory)new LoggerFactory();

        internal CameraLogger CreateLogger(object obj, string categoryName)
        {
            CameraLogger logger = (CameraLogger)null;
            this._lock.EnterReadLock();
            try
            {
                string str = string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}:{1}", (object)categoryName, obj);
                logger = new CameraLogger(this._factory.CreateLogger(str));
                if (!this._cameraLoggers.ContainsKey(str))
                    this._cameraLoggers.Add(str, logger);
                else
                    this._cameraLoggers[str] = logger;
            }
            finally
            {
                this._lock.ExitReadLock();
            }
            return logger;
        }

        public static CameraLoggerConfig CreateCameraLoggerConfig() => CameraLoggerConfig._cameraLoggerConfig;

        public void SetLogger(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentException("Invalid Factory", nameof(loggerFactory));
            this._lock.EnterWriteLock();
            try
            {
                foreach (KeyValuePair<string, CameraLogger> cameraLogger in this._cameraLoggers)
                {
                    ILogger logger = loggerFactory.CreateLogger(cameraLogger.Key);
                    cameraLogger.Value.UpdateLogger(logger);
                }
                if (this._factory != null)
                    this._factory.Dispose();
                this._factory = loggerFactory;
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }
    }
}
