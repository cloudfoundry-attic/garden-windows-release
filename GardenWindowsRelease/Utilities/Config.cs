using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace Utilities
{
    public class Config
    {
        public const string CONSUL_DNS_SUFFIX = ".cf.internal";

        public static string ConfigDir()
        {
            return ConfigDir("");
        }

        public static string ConfigDir(string service)
        {
            return
                Path.GetFullPath(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "GardenWindows", service));
        }


        public static Dictionary<string, string> Params()
        {
            var javaScriptSerializer = new JavaScriptSerializer();
            var parametersPath = Path.Combine(ConfigDir(), "parameters.json");
            string jsonString = File.ReadAllText(parametersPath);
            var hash = javaScriptSerializer.Deserialize<Dictionary<string, string>>(jsonString);
            SetMachineName(hash);
            SetBbsAddress(hash);
            return hash;
        }

        private static void SetMachineName(Dictionary<string, string> p)
        {
            if (!p.ContainsKey("MACHINE_NAME") || string.IsNullOrWhiteSpace(p["MACHINE_NAME"]))
            {
                p["MACHINE_NAME"] = Dns.GetHostName();
            }
        }

        private static void SetBbsAddress(Dictionary<string, string> p)
        {
            var sslValues = new[] { "BBS_CA_FILE", "BBS_CLIENT_CERT_FILE", "BBS_CLIENT_KEY_FILE" };
            if (sslValues.All(keyName => p.ContainsKey(keyName) && !string.IsNullOrWhiteSpace(p[keyName])))
            {
                p["BBS_ADDRESS"] = "https://bbs.service" + CONSUL_DNS_SUFFIX + ":8889";
            }
            else
            {
                p["BBS_ADDRESS"] = "http://bbs.service" + CONSUL_DNS_SUFFIX + ":8889";
            }
        }
    }
}