using MyUploader.Properties;
using Nancy;
using Nancy.Conventions;
using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUploader
{
    class WebServerLauncher : IWebServerLauncher
    {
        public void Run()
        {
            var hc = new HostConfiguration();
            hc.UrlReservations.CreateAutomatically = true;
            hc.RewriteLocalhost = true;
            var host = new NancyHost(new CustomBoostrapper(), hc, Settings.Default.URL.Trim().Replace("\r\n", "\n").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(q => new Uri(q.Trim())).ToArray());
            host.Start();
        }

        public class CustomBoostrapper : DefaultNancyBootstrapper
        {
            protected override void ConfigureConventions(NancyConventions conventions)
            {
                base.ConfigureConventions(conventions);

                conventions.StaticContentsConventions.Add(
                    StaticContentConventionBuilder.AddDirectory("static", @"static")
                );
            }
        }
    }
}
