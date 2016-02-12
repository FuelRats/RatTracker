using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.App
{
    public enum NATType
    {
        Unknown=0,
        Failed=1,
        Open=2,
        Blocked=3,
        SymmetricUDP=4,
        FullCone=5,
        Restricted=6,
        PortRestricted=7,
        Symmetric=8
    }
}
