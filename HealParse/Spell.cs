using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace HealParse
{
    public class Spell
    {
        public String SpellName { get; set; }
        public Int64 Count { get; set; }
        public ObservableCollection<DateTime> Time { get; set; }
        public Spell()
        {
            SpellName = "";
            Count = 1;
            Time = new ObservableCollection<DateTime>();
        }
        public Int64 CountSpells(DateTime from, DateTime to)
        {
            Console.WriteLine(SpellName);
            Int64 rval = 0;
            if(from != null && to != null)
            {
                for (int i = 0; i < Time.Count; i++)
                {
                    if (Time[i] > from && Time[i] < to)
                    {
                        rval += 1;
                    }
                }
            }
            return rval;
        }

        public double PercentCast(Int64 maxcasts)
        {
            double rval = 0;
            if(maxcasts < 0)
            {
                rval = Count / maxcasts;
            }
            Console.WriteLine(Count);
            return rval;
        }
    }
}
