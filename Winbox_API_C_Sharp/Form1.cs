using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Data.Sql;
using System.Data.SqlClient;

namespace Winbox_API_C_Sharp
{
    public partial class Form1 : Form
    {
        class MK
        {
            Stream connection;
            TcpClient con;

            public MK(string ip = "172.18.112.237")
            {
                con = new TcpClient();
                con.Connect(ip, 8728);
                connection = (Stream)con.GetStream();
            }
            public void Close()
            {
                connection.Close();
                con.Close();
            }
            public bool Login(string username = "api", string password = "api")
            {
                Send("/login", true);
                string hash = Read()[0].Split(new string[] { "ret=" }, StringSplitOptions.None)[1];
                Send("/login");
                Send("=name=" + username);
                Send("=response=00" + EncodePassword(password, hash), true);
                if (Read()[0] == "!done")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public void Send(string co)
            {
                byte[] bajty = Encoding.ASCII.GetBytes(co.ToCharArray());
                byte[] velikost = EncodeLength(bajty.Length);

                connection.Write(velikost, 0, velikost.Length);
                connection.Write(bajty, 0, bajty.Length);
            }
            public void Send(string co, bool endsentence)
            {
                byte[] bajty = Encoding.ASCII.GetBytes(co.ToCharArray());
                byte[] velikost = EncodeLength(bajty.Length);
                connection.Write(velikost, 0, velikost.Length);
                connection.Write(bajty, 0, bajty.Length);
                connection.WriteByte(0);
            }
            public List<string> Read()
            {
                List<string> output = new List<string>();
                string o = "";
                byte[] tmp = new byte[4];
                long count;
                while (true)
                {
                    tmp[3] = (byte)connection.ReadByte();
                    //if(tmp[3] == 220) tmp[3] = (byte)connection.ReadByte(); it sometimes happend to me that 
                    //mikrotik send 220 as some kind of "bonus" between words, this fixed things, not sure about it though
                    if (tmp[3] == 0)
                    {
                        output.Add(o);
                        if (o.Substring(0, 5) == "!done")
                        {
                            break;
                        }
                        else
                        {
                            o = "";
                            continue;
                        }
                    }
                    else
                    {
                        if (tmp[3] < 0x80)
                        {
                            count = tmp[3];
                        }
                        else
                        {
                            if (tmp[3] < 0xC0)
                            {
                                int tmpi = BitConverter.ToInt32(new byte[] { (byte)connection.ReadByte(), tmp[3], 0, 0 }, 0);
                                count = tmpi ^ 0x8000;
                            }
                            else
                            {
                                if (tmp[3] < 0xE0)
                                {
                                    tmp[2] = (byte)connection.ReadByte();
                                    int tmpi = BitConverter.ToInt32(new byte[] { (byte)connection.ReadByte(), tmp[2], tmp[3], 0 }, 0);
                                    count = tmpi ^ 0xC00000;
                                }
                                else
                                {
                                    if (tmp[3] < 0xF0)
                                    {
                                        tmp[2] = (byte)connection.ReadByte();
                                        tmp[1] = (byte)connection.ReadByte();
                                        int tmpi = BitConverter.ToInt32(new byte[] { (byte)connection.ReadByte(), tmp[1], tmp[2], tmp[3] }, 0);
                                        count = tmpi ^ 0xE0000000;
                                    }
                                    else
                                    {
                                        if (tmp[3] == 0xF0)
                                        {
                                            tmp[3] = (byte)connection.ReadByte();
                                            tmp[2] = (byte)connection.ReadByte();
                                            tmp[1] = (byte)connection.ReadByte();
                                            tmp[0] = (byte)connection.ReadByte();
                                            count = BitConverter.ToInt32(tmp, 0);
                                        }
                                        else
                                        {
                                            //Error in packet reception, unknown length
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < count; i++)
                    {
                        o += (Char)connection.ReadByte();
                    }
                }
                return output;
            }
            byte[] EncodeLength(int delka)
            {
                if (delka < 0x80)
                {
                    byte[] tmp = BitConverter.GetBytes(delka);
                    return new byte[1] { tmp[0] };
                }
                if (delka < 0x4000)
                {
                    byte[] tmp = BitConverter.GetBytes(delka | 0x8000);
                    return new byte[2] { tmp[1], tmp[0] };
                }
                if (delka < 0x200000)
                {
                    byte[] tmp = BitConverter.GetBytes(delka | 0xC00000);
                    return new byte[3] { tmp[2], tmp[1], tmp[0] };
                }
                if (delka < 0x10000000)
                {
                    byte[] tmp = BitConverter.GetBytes(delka | 0xE0000000);
                    return new byte[4] { tmp[3], tmp[2], tmp[1], tmp[0] };
                }
                else
                {
                    byte[] tmp = BitConverter.GetBytes(delka);
                    return new byte[5] { 0xF0, tmp[3], tmp[2], tmp[1], tmp[0] };
                }
            }

            public string EncodePassword(string Password, string hash)
            {
                byte[] hash_byte = new byte[hash.Length / 2];
                for (int i = 0; i <= hash.Length - 2; i += 2)
                {
                    hash_byte[i / 2] = Byte.Parse(hash.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                }
                byte[] heslo = new byte[1 + Password.Length + hash_byte.Length];
                heslo[0] = 0;
                Encoding.ASCII.GetBytes(Password.ToCharArray()).CopyTo(heslo, 1);
                hash_byte.CopyTo(heslo, 1 + Password.Length);

                Byte[] hotovo;
                System.Security.Cryptography.MD5 md5;

                md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();

                hotovo = md5.ComputeHash(heslo);

                //Convert encoded bytes back to a 'readable' string
                string navrat = "";
                foreach (byte h in hotovo)
                {
                    navrat += h.ToString("x2");
                }
                return navrat;
            }
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string registration_table;
            string[,] resource_input = new string[2, 0];
            string resources;
            string registration_data;
            string rb_wireless_data;           
            //resources = "!re.tag=sss=uptime=4d15h20m38s=version=6.27=build-time=Feb/11/2015 13:24:13=free-memory=36741120=total-memory=67108864=cpu=MIPS 24Kc V7.4=cpu-count=1=cpu-frequency=400=cpu-load=28=free-hdd-space=116649984=total-hdd-space=134217728=write-sect-since-reboot=1070=write-sect-total=8926=bad-blocks=0=architecture-name=mipsbe=board-name=RB OmniTIK UPA-5HnD=platform=MikroTik";
            //resource_respon_cleanup(resources);
            //registration_table = command("/interface/wireless/registration-table/getall");
            //rb_wireless_data = command("/interface/wireless/print");
            rb_wireless_data = ("!re.tag=sss=.id=*6=default-name=wlan1=name=Snake_AP1=mtu=1500=l2mtu=1600=mac-address=4C:5E:0C:AF:58:0F=arp=enabled=disable-running-check=false=interface-type=Atheros AR92xx=radio-name=Snake_AP1=mode=ap-bridge=ssid=http:\\ctwug.za.netSnake_AP1=area==frequency-mode=superchannel=country=ireland=antenna-gain=0=frequency=5475=band=5ghz-onlyn=channel-width=20/40mhz-Ce=scan-list=default,5400-5500=wireless-protocol=nv2=rate-set=configured=supported-rates-a/g==basic-rates-a/g==max-station-count=2007=distance=dynamic=tx-power-mode=default=noise-floor-threshold=default=nv2-noise-floor-offset=default=dfs-mode=none=vlan-mode=no-tag=vlan-id=1=wds-mode=disabled=wds-default-bridge=none=wds-default-cost=100=wds-cost-range=50-150=wds-ignore-ssid=false=update-stats-interval=disabled=bridge-mode=enabled=default-authentication=true=default-forwarding=true=default-ap-tx-limit=0=default-client-tx-limit=0=proprietary-extensions=post-2.9.25=wmm-support=disabled=hide-ssid=false=security-profile=default=interworking-profile=disabled=disconnect-timeout=3s=on-fail-retry-time=100ms=preamble-mode=both=compression=false=allow-sharedkey=false=station-bridge-clone-mac=00:00:00:00:00:00=ampdu-priorities=0=guard-interval=any=ht-supported-mcs=mcs-0,mcs-1,mcs-2,mcs-3,mcs-4,mcs-9,mcs-10,mcs-16,mcs-17,mcs-18,mcs-19,mcs-20,mcs-21,mcs-22,mcs-23=ht-basic-mcs=mcs-0,mcs-1,mcs-2,mcs-3,mcs-4,mcs-9,mcs-10=tx-chains=0,1=rx-chains=0,1=amsdu-limit=8192=amsdu-threshold=8192=tdma-period-size=2=nv2-queue-count=2=nv2-qos=default=nv2-cell-radius=10=nv2-security=disabled=nv2-preshared-key==hw-retries=7=frame-lifetime=0=adaptive-noise-immunity=none=hw-fragmentation-threshold=disabled=hw-protection-mode=none=hw-protection-threshold=0=frequency-offset=0=rate-selection=advanced=multicast-helper=default=multicast-buffering=enabled=keepalive-frames=enabled=running=true=disabled=false=comment=client;512;!done.tag=sss");
            rb_wireless_data_cleanup(rb_wireless_data);
            //StreamWriter outputStream = File.CreateText(@"c:\data\data.csv");
            //outputStream.WriteLine(rb_wireless_data);
            //outputStream.Close();
            // registration_data = "!re.tag=sss=.id=*2=interface=Snake_AP1=radio-name=Gixxer->SnakeOmni=mac-address=4C:5E:0C:0F:2B:3B=ap=false=wds=false=bridge=false=rx-rate=26Mbps-20MHz/2S=tx-rate=60Mbps-40MHz/2S/SGI=packets=37184706,33176656=bytes=2871035632,2916519196=frames=33370529,16861527=frame-bytes=2887997970,2977703980=uptime=4d15h19m47s=last-activity=0ms=signal-strength=-76=signal-to-noise=40=signal-strength-ch0=-77=signal-strength-ch1=-82=tx-signal-strength-ch0=-81=tx-signal-strength-ch1=-76=strength-at-rates=-76@6Mbps 0ms,-75@HT20-0 27m45s620ms,-77@HT20-1 150ms,-76@HT20-2 780ms,-78@HT20-3 680ms,-78@HT20-4 890ms,-80@HT40-0 480ms,-80@HT40-1 290ms,-80@HT40-2 300ms,-80@HT40-3 20s830ms,-79@HT40-4 35m3s770ms=tx-signal-strength=-75=tx-ccq=76=rx-ccq=33=distance=2=routeros-version=6.19=last-ip=172.18.116.81=tx-rate-set=BW:1x-2x SGI:2x HT:0-4,9-10=tdma-timing-offset=10=tdma-tx-size=496=tdma-rx-size=1008=tdma-retx=24=tdma-winfull=0";
            //registration_respon_cleanup(registration_data);           
        }
        private string rb_wireless_data_cleanup(string rb_wireless_data)
        {
            int i = 0;
            int n = 0;
            int a = 0;
            int temp0 = 0;
            int inner_loop = 0;
            int placeholder = 0;
            string value1;
            DateTime localdate = DateTime.Now;
            string[] resource_names = new string[100];
            string[] resource_values = new string[100];
            string[] output = new string[100];
            char[] separator = { '=' };

            //SqlConnection db_connection;
            //db_connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB; AttachDbFilename=c:\users\keenan\documents\visual studio 2015\Projects\Winbox_API_C_Sharp\Winbox_API_C_Sharp\Winbox_API_DB.mdf; Integrated Security=True");
            //db_connection.Open();
            //SqlCommand rb_resource_name_cmd;
            rb_wireless_data = rb_wireless_data.Remove(0, 37);
            rb_wireless_data = rb_wireless_data.Remove(rb_wireless_data.IndexOf("configured"));
            rb_wireless_data = rb_wireless_data.ToUpper();

            for (int k = 0; k < rb_wireless_data.Length; k++)
            {

                if (rb_wireless_data[k] == '=')
                {
                    n++;
                }
                if (n == 2)
                {
                    value1 = rb_wireless_data.Substring(temp0, (k - temp0));
                    string[] rb_resources = value1.Split('=');
                    temp0 = k;
                    n = 0;
                    for (int loop = 0; loop < rb_resources.Length; loop++)
                    {
                        placeholder = 0;
                        if (rb_resources[loop] != "")
                        {
                            if (inner_loop % 2 == 0)
                            {
                                resource_names[i] = rb_resources[loop];
                                MessageBox.Show(resource_names[i]);
                            }
                            else
                            {
                                resource_values[i] = rb_resources[loop];
                                placeholder = 1;
                                resource_names[i] = rb_resources[loop];
                                output[a] = resource_values[i];
                                MessageBox.Show(output[a]);
                                a++;
                            }
                            inner_loop++;
                        }
                        if (placeholder == 1)
                        {
                            i++;
                        }
                    }
                }
            }
            /*rb_resource_name_cmd = new SqlCommand("INSERT INTO rb_wireless_data_tbl (name,mtu,l2mtu,mac_address,arp,disable_running_check,interface_type,radio_name,mode,ssid,frequency_mode,country,antenna_gain,frequency,band,channel_width,scan_list,wireless_protocol) VALUES (@name,@mtu,@l2mtu,@mac_address,@arp,@disable_running_check,@interface_type,@radio_name,@mode,@ssid,@frequency_mode,@country,@antenna_gain,@frequency,@band,@channel_width,@scan_list,@wireless_protocol)", db_connection);
            rb_resource_name_cmd.Parameters.AddWithValue("@name", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@name"].Value = output[0];
            rb_resource_name_cmd.Parameters.AddWithValue("@mtu", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@mtu"].Value = output[1];
            rb_resource_name_cmd.Parameters.AddWithValue("@l2mtu", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@l2mtu"].Value = output[2];
            rb_resource_name_cmd.Parameters.AddWithValue("@mac_address", SqlDbType.NVarChar);
            rb_resource_name_cmd.Parameters["@mac_address"].Value = output[3];
            rb_resource_name_cmd.Parameters.AddWithValue("@arp", SqlDbType.NVarChar);
            rb_resource_name_cmd.Parameters["@arp"].Value = output[4];
            rb_resource_name_cmd.Parameters.AddWithValue("@disable_running_check", SqlDbType.NVarChar);
            rb_resource_name_cmd.Parameters["@disable_running_check"].Value = output[5];
            rb_resource_name_cmd.Parameters.AddWithValue("@interface_type", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@interface_type"].Value = output[6];
            rb_resource_name_cmd.Parameters.AddWithValue("@radio_name", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@radio_name"].Value = output[7];
            rb_resource_name_cmd.Parameters.AddWithValue("@mode", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@mode"].Value = output[8];
            rb_resource_name_cmd.Parameters.AddWithValue("@ssid", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@ssid"].Value = output[9];
            rb_resource_name_cmd.Parameters.AddWithValue("@frequency_mode", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@frequency_mode"].Value = output[10];
            rb_resource_name_cmd.Parameters.AddWithValue("@country", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@country"].Value = output[11];
            rb_resource_name_cmd.Parameters.AddWithValue("@antenna_gain", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@antenna_gain"].Value = output[12];
            rb_resource_name_cmd.Parameters.AddWithValue("@frequency", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@frequency"].Value = output[13];
            rb_resource_name_cmd.Parameters.AddWithValue("@band", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@band"].Value = output[14];
            rb_resource_name_cmd.Parameters.AddWithValue("@signal_to_noise", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@signal_to_noise"].Value = output[15];
            rb_resource_name_cmd.Parameters.AddWithValue("@channel_width", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@channel_width"].Value = output[16];
            rb_resource_name_cmd.Parameters.AddWithValue("@scan_list", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@scan_list"].Value = output[17];
            rb_resource_name_cmd.Parameters.AddWithValue("@wireless_protocol", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@wireless_protocol"].Value = output[18];
            rb_resource_name_cmd.ExecuteNonQuery();*/
            return ("");
        }


        private string resource_respon_cleanup(string resources)
         {
             int i = 0;
             int n = 0;
             int a = 0;
             int temp0 = 0;
             int inner_loop = 0;
             int placeholder = 0;
             string value1;
             DateTime localdate = DateTime.Now;
             string[] resource_names = new string[50];
             string[] resource_values = new string[50];
             string[] output = new string[16];
             char[] separator = { '=' };
             SqlConnection db_connection;
             db_connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB; AttachDbFilename=c:\users\keenan\documents\visual studio 2015\Projects\Winbox_API_C_Sharp\Winbox_API_C_Sharp\Winbox_API_DB.mdf; Integrated Security=True");
             db_connection.Open();
             SqlCommand rb_resource_name_cmd;
             resources = resources.Remove(0, 12);
             resources = resources.ToUpper();
             for (int k = 0; k < resources.Length; k++)
             {

                 if (resources[k] == '=')
                 {
                     n++;
                 }
                 if (n == 2)
                 {
                    value1 = resources.Substring(temp0, (k - temp0));
                     string[] rb_resources = value1.Split('=');
                     temp0 = k;
                     n = 0;
                     for (int loop = 0; loop < rb_resources.Length; loop++)
                     {
                         placeholder = 0;
                         if (rb_resources[loop] != "")
                         {
                             if (inner_loop % 2 == 0)
                             {
                                 resource_names[i] = rb_resources[loop];
                             }
                             else
                             {
                                 resource_values[i] = rb_resources[loop];
                                 placeholder = 1;                                 
                                 output[a] = resource_values[i];
                                 a++;
                            }
                             inner_loop++;
                         }
                         if (placeholder == 1)
                         {
                             i++;
                         }
                     }
                 }
             }        
                            rb_resource_name_cmd = new SqlCommand("INSERT INTO rb_resources_tbl (uptime,version,build_time,free_memory,total_memory,cpu_mips,cpu_count,cpu_freq,cpu_load,free_hdd_space,total_hdd_space,write_sect_since_reboot,write_sect_total,bad_blocks,architecture_name,board_name) VALUES (@uptime,@version,@build_time,@free_memory,@total_memory,@cpu_mips,@cpu_count,@cpu_freq,@cpu_load,@free_hdd_space,@total_hdd_space,@write_sect_since_reboot,@write_sect_total,@bad_blocks,@architecture_name,@board_name )", db_connection);
                            rb_resource_name_cmd.Parameters.AddWithValue("@uptime", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@uptime"].Value = output[0];
                            rb_resource_name_cmd.Parameters.AddWithValue("@version", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@version"].Value = output[1];
                            rb_resource_name_cmd.Parameters.AddWithValue("@build_time", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@build_time"].Value = output[2];
                            rb_resource_name_cmd.Parameters.AddWithValue("@free_memory", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@free_memory"].Value = output[3];
                            rb_resource_name_cmd.Parameters.AddWithValue("@total_memory", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@total_memory"].Value = output[4];
                            rb_resource_name_cmd.Parameters.AddWithValue("@cpu_mips", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@cpu_mips"].Value = output[5];
                            rb_resource_name_cmd.Parameters.AddWithValue("@cpu_count", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@cpu_count"].Value = output[6];
                            rb_resource_name_cmd.Parameters.AddWithValue("@cpu_freq", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@cpu_freq"].Value = output[7];
                            rb_resource_name_cmd.Parameters.AddWithValue("@cpu_load", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@cpu_load"].Value = output[8];
                            rb_resource_name_cmd.Parameters.AddWithValue("@free_hdd_space", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@free_hdd_space"].Value = output[9];
                            rb_resource_name_cmd.Parameters.AddWithValue("@total_hdd_space", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@total_hdd_space"].Value = output[10];
                            rb_resource_name_cmd.Parameters.AddWithValue("@write_sect_since_reboot", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@write_sect_since_reboot"].Value = output[11];
                            rb_resource_name_cmd.Parameters.AddWithValue("@write_sect_total", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@write_sect_total"].Value = output[12];
                            rb_resource_name_cmd.Parameters.AddWithValue("@bad_blocks", SqlDbType.Int);
                            rb_resource_name_cmd.Parameters["@bad_blocks"].Value = output[13];
                            rb_resource_name_cmd.Parameters.AddWithValue("@architecture_name", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@architecture_name"].Value = output[14];
                            rb_resource_name_cmd.Parameters.AddWithValue("@board_name", SqlDbType.VarChar);
                            rb_resource_name_cmd.Parameters["@board_name"].Value = output[15];
                            rb_resource_name_cmd.ExecuteNonQuery();       
            return "";

         }
        private string registration_respon_cleanup(string registration_data)
        {
            int i = 0;
            int n = 0;
            int a = 0;
            int temp0 = 0;
            int inner_loop = 0;
            int placeholder = 0;
            string value1;
            DateTime localdate = DateTime.Now;
            string[] resource_names = new string[50];
            string[] resource_values = new string[50];
            string[] output = new string[32];
            char[] separator = { '=' };
            SqlConnection db_connection;
            db_connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB; AttachDbFilename=c:\users\keenan\documents\visual studio 2015\Projects\Winbox_API_C_Sharp\Winbox_API_C_Sharp\Winbox_API_DB.mdf; Integrated Security=True");
            db_connection.Open();
            SqlCommand rb_resource_name_cmd;
            registration_data = registration_data.Remove(0, 19);
            registration_data = registration_data.ToUpper();

            for (int k = 0; k < registration_data.Length; k++)
            {

                if (registration_data[k] == '=')
                {
                    n++;
                }
                if (n == 2)
                {
                    value1 = registration_data.Substring(temp0, (k - temp0));
                    string[] rb_resources = value1.Split('=');
                    temp0 = k;
                    n = 0;
                    for (int loop = 0; loop < rb_resources.Length; loop++)
                    {
                        placeholder = 0;
                        if (rb_resources[loop] != "")
                        {
                            if (inner_loop % 2 == 0)
                            {
                                resource_names[i] = rb_resources[loop];
                            }
                            else
                            {
                                resource_values[i] = rb_resources[loop];
                                placeholder = 1;
                                resource_names[i] = rb_resources[loop];
                                output[a] = resource_values[i];
                                a++;
                            }
                            inner_loop++;
                        }
                        if (placeholder == 1)
                        {
                            i++;
                        }
                    }
                }
            }
            rb_resource_name_cmd = new SqlCommand("INSERT INTO rb_registration_tbl (interface,radio_name,mac_address,ap,wds,bridge,rx_rate,tx_rate,packets,bytes,frames,frame_bytes,uptime,last_activity,signal_strength,signal_to_noise,signal_strength_ch0,signal_strength_ch1,tx_signal_strength_ch0,tx_signal_strength_ch1,strength_at_rates,tx_signal_strength,tx_ccq,rx_ccq,distance,router_os_version,last_ip,tx_rate_set,tdma_timing_offset,tdma_tx_size,tdma_rx_size,tdma_retx,tdma_winfull) VALUES (@interface,@radio_name,@mac_address,@ap,@wds,@bridge,@rx_rate,@tx_rate,@packets,@bytes,@frames,@frame_bytes,@uptime,@last_activity,@signal_strength,@signal_to_noise,@signal_strength_ch0,@signal_strength_ch1,@tx_signal_strength_ch0,@tx_signal_strength_ch1,@strength_at_rates,@tx_signal_strength,@tx_ccq,@rx_ccq,@distance,@router_os_version,@last_ip,@tx_rate_set,@tdma_timing_offset,@tdma_tx_size,@tdma_rx_size,@tdma_retx,@tdma_winfull )", db_connection);
            rb_resource_name_cmd.Parameters.AddWithValue("@interface", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@interface"].Value = output[0];
            rb_resource_name_cmd.Parameters.AddWithValue("@radio_name", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@radio_name"].Value = output[1];
            rb_resource_name_cmd.Parameters.AddWithValue("@mac_address", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@mac_address"].Value = output[2];
            rb_resource_name_cmd.Parameters.AddWithValue("@ap", SqlDbType.NVarChar);
            rb_resource_name_cmd.Parameters["@ap"].Value = output[3];
            rb_resource_name_cmd.Parameters.AddWithValue("@wds", SqlDbType.NVarChar);
            rb_resource_name_cmd.Parameters["@wds"].Value = output[4];
            rb_resource_name_cmd.Parameters.AddWithValue("@bridge", SqlDbType.NVarChar);
            rb_resource_name_cmd.Parameters["@bridge"].Value = output[5];
            rb_resource_name_cmd.Parameters.AddWithValue("@rx_rate", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@rx_rate"].Value = output[6];
            rb_resource_name_cmd.Parameters.AddWithValue("@tx_rate", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tx_rate"].Value = output[7];
            rb_resource_name_cmd.Parameters.AddWithValue("@packets", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@packets"].Value = output[8];
            rb_resource_name_cmd.Parameters.AddWithValue("@bytes", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@bytes"].Value = output[9];
            rb_resource_name_cmd.Parameters.AddWithValue("@frames", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@frames"].Value = output[10];
            rb_resource_name_cmd.Parameters.AddWithValue("@frame_bytes", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@frame_bytes"].Value = output[11];
            rb_resource_name_cmd.Parameters.AddWithValue("@uptime", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@uptime"].Value = output[12];
            rb_resource_name_cmd.Parameters.AddWithValue("@last_activity", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@last_activity"].Value = output[13];
            rb_resource_name_cmd.Parameters.AddWithValue("@signal_strength", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@signal_strength"].Value = output[14];
            rb_resource_name_cmd.Parameters.AddWithValue("@signal_to_noise", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@signal_to_noise"].Value = output[15];
            rb_resource_name_cmd.Parameters.AddWithValue("@signal_strength_ch0", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@signal_strength_ch0"].Value = output[16];
            rb_resource_name_cmd.Parameters.AddWithValue("@signal_strength_ch1", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@signal_strength_ch1"].Value = output[17];
            rb_resource_name_cmd.Parameters.AddWithValue("@tx_signal_strength_ch0", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tx_signal_strength_ch0"].Value = output[18];
            rb_resource_name_cmd.Parameters.AddWithValue("@tx_signal_strength_ch1", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tx_signal_strength_ch1"].Value = output[19];
            rb_resource_name_cmd.Parameters.AddWithValue("@strength_at_rates", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@strength_at_rates"].Value = output[20];
            rb_resource_name_cmd.Parameters.AddWithValue("@tx_signal_strength", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tx_signal_strength"].Value = output[20];
            rb_resource_name_cmd.Parameters.AddWithValue("@tx_ccq", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tx_ccq"].Value = output[21];
            rb_resource_name_cmd.Parameters.AddWithValue("@rx_ccq", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@rx_ccq"].Value = output[22];
            rb_resource_name_cmd.Parameters.AddWithValue("@distance", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@distance"].Value = output[23];
            rb_resource_name_cmd.Parameters.AddWithValue("@router_os_version", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@router_os_version"].Value = output[24];
            rb_resource_name_cmd.Parameters.AddWithValue("@last_ip", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@last_ip"].Value = output[25];
            rb_resource_name_cmd.Parameters.AddWithValue("@tx_rate_set", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tx_rate_set"].Value = output[26];
            rb_resource_name_cmd.Parameters.AddWithValue("@tdma_timing_offset", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tdma_timing_offset"].Value = output[27];
            rb_resource_name_cmd.Parameters.AddWithValue("@tdma_tx_size", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tdma_tx_size"].Value = output[28];
            rb_resource_name_cmd.Parameters.AddWithValue("@tdma_rx_size", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tdma_rx_size"].Value = output[29];
            rb_resource_name_cmd.Parameters.AddWithValue("@tdma_retx", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tdma_retx"].Value = output[30];
            rb_resource_name_cmd.Parameters.AddWithValue("@tdma_winfull", SqlDbType.VarChar);
            rb_resource_name_cmd.Parameters["@tdma_winfull"].Value = output[31];
            rb_resource_name_cmd.ExecuteNonQuery();
            return ("");
        }

        
        private string command(string command)
        {
            string output = "";
            MK mikrotik = new MK("172.18.112.237");

            if (!mikrotik.Login("api", "api"))
            {
                MessageBox.Show("Could not log in");
                mikrotik.Close();
                return "false";
            }

            mikrotik.Send(command);
            mikrotik.Send(".tag=sss", true);

            foreach (string response in mikrotik.Read())
            {
                output += response;
            }
            return output;
        }
    }
}



