using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealParse
{
    public class Spell
    {
        public String SpellName { get; set; }
        public long Count { get; set; }
        public ObservableCollection<DateTime> Time { get; set; }
        public Spell()
        {
            SpellName = "";
            Count = 1;
            Time = new ObservableCollection<DateTime>();
        }
    }
}
