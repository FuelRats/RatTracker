using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.App
{
    public enum NetworkError
    {
        LocalNetworkError=1,
        ServerNetworkError=2,
        NetworkCodeAbort=3,
        AddressBlocked=4,
        UpstreamError=5,
        LocalNetworkImmediateDisconnect=6
    }
}
