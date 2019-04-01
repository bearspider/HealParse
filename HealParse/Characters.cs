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
        public void AddCharacter(String charname)
        {
            Boolean charsearch = true;
            for (int i = 0; i < CharacterCollection.Count; i++)
            {
                if (CharacterCollection[i].Name == charname)
                {
                    charsearch = false;
                    break;
                }
            }
            if(charsearch)
            {
                Character newcharacter = new Character();
                newcharacter.Name = charname;
                CharacterCollection.Add(newcharacter);
            }
        }
        public void AddSpell(String charname, String spellname, DateTime date)
        {
            for(int i=0; i<CharacterCollection.Count; i++)
            {
                if(CharacterCollection[i].Name == charname)
                {
                    CharacterCollection[i].AddSpell(spellname, date);
                    break;
                }
            }           
        }
    }
}
