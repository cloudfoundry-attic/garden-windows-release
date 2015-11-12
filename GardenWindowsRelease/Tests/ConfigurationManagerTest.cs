using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace Tests
{
    public class ConfigurationManagerTest : ConfigurationManager.ConfigurationManager
    {
        [Theory, AutoData]
        public void ItPersistsArgumentsInFile(
            string adminUsername, string adminPassword, string containerDirectory, string externalIp, string syslogHostIp, string syslogPort, string machineName)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            var configurationManager = new ConfigurationManagerTest();
            var context = new InstallContext();
            context.Parameters.Add("ADMIN_USERNAME", adminUsername);
            context.Parameters.Add("ADMIN_PASSWORD", adminPassword);
            context.Parameters.Add("CONTAINER_DIRECTORY", containerDirectory);
            context.Parameters.Add("EXTERNAL_IP", externalIp);
            context.Parameters.Add("SYSLOG_HOST_IP", syslogHostIp);
            context.Parameters.Add("SYSLOG_PORT", syslogPort);
            context.Parameters.Add("assemblypath", tempDirectory);
            context.Parameters.Add("MACHINE_NAME", machineName);
            configurationManager.Context = context;
            configurationManager.OnBeforeInstall(null);

            var javaScriptSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var jsonString = File.ReadAllText(Path.Combine(tempDirectory, @"..\parameters.json"));
            var hash = javaScriptSerializer.Deserialize<Dictionary<string, string>>(jsonString);
            Assert.False(hash.ContainsKey("ADMIN_PASSWORD"));
            Assert.Equal(hash["CONTAINER_DIRECTORY"], containerDirectory);
            Assert.Equal(hash["EXTERNAL_IP"], externalIp);
            Assert.Equal(hash["SYSLOG_HOST_IP"], syslogHostIp);
            Assert.Equal(hash["SYSLOG_PORT"], syslogPort);
            Assert.Equal(hash["MACHINE_NAME"], machineName);
        }
    }
}
