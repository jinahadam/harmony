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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["Calibration"] = PrefCal.Text;
            Properties.Settings.Default["Roll"] = PrefRoll.Text;
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
    }
}
