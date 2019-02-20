using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SOCKETHUB
{
    static public class StaticObj
    {
        static public List<LNBRecePCBID> StrList_LNBRecePCBID = new List<LNBRecePCBID>();
    }


    public class LNBRecePCBID
    {
        public string PCBID = null;
        public bool Msg551 = true;
    }
}
