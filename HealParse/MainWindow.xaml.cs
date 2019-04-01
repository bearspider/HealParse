using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OxyPlot;
using OxyPlot.Series;
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
using System.Diagnostics;
using System.Collections;

namespace HealParse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class GlobalVariables
    {
        public static string defaultPath = @"C:\EQAudioTriggers";
        public static string defaultDB = $"{defaultPath}\\eqtriggers.db";
        public static Regex eqRegex = new Regex(@"\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\]\s(?<stringToMatch>.*)", RegexOptions.Compiled);
        public static Regex spellRegex = new Regex(@"(\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\])\s((?<character>\w+)\sbegin\s(casting|singing)\s(?<spellname>.*)\.)|(\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\])\s(?<character>\w+)\s(begins\sto\s(cast|sing)\s.*\<(?<spellname>.*)\>)",RegexOptions.Compiled);
        public static Regex logRegex = new Regex(@"eqlog_(?<character>.*)_.*\.txt",RegexOptions.Compiled);
        public static string pathRegex = @"(?<logdir>.*\\)(?<logname>eqlog_.*\.txt)";
        public static Regex buff = new Regex(@"(\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\])\s(?<buff>.*?)\(.*\)", RegexOptions.Compiled);
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
        private String inspectbuffreset = "You sense these enchantments";
        private string logmonitorfile = null;
        private Boolean monitorstatus = false;
        private readonly SynchronizationContext synccontext;
        private object _characterLock = new object();
        private object _buffLock = new object();
        private ObservableCollection<Spell> selectedspells = new ObservableCollection<Spell>();
        private ObservableCollection<Buff> missingbuffs = new ObservableCollection<Buff>();
        private List<String> bufflist = new List<String>();
        private DateTime? datefromfilter;
        private DateTime? datetofilter;
        public Characters characters;
        private String currentlogfile;
        private String yourname;
        private int totallinecount = 0;
        private ObservableCollection<Buff> listofbuffs = new ObservableCollection<Buff>();
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            LoadBuffList();
            image_monitorindicator.DataContext = monitorstatus;
            statusbarStatus.DataContext = totallinecount;
            datagridBuffs.DataContext = missingbuffs;
            characters = new Characters();
            this.DataContext = characters;            
            BindingOperations.EnableCollectionSynchronization(characters.CharacterCollection, _characterLock);
            BindingOperations.EnableCollectionSynchronization(missingbuffs, _buffLock);
            timepickerFrom.Value = DateTime.Now.AddYears(-7);
            timepickerTo.Value = DateTime.Now;
            synccontext = SynchronizationContext.Current;
            LoadBuffSettings();
        }
        private void LoadBuffSettings()
        {
            listofbuffs.Clear();
            foreach (String buffproperty in SpellParse.Properties.Settings.Default.BuffList.Cast<string>())
            {
                Buff newbuff = new Buff
                {
                    Name = buffproperty
                };
                listofbuffs.Add(newbuff);
            }
            datagridBuffList.ItemsSource = listofbuffs;
        }
        private void LoadBuffList()
        {
            if(listofbuffs.Count > 0)
            {
                foreach (Buff buff in listofbuffs)
                {
                    missingbuffs.Add(buff);
                }
            }
        }
        private void ResetBuffMissing()
        {
            missingbuffs.Clear();
            LoadBuffList();
        }
        private bool UserFilter(object item)
        {
            Boolean Rval = false;
            var charobject = (Character)item;
            if(!Regex.IsMatch(charobject.Name,@"(a|an)\s.*" ,RegexOptions.IgnoreCase))
            {
                //Show all values if both filters are empty
                if (String.IsNullOrEmpty(textboxCharFilter.Text) && String.IsNullOrEmpty(textboxSpellFilter.Text))
                {
                    Rval = true;
                }
                //Filter by Character
                if (!String.IsNullOrEmpty(textboxCharFilter.Text) && String.IsNullOrEmpty(textboxSpellFilter.Text))
                {
                    Rval = Regex.IsMatch(charobject.Name, Regex.Escape(textboxCharFilter.Text), RegexOptions.IgnoreCase);
                }
                //Filter by Spell
                if (String.IsNullOrEmpty(textboxCharFilter.Text) && !String.IsNullOrEmpty(textboxSpellFilter.Text))
                {
                    if (ContainsSpell(charobject))
                    {
                        Rval = true;
                    }
                }
                //Filter by Character AND Spell
                if (!String.IsNullOrEmpty(textboxCharFilter.Text) && !String.IsNullOrEmpty(textboxSpellFilter.Text))
                {
                    if (Regex.IsMatch(charobject.Name, Regex.Escape(textboxCharFilter.Text), RegexOptions.IgnoreCase) && ContainsSpell(charobject))
                    {
                        Rval = true;
                    }
                }
                if (charobject.SpellsEmpty(datefromfilter.Value, datetofilter.Value))
                {
                    Rval = false;
                }
            }
            return Rval;
        }
        private bool SpellFilter(object item)
        {
            //default to not return spell
            Boolean rval = false;
            //spell filter exists
            if (String.IsNullOrEmpty(textboxSpellFilter.Text))
            {
                rval = true;
            }
            else
            {
                rval = Regex.IsMatch(((Spell)item).SpellName, Regex.Escape(textboxSpellFilter.Text), RegexOptions.IgnoreCase);
            }
            //if there is no spell filter or we matched the filter and there's a valid date range, check dates
            if(datefromfilter != null && datetofilter != null && rval)
            {
                CollectionViewSource cvsSpellTime = new CollectionViewSource();
                cvsSpellTime.Source = ((Spell)item).Time;
                cvsSpellTime.Filter += DateFilter;
                cvsSpellTime.View.Refresh();
                ((Spell)item).Count = (cvsSpellTime.View).Cast<object>().Count();
                if (((Spell)item).Count > 0)
                {
                    rval = true;
                }
                else
                {
                    rval = false;
                }
            }

            return rval;
        }
        public void DateFilter(object item, FilterEventArgs e)
        {
            if(e.Item != null && !monitorstatus)
            {
                int beforedate = (e.Item as DateTime?).Value.CompareTo(datetofilter);
                int afterdate = (e.Item as DateTime?).Value.CompareTo(datefromfilter);
                if(beforedate < 0 && afterdate > 0)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }
        private bool DateFilter(object item)
        {
            Boolean rval = false;
            for (int i = 0; i < ((Spell)item).Time.Count; i++)
            {
                int beforedate = ((Spell)item).Time[i].CompareTo(datetofilter);
                int afterdate = ((Spell)item).Time[i].CompareTo(datefromfilter);
                if (beforedate < 0 && afterdate > 0)
                {
                    rval = true;
                }
                else
                {
                    rval = false;
                }
            }
            return rval;
        }
        private bool ContainsSpell(Character charobject)
        {
            Boolean rval = false;
            for(int i = 0; i < charobject.Spells.Count; i++)
            {
                if(Regex.IsMatch(charobject.Spells[i].SpellName,Regex.Escape(textboxSpellFilter.Text),RegexOptions.IgnoreCase))
                {
                    if(DateFilter(charobject.Spells[i]))
                    {
                        Console.WriteLine("Valid Date on Spell");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Invalid Date on Spell");
                    }
                }
            }
            return rval;
        }
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            SaveBuffs();
            this.Close();
        }
        private async void ButtonLoadLog_Click(object sender, RoutedEventArgs e)
        {
            totallinecount = 0;
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
                currentlogfile = fileDialog.FileName;
                Match namematch = GlobalVariables.logRegex.Match(fileDialog.FileName);
                yourname = namematch.Groups["character"].Value;
                if (characters.Count() > 0)
                {
                    characters.Clear();
                    BindingOperations.EnableCollectionSynchronization(characters.CharacterCollection, _characterLock);
                    CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Filter = null;
                    listviewCharacters.SelectedItem = null;
                    statusbarTime.Visibility = Visibility.Hidden;
                }
                String capturedLines = null;
                using (FileStream filestream = File.Open(currentlogfile, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(filestream))
                    {
                        capturedLines = streamReader.ReadToEnd();
                    }
                }
                if (capturedLines.Length > 0)
                {
                    String[] delimiter = new string[] { "\r\n" };
                    String[] lines = capturedLines.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);                 
                    string result = await Task.Run(() => LoadLogFile(lines));
                    Console.WriteLine("Task completed.");
                    if(!string.IsNullOrEmpty(result))
                    {
                        statusbarTime.Content = $" || Parse Completed in {result} Seconds";
                        statusbarTime.Visibility = Visibility.Visible;
                        statusbarStatus.Content = lines.Length;                        
                        CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Filter = UserFilter;
                        listviewCharacters.SelectedIndex = 0;
                    }
                }
            }
        }
        private async void ButtonMonitorLog_Click(object sender, RoutedEventArgs e)
        {
            if (monitorstatus)
            {
                //stop monitoring
                monitorstatus = false;
                ToggleMonitor();
            }
            else
            {
                characters = new Characters();
                this.DataContext = characters;
                statusbarTime.Visibility = Visibility.Hidden;
                if(logmonitorfile == null)
                {
                    OpenFileDialog fileDialog = new OpenFileDialog();
                    fileDialog.Filter = "Everquest Log Files|eqlog*.txt";
                    if (fileDialog.ShowDialog() == true)
                    {
                        logmonitorfile = fileDialog.FileName;
                        statusbarFilename.DataContext = logmonitorfile;
                        statusbarFilename.Content = fileDialog.FileName;
                        Match namematch = GlobalVariables.logRegex.Match(fileDialog.FileName);
                        yourname = namematch.Groups["character"].Value;
                    }
                }
                await Task.Run(() => { MonitorLogFile(logmonitorfile); });
                ToggleMonitor();
            }            
        }
        private String LoadLogFile(string[] capturedLines)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (String captureline in capturedLines)
            {
                Match spellmatch = GlobalVariables.spellRegex.Match(captureline);
                if (spellmatch.Success)
                {
                    DateTime newtime;
                    DateTime.TryParseExact(spellmatch.Groups["eqtime"].Value, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out newtime);
                    String tempname = spellmatch.Groups["character"].Value;
                    if(tempname == "You")
                    {
                        tempname = yourname;
                    }
                    lock (_characterLock)
                    {
                        characters.AddCharacter(tempname);
                        characters.AddSpell(tempname, spellmatch.Groups["spellname"].Value, newtime);
                    }
                }
            }
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}.{1:00}", ts.Seconds, ts.Milliseconds / 10);
            return elapsedTime;
        }
        private async void MonitorLogFile(string filepath)
        {
            monitorstatus = true;
            await Task.Run(async () =>
            {
                ResetLineCount();
                using (FileStream filestream = File.Open(filepath, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    filestream.Seek(0, SeekOrigin.End);
                    using (StreamReader streamReader = new StreamReader(filestream))
                    {
                        while (monitorstatus)
                        {
                            String captureline = streamReader.ReadLine();                            
                            if(captureline != null)
                            {
                                UpdateLineCount(1);
                                Match spellmatch = GlobalVariables.spellRegex.Match(captureline);
                                if (spellmatch.Success)
                                {
                                    DateTime newtime;
                                    DateTime.TryParseExact(spellmatch.Groups["eqtime"].Value, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out newtime);
                                    String tempname = spellmatch.Groups["character"].Value;
                                    if (tempname == "You")
                                    {
                                        tempname = yourname;
                                    }
                                    synccontext.Post(new SendOrPostCallback(o =>
                                    {
                                        characters.AddCharacter((String)o);
                                        characters.AddSpell((String)o, spellmatch.Groups["spellname"].Value, newtime);
                                        UpdateList();
                                    }), tempname);
                                }
                                if(captureline.Contains(inspectbuffreset))
                                {
                                    ResetBuffMissing();
                                }
                                Match buffmatch = GlobalVariables.buff.Match(captureline);
                                if(buffmatch.Success)
                                {
                                    foreach (Buff buff in listofbuffs)
                                    {
                                        if (buffmatch.Groups["buff"].Value.Contains(buff.Name))
                                        {
                                            Boolean remove = false;
                                            Buff removebuff = new Buff();
                                            foreach(Buff checkbuff in missingbuffs)
                                            {
                                                if(checkbuff.Name == buff.Name)
                                                {
                                                    remove = true;
                                                    removebuff = checkbuff;
                                                }
                                            }
                                            if(remove)
                                            {
                                                missingbuffs.Remove(removebuff);
                                            }
                                        }
                                    }
                                }

                            }
                            Thread.Sleep(1);
                            if(monitorstatus == false)
                            {
                                break;
                            }
                        }
                    }
                }
            });
        }
        private void ResetLineCount()
        {
            int value = 0;
            synccontext.Post(new SendOrPostCallback(o =>
            {
                totallinecount = (int)o;
                statusbarStatus.Content = totallinecount;
            }), value);
        }
        private void UpdateLineCount(int value)
        {
            synccontext.Post(new SendOrPostCallback(o =>
            {
                totallinecount += (int)o;
                statusbarStatus.Content = totallinecount;
            }), value);
        }
        private void ToggleMonitor()
        {
            image_monitorindicator.DataContext = monitorstatus;
        }
        private void ListviewCharacters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateList();
        }
        private void UpdateList()
        {
            if ((Character)listviewCharacters.SelectedItem != null)
            {
                if (((Character)listviewCharacters.SelectedItem).MaxSpellCount == 0)
                {
                    ((Character)listviewCharacters.SelectedItem).CountSpells();
                }
                ICollectionView cvSpells = CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource);
                if (cvSpells != null && cvSpells.CanSort == true)
                {
                    cvSpells.SortDescriptions.Clear();
                    cvSpells.SortDescriptions.Add(new SortDescription("Count", ListSortDirection.Descending));
                }
                if (datagridSpells.ItemsSource != null)
                {
                    CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Filter = SpellFilter;
                }
            }
        }
        private void TimepickerFrom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Console.WriteLine("Timepicker From Changed");
            if (timepickerFrom != null && timepickerTo != null)
            {
                datefromfilter = timepickerFrom.Value;
                if (timepickerFrom.Value > DateTime.Now)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Can't Choose Future Date", "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Error);
                    timepickerFrom.Value = null;
                }
                if (timepickerFrom.Value != null && timepickerTo.Value != null)
                {
                    if (timepickerFrom.Value > timepickerTo.Value)
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show("Invalid Date Range", "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Error);
                        timepickerFrom.Value = null;
                    }
                    if (datagridSpells.ItemsSource != null)
                    {
                        CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Refresh();
                        CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
                        if (listviewCharacters.SelectedItem == null)
                        {
                            listviewCharacters.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine("TimepickerFrom Datagridspell null");
                        if (listviewCharacters.ItemsSource != null)
                        {
                            CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
                            if (CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).IsEmpty)
                            {
                                //Xceed.Wpf.Toolkit.MessageBox.Show("Search Results Empty", "No Results", MessageBoxButton.OK, MessageBoxImage.Error);
                                Console.WriteLine("TimepickerFrom Characters Empty");
                            }
                            else
                            {
                                Console.WriteLine("TimepickerFrom Characters NotEmpty");
                            }
                        }
                    }
                }            
            }
        }
        private void TimepickerTo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Console.WriteLine("Timepicker To Changed");
            if (timepickerFrom != null && timepickerTo != null)
            {
                datetofilter = timepickerTo.Value;
                if (timepickerTo.Value > DateTime.Now)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("Can't Choose Future Date", "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Error);
                    timepickerTo.Value = null;
                }
                if (timepickerTo.Value != null && timepickerFrom.Value != null)
                {
                    if (timepickerFrom.Value < timepickerFrom.Value)
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show("Invalid Date Range", "Invalid Date", MessageBoxButton.OK, MessageBoxImage.Error);
                        timepickerTo.Value = null;
                    }
                    if (datagridSpells.ItemsSource != null)
                    {
                        CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Refresh();
                        CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
                        if (listviewCharacters.SelectedItem == null)
                        {
                            listviewCharacters.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine("TimepickerTo Datagridspell null");
                        if (listviewCharacters.ItemsSource != null)
                        {
                            CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
                            if (CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).IsEmpty)
                            {
                                //Xceed.Wpf.Toolkit.MessageBox.Show("Search Results Empty", "No Results", MessageBoxButton.OK, MessageBoxImage.Error);
                                Console.WriteLine("TimepickerTo Characters Empty");
                            }
                            else
                            {
                                Console.WriteLine("TimepickerTo Characters NotEmpty");
                            }
                        }
                    }
                }
            }
        }
        private void TextboxSpellFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine(textboxSpellFilter.Text);
            CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
            if(CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).IsEmpty)
            {
                Console.WriteLine("listviewcharacters is empty");
                Xceed.Wpf.Toolkit.MessageBox.Show("No Matching Spells", "Spell Filter", MessageBoxButton.OK, MessageBoxImage.Error);
                textboxSpellFilter.Text = "";
                listviewCharacters.SelectedIndex = 0;
            }
            else
            {
                if(listviewCharacters.SelectedItem == null)
                {
                    Console.WriteLine("listviewcharacters is not empty/selected item is null");
                    listviewCharacters.SelectedIndex = 0;
                }
                if(datagridSpells.ItemsSource != null)
                {
                    Console.WriteLine("listviewcharacters is not empty/datagridspells itemsource not null");
                    CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Refresh();
                }                     
            }
        }
        private void TextboxCharFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
            if(!CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).IsEmpty && textboxCharFilter.Text == "")
            {
                listviewCharacters.SelectedIndex = 0;
            }
        }
        private void ComboboxQuickDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (characters != null && characters.Count() > 0)
            {
                string selection = comboboxQuickDate.SelectedValue.ToString();
                switch (selection)
                {
                    case "Last Hour":
                        timepickerTo.Value = DateTime.Now;
                        timepickerFrom.Value = DateTime.Now.AddHours(-1);
                        break;
                    case "Last 6 Hours":
                        timepickerTo.Value = DateTime.Now;
                        timepickerFrom.Value = DateTime.Now.AddHours(-6);
                        break;
                    case "Last 24 Hours":
                        timepickerTo.Value = DateTime.Now;
                        timepickerFrom.Value = DateTime.Now.AddHours(-24);
                        break;
                    case "Last 7 Days":
                        timepickerTo.Value = DateTime.Now;
                        timepickerFrom.Value = DateTime.Now.AddDays(-7);
                        break;
                    case "All":
                        timepickerTo.Value = DateTime.Now;
                        timepickerFrom.Value = DateTime.Now.AddYears(-7);
                        break;
                }
            }
        }
        private void PaneGraph_IsSelectedChanged(object sender, EventArgs e)
        {
            OxyPlot.Wpf.PlotView plotview = new OxyPlot.Wpf.PlotView();
            PlotModel newplot = new PlotModel { Title = "Spell Distribution" };            
            PieSeries newpie = new PieSeries
            {
                InnerDiameter = 0.2,
                ExplodedDistance = 0,
                Stroke = OxyPlot.OxyColor.Parse("255,255,255,255"),
                StrokeThickness = 1,
                StartAngle = 0,
                AngleSpan = 360,
                LabelField = "SpellName",
                ValueField = "Count",
            };
            newplot.Series.Add(newpie);
            plotview.Model = newplot;
            //paneGraph.Content = plotview;
        }
        private void SaveBuffs()
        {
            SpellParse.Properties.Settings.Default.BuffList.Clear();
            foreach (Buff savebuff in listofbuffs)
            {
                SpellParse.Properties.Settings.Default.BuffList.Add(savebuff.Name);
            }
            SpellParse.Properties.Settings.Default.Save();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            SaveBuffs();
        }
    }
}
