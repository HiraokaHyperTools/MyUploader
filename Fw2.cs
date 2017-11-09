using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fw2 {
    public enum NET_FW_PROFILE_TYPE {
        DOMAIN = 0, STANDARD = 1, CURRENT = 2,
    }
    public enum NET_FW_IP_VERSION {
        V4 = 0,
        V6 = 1,
        ANY = 2,
    }
    public enum NET_FW_SCOPE {
        ALL = 0,
        LOCAL_SUBNET = 1,
        CUSTOM = 2,
    }
    public enum NET_FW_IP_PROTOCOL {
        TCP = 6,
        UDP = 17,
        ANY = 256,
    }

    [Guid("E0483BA0-47FF-4D9C-A6D6-7741D0B195F7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface INetFwOpenPort {
        String Name { get; set; }
        NET_FW_IP_VERSION IpVersion { get; set; }
        NET_FW_IP_PROTOCOL Protocol { get; set; }
        int Port { get; set; }
        NET_FW_SCOPE Scope { get; set; }
        String RemoteAddresses { get; set; }
        bool Enabled { get; set; }
        bool BuiltIn { get; }
    }

    public enum NET_FW_RULE_DIRECTION {
        IN = 1, OUT = 2,
    }
    public enum NET_FW_ACTION {
        BLOCK = 0, ALLOW = 1,
    }
    [Flags]
    public enum NET_FW_PROFILE_TYPE2 {
        DOMAIN = 1, PRIVATE = 2, PUBLIC = 4, ALL = 0x7fffffff,
    }
    [Guid("AF230D27-BABA-4E42-ACED-F524F22CFCE2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface INetFwRule {
        String Name { get; set; }
        String Description { get; set; }
        String ApplicationName { get; set; }
        String ServiceName { get; set; }
        int Protocol { get; set; }
        String LocalPorts { get; set; }
        String RemotePorts { get; set; }
        String LocalAddresses { get; set; }
        String RemoteAddresses { get; set; }
        NET_FW_RULE_DIRECTION Direction { get; set; }
        Object Interfaces { get; set; }
        String InterfaceTypes { get; set; }
        bool Enabled { get; set; }
        String Grouping { get; set; }
        NET_FW_PROFILE_TYPE2 Profiles { get; set; }
        bool EdgeTraversal { get; set; }
        NET_FW_ACTION Action { get; set; }
    }

    public class Fw2Controller : IDisposable {
        MarshalByRefObject fwPolicy2;
        MarshalByRefObject fwRules;

        /// <summary>
        /// インバウンド TCP 許可しているポートとプロファイルを一覧で出力します
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>domain,allow,tcp,12345</item>
        /// <item>private,allow,tcp,12345</item>
        /// <item>public,allow,tcp,12345</item>
        /// </list>
        /// </remarks>
        /// <returns></returns>
        public string[] EnumAllAllowed() {
            List<string> al = new List<string>();
            IEnumerator fwRulesEnum = (IEnumerator)fwRules.GetType().InvokeMember("_NewEnum", System.Reflection.BindingFlags.GetProperty, null, fwRules, new Object[0]);
            while (fwRulesEnum.MoveNext()) {
                INetFwRule fwRule = (INetFwRule)fwRulesEnum.Current;

                try {
                    if (true
                        && fwRule.Direction == NET_FW_RULE_DIRECTION.IN
                        && fwRule.Enabled
                        && fwRule.Protocol == 6
                        && string.IsNullOrEmpty(fwRule.ApplicationName)
                    ) {
                        bool allow = fwRule.Action == NET_FW_ACTION.ALLOW;
                        BitArray ports = Ut.GetPorts(fwRule.LocalPorts ?? "");
                        NET_FW_PROFILE_TYPE2 profs = fwRule.Profiles;

                        for (int x = 0; x < 65536; x++) {
                            if (ports[x]) {
                                if (0 != (profs & NET_FW_PROFILE_TYPE2.DOMAIN)) al.Add("domain,allow,tcp," + x + "");
                                if (0 != (profs & NET_FW_PROFILE_TYPE2.PRIVATE)) al.Add("private,allow,tcp," + x + "");
                                if (0 != (profs & NET_FW_PROFILE_TYPE2.PUBLIC)) al.Add("public,allow,tcp," + x + "");
                            }
                        }
                    }
                }
                finally {
                    Marshal.ReleaseComObject(fwRule);
                }
            }
            return al.ToArray();
        }

        public void AllowTCP(int port, NET_FW_PROFILE_TYPE2 profile) {
            MarshalByRefObject fwRule = (MarshalByRefObject)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            try {
                INetFwRule newRule = (INetFwRule)fwRule;
                newRule.Name = string.Format("TCP {0}", port);
                newRule.Description = "IsFwAllowed";
                newRule.Protocol = 6;
                newRule.LocalPorts = "" + port;
                newRule.Direction = NET_FW_RULE_DIRECTION.IN;
                newRule.Enabled = true;
                newRule.Profiles = profile;
                newRule.Action = NET_FW_ACTION.ALLOW;

                fwRules.GetType().InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, fwRules, new Object[] { fwRule });
            }
            finally {
                Marshal.ReleaseComObject(fwRule);
            }
        }

        public Fw2Controller() {
            fwPolicy2 = (MarshalByRefObject)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            fwRules = (MarshalByRefObject)fwPolicy2.GetType().InvokeMember("Rules", System.Reflection.BindingFlags.GetProperty, null, fwPolicy2, new Object[0]);
        }

        public void Dispose() {
            Marshal.ReleaseComObject(fwRules); fwRules = null;
            Marshal.ReleaseComObject(fwPolicy2); fwPolicy2 = null;
        }

        class Ut {
            public static BitArray GetPorts(String ports) {
                BitArray bits = new BitArray(65536);
                if (string.IsNullOrEmpty(ports)) {
                    bits.SetAll(false);
                }
                else if (ports == "*") {
                    bits.SetAll(true);
                }
                else {
                    foreach (String parts in ports.Split(',')) {
                        String[] pair = parts.Split('-');
                        int from, to;
                        if (pair.Length == 1 && PUt.TryParse(pair[0], out from)) {
                            bits[from] = true;
                        }
                        else if (pair.Length == 2 && PUt.TryParse(pair[0], out from) && PUt.TryParse(pair[1], out to)) {
                            while (from <= to) {
                                bits[from] = true;
                                from++;
                            }
                        }
                    }
                }
                return bits;
            }

            class PUt {
                internal static bool TryParse(string name, out int port) {
                    if (int.TryParse(name, out port)) {
                        return true;
                    }
                    #region IANA ports
                    switch (name.ToLowerInvariant()) {
                        case "echo": port = 7; break;
                        case "discard": port = 9; break;
                        case "systat": port = 11; break;
                        case "daytime": port = 13; break;
                        case "qotd": port = 17; break;
                        case "chargen": port = 19; break;
                        case "ftp-data": port = 20; break;
                        case "ftp": port = 21; break;
                        case "ssh": port = 22; break;
                        case "telnet": port = 23; break;
                        case "smtp": port = 25; break;
                        case "time": port = 37; break;
                        case "nameserver": port = 42; break;
                        case "nicname": port = 43; break;
                        case "domain": port = 53; break;
                        case "gopher": port = 70; break;
                        case "finger": port = 79; break;
                        case "http": port = 80; break;
                        case "hosts2-ns": port = 81; break;
                        case "kerberos": port = 88; break;
                        case "hostname": port = 101; break;
                        case "iso-tsap": port = 102; break;
                        case "rtelnet": port = 107; break;
                        case "pop2": port = 109; break;
                        case "pop3": port = 110; break;
                        case "sunrpc": port = 111; break;
                        case "auth": port = 113; break;
                        case "uucp-path": port = 117; break;
                        case "sqlserv": port = 118; break;
                        case "nntp": port = 119; break;
                        case "epmap": port = 135; break;
                        case "netbios-ns": port = 137; break;
                        case "netbios-ssn": port = 139; break;
                        case "imap": port = 143; break;
                        case "sql-net": port = 150; break;
                        case "sqlsrv": port = 156; break;
                        case "pcmail-srv": port = 158; break;
                        case "print-srv": port = 170; break;
                        case "bgp": port = 179; break;
                        case "irc": port = 194; break;
                        case "rtsps": port = 322; break;
                        case "mftp": port = 349; break;
                        case "ldap": port = 389; break;
                        case "https": port = 443; break;
                        case "microsoft-ds": port = 445; break;
                        case "kpasswd": port = 464; break;
                        case "crs": port = 507; break;
                        case "exec": port = 512; break;
                        case "login": port = 513; break;
                        case "cmd": port = 514; break;
                        case "printer": port = 515; break;
                        case "efs": port = 520; break;
                        case "ulp": port = 522; break;
                        case "tempo": port = 526; break;
                        case "irc-serv": port = 529; break;
                        case "courier": port = 530; break;
                        case "conference": port = 531; break;
                        case "netnews": port = 532; break;
                        case "uucp": port = 540; break;
                        case "klogin": port = 543; break;
                        case "kshell": port = 544; break;
                        case "dhcpv6-client": port = 546; break;
                        case "dhcpv6-server": port = 547; break;
                        case "afpovertcp": port = 548; break;
                        case "rtsp": port = 554; break;
                        case "remotefs": port = 556; break;
                        case "nntps": port = 563; break;
                        case "whoami": port = 565; break;
                        case "ms-shuttle": port = 568; break;
                        case "ms-rome": port = 569; break;
                        case "http-rpc-epmap": port = 593; break;
                        case "hmmp-ind": port = 612; break;
                        case "hmmp-op": port = 613; break;
                        case "ldaps": port = 636; break;
                        case "doom": port = 666; break;
                        case "msexch-routing": port = 691; break;
                        case "kerberos-adm": port = 749; break;
                        case "mdbs_daemon": port = 800; break;
                        case "ftps-data": port = 989; break;
                        case "ftps": port = 990; break;
                        case "telnets": port = 992; break;
                        case "imaps": port = 993; break;
                        case "ircs": port = 994; break;
                        case "pop3s": port = 995; break;
                        case "kpop": port = 1109; break;
                        case "nfsd-status": port = 1110; break;
                        case "nfa": port = 1155; break;
                        case "activesync": port = 1034; break;
                        case "opsmgr": port = 1270; break;
                        case "ms-sql-s": port = 1433; break;
                        case "ms-sql-m": port = 1434; break;
                        case "ms-sna-server": port = 1477; break;
                        case "ms-sna-base": port = 1478; break;
                        case "wins": port = 1512; break;
                        case "ingreslock": port = 1524; break;
                        case "stt": port = 1607; break;
                        case "pptconference": port = 1711; break;
                        case "pptp": port = 1723; break;
                        case "msiccp": port = 1731; break;
                        case "remote-winsock": port = 1745; break;
                        case "ms-streaming": port = 1755; break;
                        case "msmq": port = 1801; break;
                        case "msnp": port = 1863; break;
                        case "ssdp": port = 1900; break;
                        case "close-combat": port = 1944; break;
                        case "knetd": port = 2053; break;
                        case "mzap": port = 2106; break;
                        case "qwave": port = 2177; break;
                        case "directplay": port = 2234; break;
                        case "ms-olap3": port = 2382; break;
                        case "ms-olap4": port = 2383; break;
                        case "ms-olap1": port = 2393; break;
                        case "ms-olap2": port = 2394; break;
                        case "ms-theater": port = 2460; break;
                        case "wlbs": port = 2504; break;
                        case "ms-v-worlds": port = 2525; break;
                        case "sms-rcinfo": port = 2701; break;
                        case "sms-xfer": port = 2702; break;
                        case "sms-chat": port = 2703; break;
                        case "sms-remctrl": port = 2704; break;
                        case "msolap-ptp2": port = 2725; break;
                        case "icslap": port = 2869; break;
                        case "cifs": port = 3020; break;
                        case "xbox": port = 3074; break;
                        case "ms-dotnetster": port = 3126; break;
                        case "ms-rule-engine": port = 3132; break;
                        case "msft-gc": port = 3268; break;
                        case "msft-gc-ssl": port = 3269; break;
                        case "ms-cluster-net": port = 3343; break;
                        case "ms-wbt-server": port = 3389; break;
                        case "ms-la": port = 3535; break;
                        case "pnrp-port": port = 3540; break;
                        case "teredo": port = 3544; break;
                        case "p2pgroup": port = 3587; break;
                        case "upnp-discovery": port = 3702; break;
                        case "dvcprov-port": port = 3776; break;
                        case "msfw-control": port = 3847; break;
                        case "msdts1": port = 3882; break;
                        case "sdp-portmapper": port = 3935; break;
                        case "net-device": port = 4350; break;
                        case "ipsec-msft": port = 4500; break;
                        case "llmnr": port = 5355; break;
                        case "rrac": port = 5678; break;
                        case "dccm": port = 5679; break;
                        case "ms-licensing": port = 5720; break;
                        case "directplay8": port = 6073; break;
                        case "man": port = 9535; break;
                        case "rasadv": port = 9753; break;
                        case "imip-channels": port = 11320; break;
                        case "directplaysrvr": port = 47624; break;
                        default: return false;
                    }
                    #endregion
                    return true;
                }
            }
        }
    }
}
