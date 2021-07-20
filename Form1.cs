using MyUploader.Properties;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyUploader
{
    public partial class Form1 : Form
    {
        public Form1(IAllow7777 fw, IWebServerLauncher web)
        {
            this.fw = fw;
            this.web = web;

            InitializeComponent();
        }

        private readonly IAllow7777 fw;
        private readonly IWebServerLauncher web;

        private static IEnumerable<IPAddress> GetConnectedIPAddresses()
        {
            IEnumerable<UnicastIPAddressInformation> Filter(
                UnicastIPAddressInformationCollection unicastAddresses,
                GatewayIPAddressInformationCollection gatewayAddresses
            )
            {
                bool Test(IPAddress address1, IPAddress address2, IPAddress mask)
                {
                    var a = address1.GetAddressBytes();
                    var b = address2.GetAddressBytes();
                    var m = mask.GetAddressBytes();
                    for (int x = 0; x < m.Length; x++)
                    {
                        if ((a[x] & m[x]) != (b[x] & m[x]))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                foreach (var ip in unicastAddresses.Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                {
                    foreach (var g in gatewayAddresses.Where(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                    {
                        if (Test(ip.Address, g.Address, ip.IPv4Mask))
                        {
                            yield return ip;
                        }
                    }
                }
            }

            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => n.GetIPProperties())
                .Where(n => n != null)
                .Where(n => n.GatewayAddresses.Any())
                .SelectMany(n => Filter(n.UnicastAddresses, n.GatewayAddresses))
                .Select(g => g?.Address)
                .Where(a => a != null)
                .ToArray();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // https://stackoverflow.com/a/13635038

            var local = Environment.MachineName + ".local";
            if (IsItMe(local))
            {
                //comboBox1.Items.Add(Settings.Default.URL.Replace("localhost", local));
            }
            foreach (var ip in Dns.GetHostAddresses(Environment.MachineName)
                .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .OrderBy(ip => GetConnectedIPAddresses().Contains(ip) ? 0 : 1)
                .ThenBy(ip => Regex.Replace(ip.ToString(), "\\d+", M => M.Value.PadLeft(10, '0')))
            )
            {
                comboBox1.Items.Add(Settings.Default.URL.Replace("localhost", ip.ToString().Split('%')[0]));
            }
            if (comboBox1.Items.Count >= 1)
            {
                comboBox1.SelectedIndex = 0;
            }

            var task1 = RunWebAsync();
            var task2 = RunFwAsync();
        }

        private async Task RunFwAsync()
        {
            try
            {
                await Task.Run(fw.Run);
            }
            catch (Exception onError)
            {
                errorConsumer(new Exception("ファイアーウォールの許可に失敗しました！", onError));
            }
        }

        private async Task RunWebAsync()
        {
            try
            {
                await Task.Run(web.Run);
            }
            catch (Exception onError)
            {
                errorConsumer(new Exception("Web サーバーの起動に失敗しました！", onError));
            }
        }

        private void errorConsumer(Exception onNext)
        {
            Panel panel = new Panel();
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.Dock = DockStyle.Bottom;
            panel.BackColor = Color.FromKnownColor(KnownColor.Info);
            panel.ForeColor = Color.FromKnownColor(KnownColor.InfoText);
            {
                Label label = new Label();
                label.AutoSize = true;
                label.Text = onNext.Message;
                label.Click += (a, b) =>
                {
                    MessageBox.Show(onNext.InnerException + "");
                };
                panel.Controls.Add(label);
            }
            Controls.Add(panel);
        }

        private bool IsItMe(string local)
        {
            try
            {
                return Dns.GetHostAddresses(local).Select(p => p.ToString()).Intersect(Dns.GetHostAddresses(Environment.MachineName).Select(p => p.ToString())).Any();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var writer = new ZXing.QrCode.QRCodeWriter();
            var matrix = writer.encode(comboBox1.Text, ZXing.BarcodeFormat.QR_CODE, 0, 0);

            int scale = 10;
            int www = 1 + matrix.Dimension + 1;
            Bitmap pic = new Bitmap(scale * www, scale * www);

            using (var cv = Graphics.FromImage(pic))
            {
                cv.Clear(Color.White);
                for (int y = 0; y < matrix.Dimension; y++)
                {
                    for (int x = 0; x < matrix.Dimension; x++)
                    {
                        if (matrix[x, y])
                        {
                            cv.FillRectangle(Brushes.Black,
                                new RectangleF(scale * (1 + x), scale * (1 + y), scale, scale)
                                );
                        }
                    }
                }
            }

            pictureBox1.Image = pic;
        }

        private void openPicture_Click(object sender, EventArgs e)
        {
            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        }
    }
}
