using Microsoft.Win32;
using System;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FixSysTime
{
    internal class Program
    {
        static string POTW = "";
        static string Country = "";

        [DllImport("Kernel32.dll")]
        public static extern bool SetLocalTime(ref SYSTEMTIME Time);
        static void Main(string[] args)
        {
            if (CheckForInternetConnection())
            {
                CheckRegedit();

                setSystemTime();
            }
            else
            {
                Environment.Exit(0);
            }
        }
        static void setSystemTime()
        {
            SYSTEMTIME st = getWebRequest($"{POTW}/{Country}");

            SetLocalTime(ref st);
        }
        static void CreateRegedit()
        {
            string resp = getInfoIP(getIPuser());
            if(resp == null)
            {
                RegistryKey rk = Registry.CurrentUser.CreateSubKey("Software\\FixSysTime");
                rk.SetValue("POTW", "Europe");
                rk.SetValue("Country", "Moscow");
                rk.Close();
            }
            else
            {
                RegistryKey rk = Registry.CurrentUser.CreateSubKey("Software\\FixSysTime");
                rk.SetValue("POTW", resp.Remove(resp.IndexOf("/")));
                rk.SetValue("Country", resp.Remove(0, resp.IndexOf("/") + 1));
                rk.Close();
            }


        }
        static string getIPuser()
        {
            return new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
        }
        static string getInfoIP(string ip)
        {
            string resp_res = "";
            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"http://ip-api.com/json/{ip}"))
            {
                HttpClient http = new HttpClient();
                HttpResponseMessage resp = http.SendAsync(req).Result;
                resp_res = resp.Content.ReadAsStringAsync().Result;
            }
            return Regex.Match(resp_res, "\"timezone\":\"(.*?)\"").Groups[1].Value;
        }
        static void ReadRegedit()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("Software\\FixSysTime");
            POTW = rk!.GetValue("POTW").ToString();
            Country = rk!.GetValue("Country").ToString();
        }
        static void CheckRegedit()
        {
            if (Registry.CurrentUser.OpenSubKey("Software\\FixSysTime") == null)
            {
                CreateRegedit();
            }
            else
            {
                ReadRegedit();
            }
        }
        static void funcSetTime()
        {
            DateTime dt = DateTime.Parse("19.01.2023 7:30:27");
            SYSTEMTIME st = new SYSTEMTIME();
            st.FromDateTime(dt);
            SetLocalTime(ref st);
        }
        static SYSTEMTIME getWebRequest(string url)
        {
            string resp_res = "";
            SYSTEMTIME st = new SYSTEMTIME();

            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"http://worldtimeapi.org/api/timezone/{url}")) {
                HttpClient http = new HttpClient();
                HttpResponseMessage resp = http.SendAsync(req).Result;
                resp_res = resp.Content.ReadAsStringAsync().Result;
            }

            st.wDayOfWeek = Convert.ToUInt16(Regex.Match(resp_res, "\"day_of_week\":([0-9])").Groups[1].Value);
            resp_res = Regex.Match(resp_res, "\"datetime\":\"(.*?)\"").Groups[1].Value;
            Match rgx = Regex.Match(resp_res, "([0-9][0-9][0-9][0-9])-([0-9][0-9])-([0-9][0-9])T([0-9][0-9]):([0-9][0-9]):([0-9][0-9]).([0-9][0-9][0-9])");

            st.wYear = Convert.ToUInt16(rgx.Groups[1].Value);
            st.wMonth = Convert.ToUInt16(rgx.Groups[2].Value);
            st.wDay = Convert.ToUInt16(rgx.Groups[3].Value);
            st.wHour = Convert.ToUInt16(rgx.Groups[4].Value);
            st.wMinute = Convert.ToUInt16(rgx.Groups[5].Value);
            st.wSecond = Convert.ToUInt16(rgx.Groups[6].Value);
            st.wMilliseconds = Convert.ToUInt16(rgx.Groups[7].Value);

            return st;
        }
        public static bool CheckForInternetConnection(int timeoutMs = 10000, string url = null)
        {
            try
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("fa") => // Iran
                        "http://www.aparat.com",
                    { Name: var n } when n.StartsWith("zh") => // China
                        "http://www.baidu.com",
                    _ =>
                        "http://www.gstatic.com/generate_204",
                };

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return true;
            }
            catch
            {
                return false;
            }
        }
        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;


            public void FromDateTime(DateTime time)
            {
                wYear = (ushort)time.Year;
                wMonth = (ushort)time.Month;
                wDayOfWeek = (ushort)time.DayOfWeek;
                wDay = (ushort)time.Day;
                wHour = (ushort)time.Hour;
                wMinute = (ushort)time.Minute;
                wSecond = (ushort)time.Second;
                wMilliseconds = (ushort)time.Millisecond;
            }


            public DateTime ToDateTime()
            {
                return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds);
            }

            public static DateTime ToDateTime(SYSTEMTIME time)
            {
                return time.ToDateTime();
            }


        }
    }
}