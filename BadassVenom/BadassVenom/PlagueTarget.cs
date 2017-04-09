using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;

namespace BadassVenom
{
    class PlagueTarget
    {
        KeyValuePair<uint, ClassID> it;
        public PlagueTarget(uint id, ClassID value) { it = new KeyValuePair<uint, ClassID>(id, value); }
        public uint Id{ get { return it.Key; } }
        public ClassID Value { get { return it.Value; } }
    }
}
