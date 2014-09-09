using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.IO;
using uint8_t = System.Byte;
using System.Runtime.InteropServices;
using ExifLib;
using System.Threading;

 


namespace harmony
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public struct GPS_LINE
    {
        public DateTime datetime;
        public string type;
        public float lat;
        public float lon;
        public float alt;

        public float roll;
        public float pitch;
        public float yaw;
    }

    public partial class MainWindow : Window
    {
        
        System.Timers.Timer aTimer;
        System.Timers.Timer bTimer;
        public const int calibration_tries = 5;
        public const int calibration_interval = 10000;

        public DateTime[] dates = new DateTime[calibration_tries];
        public DateTime[] image_dates = new DateTime[calibration_tries];

        public int tries = 0;
        int count_down = 10;

        List<GPS_LINE> gps_lines = new List<GPS_LINE>();


        internal static MainWindow main;
        public MainWindow()
        {
            InitializeComponent();


           // Properties.Settings.Default["Calibration"] = "-3705";
 //           Properties.Settings.Default.Save();



            var cal = Properties.Settings.Default["Calibration"];
            statusBar.Content = String.Format("Calibration Difference : {0}", cal);
            main = this;

                
        }

       
        internal string Status
        {
            get { return statusBar.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { statusBar.Content = String.Format("Calibration Difference : {0}", value); })); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            calibrate();           
        }


        private void calibrate()
        {

            if (aTimer != null)
            {
                aTimer.Enabled = false;
                bTimer.Enabled = false;
            }
               
            tries = 0;
            count_down = 10;

            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = calibration_interval;
            aTimer.Enabled = true;

            bTimer = new System.Timers.Timer();
            bTimer.Elapsed += new ElapsedEventHandler(onSecondElapased);
            bTimer.Interval = 1000;
            bTimer.Enabled = true;


        }


      
        private void onSecondElapased(object sender, ElapsedEventArgs e)
        {
            count_down--;

            if (tries == 0)
            {

                Dispatcher.Invoke((Action)(() => displayLabel.Content = "Prepare Camera for Calibration"));
            }
            else
            {
                if (count_down == 0)
                {
                    Dispatcher.Invoke((Action)(() => displayLabel.Content = "Snap!!"));
                    dates[tries-1] = DateTime.Now;

                }

                else
                {
                    Dispatcher.Invoke((Action)(() => displayLabel.Content = String.Format("Prepare to snap picture in {0}. Taking Picture {1}/{2}", count_down, tries, calibration_tries)));
                }
            }

        }

        // Specify what you want to happen when the Elapsed event is raised.
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            count_down = 10;
            tries++;
            if (tries > calibration_tries)
            {
                aTimer.Enabled = false;
                bTimer.Enabled = false;
                Dispatcher.Invoke((Action)(() => displayLabel.Content = "Connect the Camera and drag the calibration images below"));

            }
        }


        private String GetImageExifDatetime(String filename)
        {
            //  Console.WriteLine(filename);
            ExifReader reader = new ExifReader(filename);

            DateTime datePictureTaken;
            if (reader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized,
                                                out datePictureTaken))
            {
                return datePictureTaken.ToString();

            }
            else
            {
                return @"Error";
            }




        }


        private void ellipse_Drop(object sender, System.Windows.DragEventArgs e)
        {

            String type = "";
            foreach (String item in (String[])e.Data.GetData((System.Windows.DataFormats.FileDrop)))
            {
                type = System.IO.Path.GetExtension(item);
            }

            if (type.Length > 0)
            {
                int i = -1;
                foreach (String item in (String[])e.Data.GetData((System.Windows.DataFormats.FileDrop)))
                {
                    i++;
                    var file = System.IO.Path.GetFullPath(item);
                    image_dates[i] = DateTime.Parse(GetImageExifDatetime(item));
                }

                Array.Sort(image_dates);
                List<Double> times_for_medium = new List<Double>();


                try
                {
                    int j = -1;
                    foreach (var item in image_dates) {
                        j++;

                        Console.WriteLine(Math.Round((item - dates[j]).TotalSeconds, 0));
                        times_for_medium.Add(Math.Round((item - dates[j]).TotalSeconds, 0));

                    }


                    try {
                        //using linq to calculate mode
                        double mode = times_for_medium.GroupBy(k => k)  
                                 .OrderByDescending(g => g.Count()) 
                                 .Select(g => g.Key) 
                                 .FirstOrDefault();  

                        Properties.Settings.Default["Calibration"] = mode.ToString();
                        Properties.Settings.Default.Save();
                        Dispatcher.Invoke((Action)(() => displayLabel.Content = String.Format("Calibration Difference : {0}", mode)));
                        var cal = Properties.Settings.Default["Calibration"];
                        Dispatcher.Invoke((Action)(() =>  statusBar.Content = String.Format("Calibration Difference : {0}", cal)));

          
                    } catch {

                       
          
                    }


                }
                catch
                {
                    Dispatcher.Invoke((Action)(() => statusBar.Content = "Calibration Failed. :( "));
                }
            }
            else
            {
                Console.WriteLine("Matching");


                ThreadPool.QueueUserWorkItem((o) =>
                {
                    
                    Dispatcher.Invoke((Action)(() => displayLabel.Content = "Processing Images"));

                    List<String> exifData = new List<String>();
                    string[] directoryName = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    //where to handle multiple folders

                    List<String> allfiles = new List<String>();

                    //handles multiple folders
                    if (directoryName.Count() == 1)
                    {
                        allfiles = Directory.GetFiles(directoryName[0]).ToList();
                    } else if (directoryName.Count() > 1) {
                       
                        foreach (var dir in directoryName) {
                            foreach (var f in Directory.GetFiles(dir)) {
                                  allfiles.Add(f);
                            }
                        }

                    }

                    List<String> t = new List<String>();
                    double cal = Convert.ToDouble(Properties.Settings.Default["Calibration"]);
                    double roll = Convert.ToDouble(Properties.Settings.Default["Roll"]);
                    
                    //TODO: move this to the directory read loop.?
                    foreach (String file in allfiles.ToArray())
                    {
                        if ((System.IO.Path.GetExtension(file)).ToUpper() == ".JPG")
                        {
                            t.Add(file);
                        }
                    }

                    Console.WriteLine("image count {0}", t.Count());

                    string[] files = t.ToArray();
                    List<string> matched = new List<string>();
                    int match_count = 0;
                    int discarded_count = 0;

                    /*
                    List<String> debug_gps = new List<String>();
                    foreach (var g in gps_lines)
                    {
                        debug_gps.Add(g.datetime.ToString("hh.mm.ss.ffffff"));    
                    }

                    string textFileDir = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar;
                    System.IO.File.WriteAllLines(textFileDir + System.IO.Path.DirectorySeparatorChar + "output.txt", debug_gps.ToArray());
                    System.Diagnostics.Process.Start(textFileDir + System.IO.Path.DirectorySeparatorChar + "output.txt");
                    */

                    /*
                    List<String> debug_exif = new List<String>();
                    foreach (var fname in files)
                    {
                        debug_exif.Add(DateTime.Parse(GetImageExifDatetime(fname)).ToString("hh.mm.ss.ffffff"));
                    }

                    string textFileDir = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar;
                    System.IO.File.WriteAllLines(textFileDir + System.IO.Path.DirectorySeparatorChar + "output.txt", debug_exif.ToArray());
                    System.Diagnostics.Process.Start(textFileDir + System.IO.Path.DirectorySeparatorChar + "output.txt");
                    */


                    int processing_count = 0;
                    foreach (var fname in files)
                    {
                        processing_count++;
                        Dispatcher.Invoke((Action)(() => displayLabel.Content = String.Format("Processing {0} of {1} ", processing_count, files.Count())));

                        DateTime imageDt = DateTime.Parse(GetImageExifDatetime(fname)).AddSeconds(cal);
                        bool match = false;
                        foreach (var gps in gps_lines)
                        {
                            if (imageDt.ToString("hh.mm.ss.ffffff") == gps.datetime.ToString("hh.mm.ss.ffffff"))
                            {
                                //Console.WriteLine("match");
                                match = true;
                                match_count++;
                                if (gps.roll > (roll*-1) && gps.roll < roll)
                                {
                                    matched.Add(String.Format("{0},{1},{2},{3},{4},{5},{6}", GetFileName(fname), gps.lat, gps.lon, gps.alt, gps.roll, gps.pitch, gps.yaw));
                                }
                                else
                                {
                                    discarded_count++;
                                }
                                break;
                            }
                        }

                        if (!match)
                        {
                            foreach (var gps in gps_lines)
                            {
                                if (imageDt.ToString("hh.mm.ss") == gps.datetime.ToString("hh.mm.ss"))
                                {
                                    //Console.WriteLine("match");
                                    match = true;
                                    match_count++;
                                    if (gps.roll > (roll * -1) && gps.roll < roll)
                                    {
                                        matched.Add(String.Format("{0},{1},{2},{3},{4},{5},{6}", GetFileName(fname), gps.lat, gps.lon, gps.alt, gps.roll, gps.pitch, gps.yaw));
                                    }
                                    else
                                    {
                                        discarded_count++;
                                    }
                                    
                                    break;
                                }
                            }

                        }

                        if (!match)
                        {
                          //  Console.WriteLine("{0} no match", imageDt.ToString("hh.mm.ss.ffffff"));
                            
                        }
                        //    Console.WriteLine("no match found: {0}", imageDt.ToString("hh.mm.ss.ffffff"));



                        //write to a file and open it
                      
        

                    } //end loop

                    
                    string textFileDir = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar;
                    System.IO.File.WriteAllLines(textFileDir + System.IO.Path.DirectorySeparatorChar + "output.txt", matched.ToArray());
                    System.Diagnostics.Process.Start(textFileDir + System.IO.Path.DirectorySeparatorChar + "output.txt");
                    Dispatcher.Invoke((Action)(() => displayLabel.Content = String.Format("Processing Done. {0}/{1} images matched. {2} discarded", match_count, files.Count(), discarded_count)));
                    

                }); //end thread

                    

            }
        }

        private String GetFileName(String file)
        {
            return System.IO.Path.GetFileName(file);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
          
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "Binary Log|*.bin";
            ofd.ShowDialog();


            //handles mutliple .BIN Files
            foreach (var bin_file in ofd.FileNames) {
                Dispatcher.Invoke((Action)(() => displayLabel.Content = "Processing .BIN file(s)"));

                if (File.Exists(bin_file))
                {

                    BinParser bp = new BinParser(bin_file);
                    List<GPS_LINE> lines = bp.Lines();
                    foreach (GPS_LINE line in combineGPSandATT(lines)) {
                        gps_lines.Add(line);
                    }
                    

                
                }
            }

            Dispatcher.Invoke((Action)(() => displayLabel.Content = String.Format("{0} GPS cordinates, Drag the image folder to the box below or add more .BIN files", gps_lines.Count())));



        }

        private List<GPS_LINE> combineGPSandATT(List<GPS_LINE> lines)
        {
            List<GPS_LINE> result = new List<GPS_LINE>();
            var temp = new GPS_LINE();
            foreach (var item in lines)
            {
                if (item.type == "GPS")
                {
                    temp.datetime = item.datetime;
                    temp.lat = item.lat;
                    temp.lon = item.lon;
                    temp.alt = item.alt;
                }

                if (item.type == "ATT")
                {
                    if (temp.lat != 0 && temp.lon != 0)
                    {
                        temp.roll = item.roll;
                        temp.pitch = item.pitch;
                        temp.yaw = item.yaw;
                        result.Add(temp);
                    }
                }
            }

            return result;

        }


        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Window1 win2 = new Window1();
            win2.Topmost = true;
            win2.Show();
           // this.Close();
        }


    } //end of class


    class BinParser
    {
        const byte HEAD_BYTE1 = 0xA3;    // Decimal 163  
        const byte HEAD_BYTE2 = 0x95;    // Decimal 149  

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct log_Format
        {
            public uint8_t type;
            public uint8_t length;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] format;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] labels;
        }

        static Dictionary<string, log_Format> logformat = new Dictionary<string, log_Format>();



        List<GPS_LINE> lines = new List<GPS_LINE>();

        public BinParser(String filename)
        {


            using (BinaryReader br = new BinaryReader(File.OpenRead(filename)))
            {
                int log_step = 0;

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    byte data = br.ReadByte();

                    switch (log_step)
                    {
                        case 0:
                            if (data == HEAD_BYTE1)
                            {
                                log_step++;
                            }
                            break;

                        case 1:
                            if (data == HEAD_BYTE2)
                            {
                                log_step++;
                            }
                            else
                            {
                                log_step = 0;
                            }
                            break;

                        case 2:
                            log_step = 0;
                            try
                            {
                                string line = logEntry(data, br);
                                var processed_line = processLine(line);
                                if (processed_line.type == "GPS" || processed_line.type == "ATT")
                                {
                                    lines.Add(processed_line);
                                }

                            }
                            catch { Console.WriteLine("Bad Binary log line {0}", data); }
                            break;
                    }
                }
            }

        }

        public List<GPS_LINE> Lines()
        {
            return lines;
        }

        private DateTime DateTimeFromGPSTimeandGPSWeek(String gpstime, String gpsweek)
        {
            DateTime gpsweekstart = DateTime.Parse("06 January 1980");
            int days = int.Parse(gpsweek) * 7;
            //converts to localtime from UTC
            DateTime convertedDate = gpsweekstart.AddDays(days).AddSeconds(double.Parse(gpstime) / 1000);
            return TimeZone.CurrentTimeZone.ToLocalTime(convertedDate);



        }




        static string logEntry(byte packettype, BinaryReader br)
        {

            switch (packettype)
            {

                case 0x80:  // FMT

                    log_Format logfmt = new log_Format();

                    object obj = logfmt;

                    int len = Marshal.SizeOf(obj);

                    byte[] bytearray = br.ReadBytes(len);

                    IntPtr i = Marshal.AllocHGlobal(len);

                    // create structure from ptr
                    obj = Marshal.PtrToStructure(i, obj.GetType());

                    // copy byte array to ptr
                    Marshal.Copy(bytearray, 0, i, len);

                    obj = Marshal.PtrToStructure(i, obj.GetType());

                    Marshal.FreeHGlobal(i);

                    logfmt = (log_Format)obj;

                    string lgname = ASCIIEncoding.ASCII.GetString(logfmt.name).Trim(new char[] { '\0' });
                    string lgformat = ASCIIEncoding.ASCII.GetString(logfmt.format).Trim(new char[] { '\0' });
                    string lglabels = ASCIIEncoding.ASCII.GetString(logfmt.labels).Trim(new char[] { '\0' });

                    logformat[lgname] = logfmt;

                    string line = String.Format("FMT, {0}, {1}, {2}, {3}, {4}\r\n", logfmt.type, logfmt.length, lgname, lgformat, lglabels);

                    return line;

                default:
                    string format = "";
                    string name = "";
                    int size = 0;

                    foreach (log_Format fmt in logformat.Values)
                    {
                        if (fmt.type == packettype)
                        {
                            name = ASCIIEncoding.ASCII.GetString(fmt.name).Trim(new char[] { '\0' });
                            format = ASCIIEncoding.ASCII.GetString(fmt.format).Trim(new char[] { '\0' });
                            size = fmt.length;
                            break;
                        }
                    }

                    // didnt find a match, return unknown packet type
                    if (size == 0)
                        return "UNKW, " + packettype;

                    return ProcessMessage(br.ReadBytes(size - 3), name, format); // size - 3 = message - messagetype - (header *2)
            }
        }


        static string ProcessMessage(byte[] message, string name, string format)
        {
            char[] form = format.ToCharArray();

            int offset = 0;

            StringBuilder line = new StringBuilder(name);

            foreach (char ch in form)
            {
                switch (ch)
                {
                    case 'b':
                        line.Append(", " + (sbyte)message[offset]);
                        offset++;
                        break;
                    case 'B':
                        line.Append(", " + message[offset]);
                        offset++;
                        break;
                    case 'h':
                        line.Append(", " + BitConverter.ToInt16(message, offset).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'H':
                        line.Append(", " + BitConverter.ToUInt16(message, offset).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'i':
                        line.Append(", " + BitConverter.ToInt32(message, offset).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'I':
                        line.Append(", " + BitConverter.ToUInt32(message, offset).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'f':
                        line.Append(", " + BitConverter.ToSingle(message, offset).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'c':
                        line.Append(", " + (BitConverter.ToInt16(message, offset) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'C':
                        line.Append(", " + (BitConverter.ToUInt16(message, offset) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                        offset += 2;
                        break;
                    case 'e':
                        line.Append(", " + (BitConverter.ToInt32(message, offset) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'E':
                        line.Append(", " + (BitConverter.ToUInt32(message, offset) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'L':
                        line.Append(", " + ((double)BitConverter.ToInt32(message, offset) / 10000000.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        offset += 4;
                        break;
                    case 'n':
                        line.Append(", " + ASCIIEncoding.ASCII.GetString(message, offset, 4).Trim(new char[] { '\0' }));
                        offset += 4;
                        break;
                    case 'N':
                        line.Append(", " + ASCIIEncoding.ASCII.GetString(message, offset, 16).Trim(new char[] { '\0' }));
                        offset += 16;
                        break;
                    case 'M':
                        break;
                    case 'Z':
                        line.Append(", " + ASCIIEncoding.ASCII.GetString(message, offset, 64).Trim(new char[] { '\0' }));
                        offset += 64;
                        break;
                    default:

                        break;
                }
            }

            line.Append("\r\n");
            return line.ToString();
        }


        private GPS_LINE processLine(String line)
        {

            GPS_LINE gps_line = new GPS_LINE();
            string[] items = line.Split(',', ':');
            if (items[0].Contains("GPS"))
            {
                var gps_time = items[2];
                var gps_week = items[3];
                var lat = items[6];
                var lon = items[7];
                var alt = items[9];

                gps_line.datetime = DateTimeFromGPSTimeandGPSWeek(gps_time, gps_week);
                gps_line.lat = float.Parse(lat);
                gps_line.lon = float.Parse(lon);
                gps_line.alt = float.Parse(alt);
                gps_line.type = "GPS";

            }
            else if (items[0].Contains("ATT"))
            {
                var roll = items[3];
                var pitch = items[5];
                var yaw = items[7];
                gps_line.type = "ATT";

                gps_line.roll = float.Parse(roll);
                gps_line.pitch = float.Parse(pitch);
                gps_line.yaw = float.Parse(yaw);

                // Console.WriteLine("{0} {1} {2}", roll, pitch, yaw);

                // foreach (var item in items)
                //      Console.Write(item);
            }

            return gps_line;

        }


    } //end BIN Parser Class


}// end of namespace
