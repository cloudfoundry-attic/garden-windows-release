using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using Ploeh.AutoFixture.Xunit2;
using Utilities;
using Xunit;

namespace Tests
{
    public class TempDirectory : IDisposable
    {
        private string path;

        public TempDirectory()
        {
            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
        }

        public void Dispose()
        {
            Directory.Delete(Path.Combine(path, "../"), true);
        }

        public override string ToString()
        {
            return path;
        }
    }
    public class ConfigurationManagerTest : ConfigurationManager.ConfigurationManager
    {
        private string destinationPath { get; set; }

        protected override string Destination()
        {
            return destinationPath;
        }

        [Theory, AutoData]
        public void ItPersistsArgumentsInFile(
            string containerDirectory, string machineIp, string syslogHostIp, string syslogPort, string machineName)
        {
            TempDirectory tempDirectory = new TempDirectory();
            var configurationManager = new ConfigurationManagerTest();
            configurationManager.destinationPath = tempDirectory.ToString();
            var context = new InstallContext();
            context.Parameters.Add("CONTAINER_DIRECTORY", containerDirectory);
            context.Parameters.Add("MACHINE_IP", machineIp);
            context.Parameters.Add("SYSLOG_HOST_IP", syslogHostIp);
            context.Parameters.Add("SYSLOG_PORT", syslogPort);
            context.Parameters.Add("MACHINE_NAME", machineName);
            configurationManager.Context = context;
            configurationManager.OnBeforeInstall(null);

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
