using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DotnetActorFramework
{
    public static class ConfigurationExceptionExtensions
    {
        public static void ThrowIfNullOrEmpty(this string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ConfigurationException("Invalid configuration key: ", key);
            }
        }
    }
}
