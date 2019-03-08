using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.ComponentModel;
using Xceed.Wpf.Toolkit;

namespace HealParse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class GlobalVariables
    {
        public static string defaultPath = @"C:\EQAudioTriggers";
        public static string defaultDB = $"{defaultPath}\\eqtriggers.db";
        public static Regex eqRegex = new Regex(@"\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\]\s(?<stringToMatch>.*)");
        public static Regex spellRegex = new Regex(@"(?<character>.*)\sbegins\sto\scast\sa\sspell\.\s\<(?<spellname>.*)\>");
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
        private ObservableCollection<Spell> spells = new ObservableCollection<Spell>();
        private DateTime? datefromfilter;
        private DateTime? datetofilter;

        public Characters characters;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            image_monitorindicator.DataContext = monitorstatus;
            characters = new Characters();
            this.DataContext = characters;
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
                statusbarFilename.Content = fileDialog.FileName;
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
                        statusbarFilename.Content = fileDialog.FileName;
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
            statusbarStatus.Content = totallinecount;
        }
        private void LoadLogFile(string filepath)
        {
            if(characters.Count() > 0)
            {
                characters.Clear();
            }
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
                        foreach(String captureline in lines)
                        {
                            Match eqlinematch = GlobalVariables.eqRegex.Match(captureline);
                            Match spellmatch = GlobalVariables.spellRegex.Match(eqlinematch.Groups["stringToMatch"].Value);
                            if(spellmatch.Success)
                            {
                                DateTime newtime;
                                DateTime.TryParseExact(eqlinematch.Groups["eqtime"].Value, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture,DateTimeStyles.None,out newtime);
                                characters.AddCharacter(spellmatch.Groups["character"].Value);
                                characters.AddSpell(spellmatch.Groups["character"].Value, spellmatch.Groups["spellname"].Value, newtime);
                            }                            
                        }                        
                    }
                }
            }
            statusbarFilename.Content = filepath;
            listviewCharacters.ItemsSource = characters.CharacterCollection;
        }
        private void MonitorLogFile(string filepath)
        {
            monitorstatus = true;
        }
        private void ToggleMonitor()
        {
            image_monitorindicator.DataContext = monitorstatus;
        }

        private void ListviewCharacters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Character)listviewCharacters.SelectedItem != null)
            {
                //this.DataContext = (Character)listviewCharacters.SelectedItem;
                //spellview = CollectionViewSource.GetDefaultView(((Character)listviewCharacters.SelectedItem).Spells);                
                //datagridSpells.ItemsSource = spellview;
            }
        }

        private void TimepickerFrom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            datefromfilter = timepickerFrom.Value;
            if (timepickerFrom.Value != null && timepickerTo.Value != null)
            {                
                if(timepickerFrom.Value > timepickerTo.Value)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Invalid Date Range", "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Error);
                    timepickerFrom.Value = null;
                }
            }
            
        }

        private void TimepickerTo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            datetofilter = timepickerTo.Value;
            if (timepickerTo.Value != null && timepickerFrom.Value != null)
            {                
                if(timepickerFrom.Value < timepickerFrom.Value)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Invalid Date Range", "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Error);
                    timepickerTo.Value = null;
                }
                else
                {

                }
            }
            
        }
    }
}
