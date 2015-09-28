using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace ContainerizerService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : CommonService
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        public override string ServiceName()
        {
            return this.serviceInstaller.ServiceName;
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.User;

            string inputUsername = Context.Parameters["ADMIN_USERNAME"];

            try
            {
                var ntAccount = new NTAccount(inputUsername);
                var ssid = (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));
                var normalizedNtAccount = (NTAccount)ssid.Translate(typeof(NTAccount));

                this.serviceProcessInstaller.Username = normalizedNtAccount.Value;

            }
            catch (IdentityNotMappedException)
            {
                throw new Exception("ADMIN_USERNAME value is not a valid username. Please make sure that the ADMIN_USERNAME is present on the Machine or AD.");
            }


            this.serviceProcessInstaller.Password = Context.Parameters["ADMIN_PASSWORD"];

            base.OnBeforeInstall(savedState);
        }
    }
}
