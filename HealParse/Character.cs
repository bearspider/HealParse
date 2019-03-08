using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealParse
{
    public class Character
    {
        public String Name { get; set; }
        public ObservableCollection<Spell> Spells { get; set; }
        public Character()
        {
            Name = "";
            Spells = new ObservableCollection<Spell>();
        }
        public void AddSpell(String spellname, DateTime date)
        {         
            try
            {
                Spell oldspell = Spells.Single<Spell>(i => i.SpellName == spellname);
                oldspell.Count++;
                oldspell.Time.Add(date);
            }
            catch(InvalidOperationException)
            {
                Spell newspell = new Spell();
                newspell.SpellName = spellname;
                newspell.Time.Add(date);
                Spells.Add(newspell);
            }
        }
    }
}
