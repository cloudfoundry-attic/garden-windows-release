using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using Ploeh.AutoFixture.Xunit2;
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
        [Theory, AutoData]
        public void ItPersistsArgumentsInFile(
            string containerDirectory, string machineIp, string syslogHostIp, string syslogPort, string machineName)
        {
            using(var tempDirectory = new TempDirectory())
            {
                var configurationManager = new ConfigurationManagerTest();
                var context = new InstallContext();
                context.Parameters.Add("CONTAINER_DIRECTORY", containerDirectory);
                context.Parameters.Add("MACHINE_IP", machineIp);
                context.Parameters.Add("SYSLOG_HOST_IP", syslogHostIp);
                context.Parameters.Add("SYSLOG_PORT", syslogPort);
                context.Parameters.Add("assemblypath", tempDirectory.ToString());
                context.Parameters.Add("MACHINE_NAME", machineName);
                configurationManager.Context = context;
                configurationManager.OnBeforeInstall(null);

                var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var jsonString = File.ReadAllText(Path.Combine(tempDirectory.ToString(), @"..\parameters.json"));
                var hash = javaScriptSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                Assert.Equal(hash["CONTAINER_DIRECTORY"], containerDirectory);
                Assert.Equal(hash["MACHINE_IP"], machineIp);
                Assert.Equal(hash["SYSLOG_HOST_IP"], syslogHostIp);
                Assert.Equal(hash["SYSLOG_PORT"], syslogPort);
                Assert.Equal(hash["MACHINE_NAME"], machineName);
            }
        }
    }
}
