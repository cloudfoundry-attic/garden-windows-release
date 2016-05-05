using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Utilities;

namespace ConfigurationManager
{
    [RunInstaller(true)]
    public partial class ConfigurationManager : System.Configuration.Install.Installer
    {
        private const string eventSource = "Diego MSI Windows Features Installer";

        public ConfigurationManager()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(eventSource))
                EventLog.CreateEventSource(eventSource, "Application");
            EventLog.WriteEntry(eventSource, "Service Initializing", EventLogEntryType.Information, 0);
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);

            var missing = new List<string>();

            var required = new List<string>()
            {
                "MACHINE_IP"
            };

            var optional = new List<string>
            {
                "CONTAINER_DIRECTORY",
                "MACHINE_NAME",
                "SYSLOG_HOST_IP",
                "SYSLOG_PORT"
            };

            foreach (var key in required)
            {
                if (Context.Parameters[key] == null || Context.Parameters[key] == "")
                    missing.Add(key);
            }

            if (missing.Count > 0)
            {
                throw new Exception("Please provide all of the following msiexec properties: " +
                                    string.Join(", ", missing));
            }

            var presentOptional = optional.Where(key => Context.Parameters[key] != null && Context.Parameters[key] != "");
            var keys = required.Concat(presentOptional).ToList();
            WriteParametersFile(keys);
        }

        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            Directory.Delete(Destination(), true);
            base.OnBeforeUninstall(savedState);
        }

        private void WriteParametersFile(IEnumerable<string> keys)
        {
            if (!Directory.Exists(Destination()))
            {
                Directory.CreateDirectory(Destination());
            }
            var directorySecurity = new DirectorySecurity();
            directorySecurity.SetAccessRuleProtection(true, false);
            directorySecurity.SetAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
            Directory.SetAccessControl(Destination(), directorySecurity);
            var parameters = new Dictionary<string, string>();
            foreach (string key in keys)
            {
                var value = Context.Parameters[key];
                parameters.Add(key, value);
            }
            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonString = javaScriptSerializer.Serialize(parameters);
            var configFile = DestinationFilename("parameters.json");
            File.WriteAllText(configFile, jsonString);
        }

        protected virtual string Destination()
        {
            return Config.ConfigDir();
        }

        private string DestinationFilename(string path)
        {
            var filename = Path.GetFileName(path);
            return Path.GetFullPath(Path.Combine(Destination(), filename));
        }
    }
}