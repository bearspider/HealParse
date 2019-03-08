using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealParse
{
    public class Characters
    {
        public ObservableCollection<Character> CharacterCollection { get; set; }

        public Characters()
        {
            CharacterCollection = new ObservableCollection<Character>();
        }
        public int Count()
        {
            return CharacterCollection.Count;
        }
        public void Clear()
        {
            CharacterCollection.Clear();
        }
        public Boolean CharacterExists(String Name)
        {
            Boolean rval = true;
            try
            {
                CharacterCollection.Single<Character>(i => i.Name == Name);
            }
            catch(InvalidOperationException)
            {
                rval = false;
            }
            return rval;
        }
        public Character Find(String Name)
        {
            Character rval = new Character();
            rval.Name = Name;
            if(CharacterExists(Name))
            {
                rval = CharacterCollection.Single<Character>(i => i.Name == Name);
            }
            return rval;
        }
        public void AddCharacter(String charname)
        {
            if(!CharacterExists(charname))
            {
                Character newcharacter = new Character();
                newcharacter.Name = charname;
                CharacterCollection.Add(newcharacter);
            }
        }
        public void AddSpell(String charname, String spellname, DateTime date)
        {
            Find(charname).AddSpell(spellname,date);             
        }
    }
}
