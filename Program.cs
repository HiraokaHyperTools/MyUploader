using Fw2;
using MyUploader.Properties;
using Nancy.Hosting.Self;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyUploader {
    static class Program {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            if (args.Length >= 1 && args[0] == "/fw") {
                Fw2Controller fw2 = new Fw2Controller();
                fw2.AllowTCP(7777, NET_FW_PROFILE_TYPE2.ALL);
                return;
            }

            Settings.Default.SaveDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            IKernel kernel = new StandardKernel(new MainModule());

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(kernel.Get<Form1>());
        }
    }
}
