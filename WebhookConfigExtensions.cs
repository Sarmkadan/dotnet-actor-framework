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
    public static class WebhookConfigExtensions
    {
        public static void ThrowIfNullOrEmpty(this string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ConfigurationException("Invalid webhook URL: ", url);
            }
        }
    }
}
