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
        KeyValuePair<uint, ClassId> it;
        public PlagueTarget(uint id, ClassId value) { it = new KeyValuePair<uint, ClassId>(id, value); }
        public uint Id{ get { return it.Key; } }
        public ClassId Value { get { return it.Value; } }
    }
}
