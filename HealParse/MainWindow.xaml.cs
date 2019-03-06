using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HealParse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class GlobalVariables
    {
        public static string defaultPath = @"C:\EQAudioTriggers";
        public static string defaultDB = $"{defaultPath}\\eqtriggers.db";
        public static string eqRegex = @"\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\](?<stringToMatch>.*)";
        public static string pathRegex = @"(?<logdir>.*\\)(?<logname>eqlog_.*\.txt)";
    }
    #region Converters
    public class MonitorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            String rString = "";
            if ((Boolean)value)
            {
                rString = "Images/Hopstarter-Soft-Scraps-Button-Blank-Green.ico";
            }
            else
            {
                rString = "Images/Hopstarter-Soft-Scraps-Button-Blank-Red.ico";
            }
            return rString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
    public partial class MainWindow : Window
    {
        #region Properties
        private object _fightlock = new object();

        private string logmonitorfile = null;
        private Boolean monitorstatus = false;

        private long totallinecount;
        private readonly SynchronizationContext synccontext;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            image_monitorindicator.DataContext = monitorstatus;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ButtonLoadLog_Click(object sender, RoutedEventArgs e)
        {
            if(monitorstatus)
            {
                //stop monitoring
                monitorstatus = false;
                ToggleMonitor();
            }
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Everquest Log Files|eqlog*.txt";
            //string filePattern = @"eqlog_(.*)_(.*)\.txt";
            if (fileDialog.ShowDialog() == true)
            {
                logmonitorfile = fileDialog.FileName;
                statusbarFilename.DataContext = logmonitorfile;
                LoadLogFile(fileDialog.FileName);
            }
            
        }
        private void ButtonMonitorLog_Click(object sender, RoutedEventArgs e)
        {
            if (monitorstatus)
            {
                //stop monitoring
                monitorstatus = false;
                ToggleMonitor();
            }
            else
            {
                if(logmonitorfile == null)
                {
                    OpenFileDialog fileDialog = new OpenFileDialog();
                    fileDialog.Filter = "Everquest Log Files|eqlog*.txt";
                    //string filePattern = @"eqlog_(.*)_(.*)\.txt";
                    if (fileDialog.ShowDialog() == true)
                    {
                        logmonitorfile = fileDialog.FileName;
                        statusbarFilename.DataContext = logmonitorfile;
                    }
                }
                MonitorLogFile(logmonitorfile);
                ToggleMonitor();
            }
            
        }
        private void UpdateLineCount(int value)
        {
            /*synccontext.Post(new SendOrPostCallback(o =>
            {
                totallinecount += (int)o;
                statusbarStatus.DataContext = totallinecount;
            }), value);*/
            totallinecount += value;
            statusbarStatus.DataContext = totallinecount;
        }
        private void LoadLogFile(string filepath)
        {
            using (FileStream filestream = File.Open(filepath, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //filestream.Seek(0, SeekOrigin.End);
                using (StreamReader streamReader = new StreamReader(filestream))
                {
                    String capturedLine = streamReader.ReadToEnd();
                    if(capturedLine.Length > 0)
                    {
                        String[] delimiter = new string[] { "\r\n" };
                        String[] lines = capturedLine.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                        UpdateLineCount(lines.Length);
                    }
                }
            }
        }
        private void MonitorLogFile(string filepath)
        {
            monitorstatus = true;
        }
        private void ToggleMonitor()
        {
            image_monitorindicator.DataContext = monitorstatus;
        }
    }
}
