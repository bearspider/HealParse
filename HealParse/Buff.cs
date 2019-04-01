using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealParse
{
    public class Buff
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public Buff()
        {
            Name = "";
            Duration = 0;
        }
    }
}
