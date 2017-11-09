using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyUploader {
    public class MainModule : NinjectModule {
        public override void Load() {
            Bind<IAllow7777>().To<Allow7777>();
            Bind<IWebServerLauncher>().To<WebServerLauncher>();
        }
    }
}
