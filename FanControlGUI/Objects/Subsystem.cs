using System.Collections.Generic;

namespace FanControlGUI.Objects
{
    public class Subsystem
    {
        public int SubsystemId { get; set; }
        public List<Fan> Fans { get; set; } = new List<Fan>();
    }
}
