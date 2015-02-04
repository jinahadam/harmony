using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace harmony
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            var cal = Properties.Settings.Default["Calibration"];
            PrefCal.Text = cal.ToString();

            var roll = Properties.Settings.Default["Roll"];
            PrefRoll.Text = roll.ToString();

            var utc = Properties.Settings.Default["UTCDiff"];
            PrefUTC.Text = utc.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["Calibration"] = PrefCal.Text;
            Properties.Settings.Default["Roll"] = PrefRoll.Text;
            Properties.Settings.Default["UTCDiff"] = PrefUTC.Text;
            MainWindow.main.Status = PrefCal.Text;

            Properties.Settings.Default.Save();
            this.Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
          //  Properties.Settings.Default["Calibration"] = PrefCal.Text;
          //  Properties.Settings.Default.Save();
        }

        private void TextBox2_TextChanged(object sender, TextChangedEventArgs e)
        {
         //   Properties.Settings.Default["Roll"] = PrefRoll.Text;
         //   Properties.Settings.Default.Save();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

                   // var diff = (DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(GetNISTDate())).TotalSeconds;
                    var utc_date = GetNISTDate();
                    Console.WriteLine(utc_date);
                    var diff = (DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(utc_date)).TotalSeconds;
                    PrefUTC.Text = diff.ToString();
                    Properties.Settings.Default["UTCDiff"] = PrefUTC.Text;
                    Properties.Settings.Default.Save();
        }


        //Get a NTP time from NIST
        //do not request a nist date more than once every 4 seconds, or the connection will be refused.
        //more servers at tf.nist.goc/tf-cgi/servers.cgi
        public static DateTime GetDummyDate()
        {
            return new DateTime(1000, 1, 1); //to check if we have an online date or not.
        }

        public static DateTime GetNISTDate()
        {
            Random ran = new Random(DateTime.Now.Millisecond);
            DateTime date = GetDummyDate();
            string serverResponse = string.Empty;

            // Represents the list of NIST servers
            string[] servers = new string[] {
        "nist1-ny.ustiming.org",
        "time-a.nist.gov",
        "nist1-chi.ustiming.org",
        "time.nist.gov",
        "ntp-nist.ldsbc.edu",
        "nist1-la.ustiming.org"                         
             };

            // Try each server in random order to avoid blocked requests due to too frequent request
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    // Open a StreamReader to a random time server
                    System.IO.StreamReader reader = new System.IO.StreamReader(new System.Net.Sockets.TcpClient(servers[ran.Next(0, servers.Length)], 13).GetStream());
                    serverResponse = reader.ReadToEnd();
                    reader.Close();

                    // Check to see that the signature is there
                    if (serverResponse.Length > 47 && serverResponse.Substring(38, 9).Equals("UTC(NIST)"))
                    {
                        // Parse the date
                        int jd = int.Parse(serverResponse.Substring(1, 5));
                        int yr = int.Parse(serverResponse.Substring(7, 2));
                        int mo = int.Parse(serverResponse.Substring(10, 2));
                        int dy = int.Parse(serverResponse.Substring(13, 2));
                        int hr = int.Parse(serverResponse.Substring(16, 2));
                        int mm = int.Parse(serverResponse.Substring(19, 2));
                        int sc = int.Parse(serverResponse.Substring(22, 2));

                        if (jd > 51544)
                            yr += 2000;
                        else
                            yr += 1999;

                        date = new DateTime(yr, mo, dy, hr, mm, sc);

                        // Exit the loop
                        break;
                    }
                }
                catch (Exception ex)
                {
                    /* Do Nothing...try the next server */
                }
            }
            return date;
        }

    }
}
