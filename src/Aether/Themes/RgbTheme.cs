using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Aether.Devices.Drivers;
using Aether.Devices.Sensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Aether.Themes
{
    internal sealed class RgbTheme
    {
        public static IDisposable Run(DisplayDriver display, IObservable<Measurement> source)
        {
            float co2 = 0.0f;
            float voc = 0.0f;
            float pm2_5 = 0.0f;
            float pm10 = 0.0f;

            var colorConverter = new ColorSpaceConverter();

            LinearRgb blue = colorConverter.ToLinearRgb(new Rgb(0.0f, 0.0f, 1.0f));
            LinearRgb green = colorConverter.ToLinearRgb(new Rgb(0.0f, 1.0f, 0.0f));
            LinearRgb yellow = colorConverter.ToLinearRgb(new Rgb(1.0f, 1.0f, 0.0f));
            LinearRgb orange = colorConverter.ToLinearRgb(new Rgb(1.0f, 0.5f, 0.0f));
            LinearRgb red = colorConverter.ToLinearRgb(new Rgb(1.0f, 0.0f, 0.0f));

            Image image = display.CreateImage(display.Width, display.Height);

            return source.Subscribe(measurement =>
            {
                switch (measurement.Measure)
                {
                    case Measure.CO2:
                        const float co2Min = 600.0f;
                        const float co2Max = 3000.0f;

                        co2 = ((float)measurement.Co2.PartsPerMillion - co2Min) * (1.0f / (co2Max - co2Min));
                        break;
                    case Measure.VOC:
                        const float vocMin = 100.0f;
                        const float vocMax = 300.0f;

                        voc = ((float)measurement.Voc.Value - vocMin) * (1.0f / (vocMax - vocMin));
                        break;
                    case Measure.PM2_5:
                        pm2_5 = (float)measurement.MassConcentration.MicrogramsPerCubicMeter;
                        pm2_5 = pm2_5 switch
                        {
                            <= 12.1f => ConstFindLerpDistance(1.0f, 4f, 0.0f, 12.1f, pm2_5),
                            <= 35.5f => ConstFindLerpDistance(2.0f, 4.0f, 12.1f, 35.5f, pm2_5),
                            <= 55.5f => ConstFindLerpDistance(3.0f, 4.0f, 35.5f, 55.5f, pm2_5),
                            _ => ConstFindLerpDistance(4.0f, 4.0f, 55.5f, 150.5f, pm2_5)
                        };
                        break;
                    case Measure.PM10_0:
                        pm10 = (float)measurement.MassConcentration.MicrogramsPerCubicMeter;
                        pm10 = pm10 switch
                        {
                            <= 55.0f => ConstFindLerpDistance(1.0f, 4f, 0.0f, 55.0f, pm10),
                            <= 155.0f => ConstFindLerpDistance(2.0f, 4.0f, 55.0f, 155.0f, pm10),
                            <= 255.0f => ConstFindLerpDistance(3.0f, 4.0f, 155.0f, 255.0f, pm10),
                            _ => ConstFindLerpDistance(4.0f, 4.0f, 255.0f, 355.0f, pm10)
                        };
                        break;
                    default:
                        return;
                }

                float combinedIndex = (float)Math.Max(Math.Max(co2, voc), Math.Max(pm2_5, pm10));

                (LinearRgb from, LinearRgb to, float offset) = combinedIndex switch
                {
                    < 0.25f => (blue, green, 0.0f),
                    < 0.50f => (green, yellow, 1.0f),
                    < 0.75f => (yellow, orange, 2.0f),
                    _ => (orange, red, 3.0f)
                };

                float lerp = Math.Clamp(combinedIndex * 4.0f - offset, 0.0f, 1.0f);

                float r = from.R + (to.R - from.R) * lerp;
                float g = from.G + (to.G - from.G) * lerp;
                float b = from.B + (to.B - from.B) * lerp;

                Rgb rgb = colorConverter.ToRgb(new LinearRgb(r, g, b));

                byte ri = (byte)Convert.ToInt32(Math.Clamp(rgb.R * 255.0f, 0.0f, 255.0f));
                byte gi = (byte)Convert.ToInt32(Math.Clamp(rgb.G * 255.0f, 0.0f, 255.0f));
                byte bi = (byte)Convert.ToInt32(Math.Clamp(rgb.B * 255.0f, 0.0f, 255.0f));

                Color color = Color.FromRgba(ri, gi, bi, a: 8);
                image.Mutate(ctx => ctx.Clear(color));

                display.DisplayImage(image);
            });
        }

        /// <summary>
        /// Takes a value <paramref name="min"/> &lt;= <paramref name="x"/> &lt;= <paramref name="max"/>, finds its normalized distance  between the two (e.g. 5 &lt;= 10 &lt;= 15 will result in 0.5.
        /// Then, it adjusts the value to be part of a multi-step e.g. 1.0 with step 1/2 will result in 0.5.
        /// </summary>
        /// <param name="stepNo">The current step, starting at 1.</param>
        /// <param name="stepCount">The total step count.</param>
        /// <param name="min">The minimum value of this step.</param>
        /// <param name="max">The maximum value of this step.</param>
        /// <param name="x">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float ConstFindLerpDistance(float stepNo, float stepCount, float min, float max, float x)
        {
            // This all folds down into two operations (x * y + z).
            return x * (1.0f / (max - min) / stepCount) + (((stepNo - 1.0f) / stepCount) - min / (max - min) / stepCount);
        }
    }
}
