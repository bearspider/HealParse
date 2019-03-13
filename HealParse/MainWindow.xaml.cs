﻿using Microsoft.Win32;
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
        public static Regex eqRegex = new Regex(@"\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\]\s(?<stringToMatch>.*)");
        public static Regex spellRegex = new Regex(@"(\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\])\s((?<character>\w+)\sbegin\s(casting|singing)\s(?<spellname>.*)\.)|(\[(?<eqtime>\w+\s\w+\s+\d+\s\d+:\d+:\d+\s\d+)\])\s(?<character>\w+)\s(begins\sto\s(cast|sing)\s.*\<(?<spellname>.*)\>)");
        public static Regex logRegex = new Regex(@"eqlog_(?<character>.*)_.*\.txt");
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
        private readonly SynchronizationContext synccontext;
        private object _characterLock = new object();
        private ObservableCollection<Spell> selectedspells = new ObservableCollection<Spell>();
        private DateTime? datefromfilter;
        private DateTime? datetofilter;
        public Characters characters;
        private String currentlogfile;
        private String yourname;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            image_monitorindicator.DataContext = monitorstatus;
            characters = new Characters();
            this.DataContext = characters;            
            BindingOperations.EnableCollectionSynchronization(characters.CharacterCollection, _characterLock);
            synccontext = SynchronizationContext.Current;
        }
        private bool UserFilter(object item)
        {
            var charobject = (Character)item;
            if(Regex.IsMatch(charobject.Name,@"(a|an)\s.*" ,RegexOptions.IgnoreCase))
            {
                return false;
            }
            //Show all values if both filters are empty
            if (String.IsNullOrEmpty(textboxCharFilter.Text) && String.IsNullOrEmpty(textboxSpellFilter.Text))
            {
                return true;
            }
            //Filter by Character
            if(!String.IsNullOrEmpty(textboxCharFilter.Text) && String.IsNullOrEmpty(textboxSpellFilter.Text))
            {
                return Regex.IsMatch(charobject.Name, Regex.Escape(textboxCharFilter.Text), RegexOptions.IgnoreCase);
            }
            //Filter by Spell
            if(String.IsNullOrEmpty(textboxCharFilter.Text) && !String.IsNullOrEmpty(textboxSpellFilter.Text))
            {
                if(ContainsSpell(charobject))
                {
                    return true;
                }
            }
            //Filter by Character AND Spell
            if(!String.IsNullOrEmpty(textboxCharFilter.Text) && !String.IsNullOrEmpty(textboxSpellFilter.Text))
            {
                if(Regex.IsMatch(charobject.Name, Regex.Escape(textboxCharFilter.Text), RegexOptions.IgnoreCase) && ContainsSpell(charobject))
                {
                    return true;
                }
            }
            return false;
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
                for(int i = 0; i < ((Spell)item).Time.Count; i++)
                {
                    int beforedate = ((Spell)item).Time[i].CompareTo(datetofilter);
                    int afterdate = ((Spell)item).Time[i].CompareTo(datefromfilter);
                    if(beforedate < 0 && afterdate > 0)
                    {
                        rval = true;
                    }
                    else
                    {
                        rval = false;
                    }
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
                    return true;
                }
            }
            return rval;
        }
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private async void ButtonLoadLog_Click(object sender, RoutedEventArgs e)
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
                currentlogfile = fileDialog.FileName;
                Match namematch = GlobalVariables.logRegex.Match(fileDialog.FileName);
                yourname = namematch.Groups["character"].Value;
                if (characters.Count() > 0)
                {
                    characters.Clear();
                    datagridSpells.DataContext = null;
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
                    if(!string.IsNullOrEmpty(result))
                    {
                        statusbarTime.Content = $" || Parse Completed in {result} Seconds";
                        statusbarTime.Visibility = Visibility.Visible;
                        statusbarStatus.Content = lines.Length;
                        listviewCharacters.SelectedIndex = 0;
                        CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Filter = UserFilter;
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
                await Task.Run(() =>                {
                    MonitorLogFile(logmonitorfile);
                });
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
                datagridSpells.ItemsSource = ((Character)listviewCharacters.SelectedItem).Spells;
                pieGraph.ItemsSource = ((Character)listviewCharacters.SelectedItem).Spells;
                ICollectionView cvSpells = CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource);
                if(cvSpells != null && cvSpells.CanSort == true)
                {
                    cvSpells.SortDescriptions.Clear();
                    cvSpells.SortDescriptions.Add(new SortDescription("Count", ListSortDirection.Descending));
                }
                CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Filter = SpellFilter;
            }
        }
        private void TimepickerFrom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
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
                }
                if (datagridSpells != null)
                {
                    CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Refresh();
                }
            }
        }
        private void TimepickerTo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (timepickerTo != null && timepickerTo != null)
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
                }
                if (datagridSpells != null)
                {
                   CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Refresh();
                }
            }
        }
        private void TextboxSpellFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(datagridSpells.ItemsSource).Refresh();
        }
        private void TextboxCharFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(listviewCharacters.ItemsSource).Refresh();
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
                        timepickerTo.Value = null;
                        timepickerFrom.Value = null;
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
            paneGraph.Content = plotview;
        }
    }
}
