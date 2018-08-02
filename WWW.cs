using MyUploader.Properties;
using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyUploader {
    public class WWW : NancyModule {
        public WWW() {
            Get["/"] = query => {
                return View["Views/index.sshtml"];
            };
            Post["/"] = query => {
                Res res = new Res() { Uploaded = true };
                lock (typeof(WWW)) {
                    byte[] bin = new byte[4000];
                    int numFiles = 0;
                    foreach (var f in Request.Files) {
                        // 2016-05-07 iPhone 192.168.2.47 image 012 みたいに
                        ++numFiles;
                        var fn = String.Format("{0:yyyy-MM-dd} {1} {2} {3} {4}"
                            , DateTime.Now
                            , UAUt.Filter(Request.Headers["User-Agent"])
                            , Request.UserHostAddress
                            , numFiles
                            , FNUt.Norm(f.Name)
                            );
                        String fp2;
                        lock (typeof(WWW)) {
                            for (int x = 1; ; x++) {
                                fp2 = Path.Combine(Settings.Default.SaveDir, Path.GetFileNameWithoutExtension(fn) + " " + x.ToString("000") + Path.GetExtension(fn));
                                if (!File.Exists(fp2)) break;
                            }
                            File.Create(fp2).Close();
                        }
                        var si = f.Value;
                        using (var os = File.Create(fp2)) {
                            while (true) {
                                int r = si.Read(bin, 0, bin.Length);
                                if (r < 1) break;
                                os.Write(bin, 0, r);
                            }
                        }
                        res.Files.Add(fp2);
                    }
                }
                return View["Views/res.sshtml", res];
            };
        }

        class Res {
            public List<string> Files { get; set; }
            public bool Uploaded { get; set; }
            public int n { get { return Files.Count; } }

            public Res() {
                Files = new List<string>();
            }
        }

        class UAUt {
            internal static string Filter(IEnumerable<string> UAs) {
                foreach (String ua in UAs) {
                    if (ua.Contains("iPhone")) return "iPhone ";
                    if (ua.Contains("iPad")) return "iPad ";
                    if (ua.Contains("Android")) return "Android ";
                    if (ua.Contains("Chrome/")) return "Chrome ";
                    if (ua.Contains("Safari")) return "Safari ";
                    if (ua.Contains("Touch") && ua.Contains("Windows NT 6.2")) return "Surface ";
                    if (ua.Contains("Windows")) return "Windows ";
                }
                return "";
            }
        }

        class FNUt {
            public static string Norm(string s) {
                return Regex.Replace(s, "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]", "_");
            }
        }
    }
}
