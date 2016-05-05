using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Principal;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
using System.Security.AccessControl;

namespace Tests
{
    public class TempDirectory
    {
        private string path;

        public TempDirectory()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
        }

        public override string ToString()
        {
            return path;
        }
    }

    public class ConfigurationManagerWithOverridableDestinationPath : ConfigurationManager.ConfigurationManager
    {
        public string destinationPath { get; set; }

        protected override string Destination()
        {
            return destinationPath;
        }

        public new void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);
        }
    }

    public class ConfigurationManagerTest : IDisposable
    {
        private ConfigurationManagerWithOverridableDestinationPath configurationManager;
        private TempDirectory tempDirectory = new TempDirectory();

        public ConfigurationManagerTest()
        {
            configurationManager = new ConfigurationManagerWithOverridableDestinationPath();
            configurationManager.destinationPath = tempDirectory.ToString();
        }

        public void Dispose()
        {
            Directory.Delete(tempDirectory.ToString(), true);
        }

        [Theory, AutoData]
        public void ItPersistsArgumentsInFile(
            string containerDirectory, string machineIp, string syslogHostIp, string syslogPort, string machineName)
        {
            var context = new InstallContext();
            context.Parameters.Add("CONTAINER_DIRECTORY", containerDirectory);
            context.Parameters.Add("MACHINE_IP", machineIp);
            context.Parameters.Add("SYSLOG_HOST_IP", syslogHostIp);
            context.Parameters.Add("SYSLOG_PORT", syslogPort);
            context.Parameters.Add("MACHINE_NAME", machineName);
            configurationManager.Context = context;
            configurationManager.OnBeforeInstall(null);

            var acl = Directory.GetAccessControl(tempDirectory.ToString());
            var accessRules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            Assert.Equal(accessRules.Count, 1);
            var rule = (FileSystemAccessRule)accessRules[0];
            Assert.Equal(rule.AccessControlType, AccessControlType.Allow);
            Assert.Equal(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), rule.IdentityReference);
            Assert.Equal(rule.FileSystemRights, FileSystemRights.FullControl);

            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var parametersPath = Path.Combine(tempDirectory.ToString(), "parameters.json");
            var jsonString = File.ReadAllText(parametersPath);
            var hash = javaScriptSerializer.Deserialize<Dictionary<string, string>>(jsonString);
            Assert.Equal(hash["CONTAINER_DIRECTORY"], containerDirectory);
            Assert.Equal(hash["MACHINE_IP"], machineIp);
            Assert.Equal(hash["SYSLOG_HOST_IP"], syslogHostIp);
            Assert.Equal(hash["SYSLOG_PORT"], syslogPort);
            Assert.Equal(hash["MACHINE_NAME"], machineName);
        }
    }
}
