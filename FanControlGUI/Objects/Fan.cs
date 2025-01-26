namespace FanControlGUI.Objects
{
    public class Fan
    {
        public int FanId { get; set; }
        public double Speed { get; set; } = 0; // Default speed
        public int MaxRPM { get; set; }

        public void UpdateSpeed(double percentage)
        {
            Speed = percentage * MaxRPM;
        }
    }
}
