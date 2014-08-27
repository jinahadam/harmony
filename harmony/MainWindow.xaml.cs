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


namespace harmony
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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



        System.Timers.Timer aTimer;
        System.Timers.Timer bTimer;
        public const int calibration_tries = 5;
        public const int calibration_interval = 10000;

        public DateTime[] dates = new DateTime[calibration_tries];

        public int tries = 0;
        int count_down = 10;



        public MainWindow()
        {
            InitializeComponent();


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            calibrate();
            Console.WriteLine("Being BIN");
            // var temp1 = MissionPlanner.Log.BinaryLog.ReadLog("126.BIN");

            // Console.WriteLine("temp1 {0}", temp1.Count);
            // delete binary log file
            //File.Delete(logfile);

            //logfile = logfile + ".log";

            // write assci log
            //using (StreamWriter sw = new StreamWriter(logfile))
            //{
            // foreach (string line in temp1)
            // {
            //     Console.WriteLine(line);
            // }
            //  sw.Close();
            //}
        }


        private void calibrate()
        {
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
                    dates[tries] = DateTime.Today;

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
                //  Dispatcher.Invoke((Action)(() => boxLabel.Content = "Drop Images Here"));


            }
        }


        //drag and drop stuff
        private void ellipse_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Rectangle ellipse = sender as Rectangle;
            if (ellipse != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(ellipse,
                                     ellipse.Fill.ToString(),
                                     System.Windows.DragDropEffects.Copy);
            }
        }

        private void ellipse_Drop(object sender, System.Windows.DragEventArgs e)
        {
            foreach (String item in (String[])e.Data.GetData((System.Windows.DataFormats.FileDrop)))
            {
                Console.WriteLine(System.IO.Path.GetFullPath(item));
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("BIN FILE??");
          
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Binary Log|*.bin";

            ofd.ShowDialog();

            if (File.Exists(ofd.FileName))
            {

                List<string> lines = new List<string>();

                using (BinaryReader br = new BinaryReader(File.OpenRead(ofd.FileName)))
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


                                    Console.WriteLine(line);
                                    lines.Add(line);
                                }
                                catch { Console.WriteLine("Bad Binary log line {0}", data); }
                                break;
                        }
                    }
                }

            }

            

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

    } //end of class

}// end of namespace
