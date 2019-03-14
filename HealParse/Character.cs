using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HealParse
{
    public class Character
    {
        private DateTime datetofilter;
        private DateTime datefromfilter;
        public String Name { get; set; }
        public ObservableCollection<Spell> Spells { get; set; }
        public Int64 MaxSpellCount { get; set; }
        public Character()
        {
            Name = "";
            Spells = new ObservableCollection<Spell>();
            MaxSpellCount = 0;
        }
        public void AddSpell(String spellname, DateTime date)
        {
            Boolean spellcheck = true;
            for(int i = 0; i < Spells.Count; i++)
            {
                if(Spells[i].SpellName == spellname)
                {
                    Spells[i].Count++;
                    Spells[i].Time.Add(date);
                    spellcheck = false;
                    break;
                }
            }
            if(spellcheck)
            {
                Spell newspell = new Spell();
                newspell.SpellName = spellname;
                newspell.Time.Add(date);
                Spells.Add(newspell);
            }
        }
        public void CountSpells()
        {
            for (int i = 0; i < Spells.Count; i++)
            {
                MaxSpellCount += Spells[i].Count;
            }
        }
        public Int64 TotalSpellCount()
        {
            Int64 rval = 0;
            for (int i = 0; i < Spells.Count; i++)
            {
                rval += Spells[i].Count;
            }
            return rval;
        }
        public Boolean SpellsEmpty(DateTime datefrom, DateTime dateto)
        {
            datetofilter = dateto;
            datefromfilter = datefrom;
            Boolean rval = true;
            for (int i=0; i<Spells.Count; i++)
            {
                CollectionViewSource cvsSpellTime = new CollectionViewSource();
                cvsSpellTime.Source = Spells[i].Time;
                cvsSpellTime.Filter += DateFilter;
                cvsSpellTime.View.Refresh();
                if((cvsSpellTime.View).Cast<object>().Count() > 0)
                {
                    rval = false;
                }
            }
            return rval;
        }
        public void DateFilter(object item, FilterEventArgs e)
        {
            if (e.Item != null)
            {
                int beforedate = (e.Item as DateTime?).Value.CompareTo(datetofilter);
                int afterdate = (e.Item as DateTime?).Value.CompareTo(datefromfilter);
                if (beforedate < 0 && afterdate > 0)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }
    }
}
