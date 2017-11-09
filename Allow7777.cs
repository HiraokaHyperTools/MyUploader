using Fw2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUploader {
    class Allow7777 : IAllow7777 {
        public void Run() {
            Fw2Controller fw2 = new Fw2Controller();
            var port = 7777;
            var allAllowed = fw2.EnumAllAllowed();
            var alreadyAllowed = true
                && allAllowed.Contains($"domain,allow,tcp,{port}")
                && allAllowed.Contains($"private,allow,tcp,{port}")
                && allAllowed.Contains($"public,allow,tcp,{port}")
                ;
            if (!alreadyAllowed) {
                ProcessStartInfo psi = new ProcessStartInfo(GetType().Assembly.GetModules()[0].FullyQualifiedName, "/fw");
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
        }
    }
}
