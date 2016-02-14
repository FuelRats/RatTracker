using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatTracker_WPF.Models.App
{
    public class PlayerInfo
    {
        public string CurrentSystem { get; set; }
        public bool OnDuty { get; set; }
        public float JumpRange { get; set; }
        public bool SuperCruise { get; set; }
        public PlayerInfo myPlayerInfo { get; set; }
    }
}
