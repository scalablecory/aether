using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Aether.Devices.Sensors;

namespace Aether.Reactive
{
    internal static class ObservableAirQualityIndex
    {
        /// <summary>
        /// Calculates air quality indexes based on a stream of measurements.
        /// </summary>
        public static IObservable<Measurement> GetAirQualityIndex(IObservable<Measurement> measurements) =>
            measurements
            .Scan(new State(), static (state, measurement) =>
            {
                switch (measurement.Measure)
                {
                    case Measure.CO2:
                        double co2 = measurement.Co2.PartsPerMillion;
                        state.Co2 = co2 switch
                        {
                            <= 1100.0 => ConstMapValue(co2, 400.0, 1100.0, 0.0, 50.0),
                            <= 1500.0 => ConstMapValue(co2, 1000.0, 1500.0, 50.0, 100.0),
                            <= 2500.0 => ConstMapValue(co2, 1500.0, 2500.0, 100.0, 150.0),
                            <= 5000.0 => ConstMapValue(co2, 2500.0, 5000.0, 150.0, 200.0),
                            _ => ConstMapValue(co2, 5000.0, 15000.0, 200.0, 500.0),
                        };
                        break;
                    case Measure.VOC:
                        // TODO: this is specific to the SGP40; find a way to generalize this?
                        state.Voc = ConstMapValue(measurement.Voc.Value, 100.0, 400.0, 0.0, 500.0);
                        break;
                    case Measure.PM2_5:
                        // Based on EPA's AQI breakpoints.
                        double pm2_5 = measurement.MassConcentration.MicrogramsPerCubicMeter;
                        state.PM2_5 = pm2_5 switch
                        {
                            <= 12.0 => pm2_5 * (1.0 / 12.0 * 50.0),
                            <= 35.4 => ConstMapValue(pm2_5, 12.0, 35.4, 50.0, 100.0),
                            <= 55.4 => ConstMapValue(pm2_5, 35.4, 55.4, 100.0, 150.0),
                            <= 150.4 => ConstMapValue(pm2_5, 55.4, 150.4, 150.0, 200.0),
                            <= 240.4 => ConstMapValue(pm2_5, 150.4, 240.4, 200.0, 300.0),
                            <= 350.4 => ConstMapValue(pm2_5, 240.4, 350.4, 300.0, 400.0),
                            _ => ConstMapValue(pm2_5, 350.4, 500.4, 400.0, 500.0)
                        };
                        break;
                    case Measure.PM10_0:
                        // Based on EPA's AQI breakpoints.
                        double pm10 = measurement.MassConcentration.MicrogramsPerCubicMeter;
                        state.PM10_0 = pm10 switch
                        {
                            <= 54.0 => pm10 * (1.0 / 54.0 * 50.0),
                            <= 154.0 => ConstMapValue(pm10, 54.0, 154.0, 50.0, 100.0),
                            <= 254.0 => ConstMapValue(pm10, 154.0, 254.0, 100.0, 150.0),
                            <= 354.0 => ConstMapValue(pm10, 254.0, 354.0, 150.0, 200.0),
                            <= 424.0 => ConstMapValue(pm10, 354.0, 424.0, 200.0, 300.0),
                            <= 504.0 => ConstMapValue(pm10, 424.0, 504.0, 300.0, 400.0),
                            _ => ConstMapValue(pm10, 504.0, 604.0, 400.0, 500.0)
                        };
                        break;
                    default:
                        state.Report = false;
                        return state;
                }

                state.Report = true;
                return state;
            })
            .Where(static state => state.Report)
            .Select(static state =>
            {
                double aqi = Math.Clamp(Math.Max(Math.Max(state.Co2, state.Voc), Math.Max(state.PM2_5, state.PM10_0)), 0.0, 500.0);
                return Measurement.FromAirQualityIndex(new CustomUnits.AirQualityIndex(aqi));
            })
            .Replay(bufferSize: 1)
            .RefCount();

        private sealed class State
        {
            public double Co2;
            public double Voc;
            public double PM2_5;
            public double PM10_0;
            public bool Report;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ConstMapValue(double x, double min, double max, double newMin, double newMax)
        {
            // With inlining, this all folds down into just two operations (x * y + z).

            double range = max - min;
            double newRange = newMax - newMin;
            double scale = newRange / range;
            double offset = newMin - min * scale;

            return x * scale + offset;
        }
    }
}
