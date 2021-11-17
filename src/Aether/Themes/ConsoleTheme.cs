using Aether.Devices.Sensors;

namespace Aether.Themes
{
    internal static class ConsoleTheme
    {
        public static IDisposable Run(IObservable<Measurement> source) =>
            source.Subscribe(static (Measurement measurement) =>
            {
                Console.WriteLine($"[{DateTime.Now:t}] {measurement.Measure}: {measurement}");
            });
    }
}
