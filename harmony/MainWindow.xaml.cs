using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace harmony
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Timers.Timer aTimer;
        System.Timers.Timer bTimer;
        public const int calibration_tries = 5;
        public const int calibration_interval = 10000;

        public int tries = 0;
        int count_down = 10;
       
        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            calibrate();
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
                Dispatcher.Invoke((Action)(() => calibrationLabel.Content = "Prepare Camera for Calibration"));
            }
            else
            {
                if (count_down == 0)
                {
                    Dispatcher.Invoke((Action)(() => calibrationLabel.Content = "Snap!!"));
                }
                else
                {
                    Dispatcher.Invoke((Action)(() => calibrationLabel.Content = String.Format("Prepare to snap picture in {0}. Taking Picture {1}/{2}", count_down, tries, calibration_tries)));
                }
            }

          
            Console.WriteLine("1 second elapsed");
        }

        // Specify what you want to happen when the Elapsed event is raised.
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            count_down = 10;
            tries++;
            if (tries > calibration_tries) {
                aTimer.Enabled = false;
                bTimer.Enabled = false;
                Dispatcher.Invoke((Action)(() => calibrationLabel.Content = "Connect the Camera and drag the calibration images below"));

            }
            Console.WriteLine("10 second timer");
        }

     
    }
}
