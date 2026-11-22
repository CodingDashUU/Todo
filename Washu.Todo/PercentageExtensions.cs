namespace Washu.Todo;

public static class PercentageExtensions
{
    extension(Percentage percentage)
    {
        public string Hue
        {
            get
            {
                var clamped = Math.Clamp(percentage.Value, 0.0, 1.0);
                var hue = (int)Math.Round(clamped * 120);
                return $"hsl({hue}, 85%, 45%)";
            }
        }
    } 
}