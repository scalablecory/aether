using System.Diagnostics;
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
        private const double FrameTimeInSeconds = 1.0 / 30.0;
        private const double MaxAiqChangePerSecond = 1.0 / 10.0;
        private const float BurstAnimationLengthInSeconds = 1.0f;
        private const int BurstPixelCount = 1;
        private const int BurstPixelStide = 1;
        private const double BurstPixelMovementPerSecond = 15.0;

        public static IDisposable Run(AddressableRgbDriver display, IObservable<Measurement> source)
        {
            var colorConverter = new ColorSpaceConverter();

            LinearRgb blue = colorConverter.ToLinearRgb(new Rgb(0.0f, 0.0f, 1.0f));
            LinearRgb green = colorConverter.ToLinearRgb(new Rgb(0.0f, 1.0f, 0.0f));
            LinearRgb yellow = colorConverter.ToLinearRgb(new Rgb(1.0f, 1.0f, 0.0f));
            LinearRgb orange = colorConverter.ToLinearRgb(new Rgb(1.0f, 0.5f, 0.0f));
            LinearRgb red = colorConverter.ToLinearRgb(new Rgb(1.0f, 0.0f, 0.0f));

            LedPixel[] pixels = new LedPixel[display.LedCount];
            double pixelCount = pixels.Length;

            float prevAiq = 0.0f;
            float fastCounter = 0.0f;

            int prevBurstIdx = -1;
            double burstIdxAcc = 0.0;

            LedPixel prevColor = default;

            long prevFrameTime = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
            double frameTimeToSecondsScale = 1.0 / Stopwatch.Frequency;

            var aiqObservable = source.Scan(
                    (co2: 0.0f, voc: 0.0f, pm2_5: 0.0f, pm10: 0.0f, aiq: 0.0f),
                    static (acc, measurement) =>
                    {
                        switch (measurement.Measure)
                        {
                            case Measure.CO2:
                                const float co2Min = 600.0f;
                                const float co2Max = 3000.0f;

                                acc.co2 = ((float)measurement.Co2.PartsPerMillion - co2Min) * (1.0f / (co2Max - co2Min));
                                break;
                            case Measure.VOC:
                                const float vocMin = 100.0f;
                                const float vocMax = 300.0f;

                                acc.voc = ((float)measurement.Voc.Value - vocMin) * (1.0f / (vocMax - vocMin));
                                break;
                            case Measure.PM2_5:
                                float pm2_5 = (float)measurement.MassConcentration.MicrogramsPerCubicMeter;
                                acc.pm2_5 = pm2_5 switch
                                {
                                    <= 12.1f => ConstFindLerpDistance(1.0f, 4f, 0.0f, 12.1f, pm2_5),
                                    <= 35.5f => ConstFindLerpDistance(2.0f, 4.0f, 12.1f, 35.5f, pm2_5),
                                    <= 55.5f => ConstFindLerpDistance(3.0f, 4.0f, 35.5f, 55.5f, pm2_5),
                                    _ => ConstFindLerpDistance(4.0f, 4.0f, 55.5f, 150.5f, pm2_5)
                                };
                                break;
                            case Measure.PM10_0:
                                float pm10 = (float)measurement.MassConcentration.MicrogramsPerCubicMeter;
                                acc.pm10 = pm10 switch
                                {
                                    <= 55.0f => ConstFindLerpDistance(1.0f, 4f, 0.0f, 55.0f, pm10),
                                    <= 155.0f => ConstFindLerpDistance(2.0f, 4.0f, 55.0f, 155.0f, pm10),
                                    <= 255.0f => ConstFindLerpDistance(3.0f, 4.0f, 155.0f, 255.0f, pm10),
                                    _ => ConstFindLerpDistance(4.0f, 4.0f, 255.0f, 355.0f, pm10)
                                };
                                break;
                            default:
                                return acc;
                        }

                        acc.aiq = Math.Clamp(Math.Max(Math.Max(acc.co2, acc.voc), Math.Max(acc.pm2_5, acc.pm10)), 0.0f, 1.0f);
                        return acc;
                    })
                .Select(static x => x.aiq);

            var timer = Observable.Interval(TimeSpan.FromSeconds(FrameTimeInSeconds));

            return Observable.CombineLatest(timer, aiqObservable, (_, aiq) => aiq)
                .Subscribe(nextAiq =>
                {
                    long curFrameTime = Stopwatch.GetTimestamp();
                    double stepTimeInSeconds = (curFrameTime - prevFrameTime) * frameTimeToSecondsScale;
                    prevFrameTime = curFrameTime;

                    float maxAiqChange = (float)(MaxAiqChangePerSecond * stepTimeInSeconds);
                    float aiqChange = nextAiq - prevAiq;

                    if (Math.Abs(aiqChange) > maxAiqChange)
                    {
                        float targetAiq = nextAiq;
                        nextAiq = prevAiq + (aiqChange > 0.0f ? maxAiqChange : -maxAiqChange);

                        if (Math.Abs(targetAiq - nextAiq) >= 0.1)
                        {
                            fastCounter = BurstAnimationLengthInSeconds;
                        }
                        else
                        {
                            fastCounter = Math.Max(fastCounter - (float)stepTimeInSeconds, 0.0f);
                        }
                    }
                    else
                    {
                        fastCounter = Math.Max(fastCounter - (float)stepTimeInSeconds, 0.0f);
                    }

                    prevAiq = nextAiq;

                    (LinearRgb from, LinearRgb to, float offset) = nextAiq switch
                    {
                        < 0.25f => (blue, green, 0.0f),
                        < 0.50f => (green, yellow, 1.0f),
                        < 0.75f => (yellow, orange, 2.0f),
                        _ => (orange, red, 3.0f)
                    };

                    float lerp = Math.Clamp(nextAiq * 4.0f - offset, 0.0f, 1.0f);

                    float r = from.R + (to.R - from.R) * lerp;
                    float g = from.G + (to.G - from.G) * lerp;
                    float b = from.B + (to.B - from.B) * lerp;

                    Rgb rgb = colorConverter.ToRgb(new LinearRgb(r, g, b));

                    byte ri = (byte)Convert.ToInt32(Math.Clamp(rgb.R * 255.0f, 0.0f, 255.0f));
                    byte gi = (byte)Convert.ToInt32(Math.Clamp(rgb.G * 255.0f, 0.0f, 255.0f));
                    byte bi = (byte)Convert.ToInt32(Math.Clamp(rgb.B * 255.0f, 0.0f, 255.0f));

                    var color = new LedPixel(Brightness: 16, ri, gi, bi);
                    int burstIdx;

                    if (fastCounter > 0.0f)
                    {
                        burstIdxAcc += stepTimeInSeconds * BurstPixelMovementPerSecond;
                        while (burstIdxAcc >= pixelCount)
                        {
                            burstIdxAcc -= pixelCount;
                        }
                        burstIdx = (int)burstIdxAcc;
                    }
                    else
                    {
                        burstIdx = -1;
                    }

                    if (color != prevColor || burstIdx != prevBurstIdx)
                    {
                        prevColor = color;
                        prevBurstIdx = burstIdx;

                        pixels.AsSpan().Fill(color);

                        if (burstIdx != -1)
                        {
                            LedPixel brightColor = color with { Brightness = 255 };
                            int idx = burstIdx;

                            for (int i = 0; i < BurstPixelCount; ++i)
                            {
                                while (idx >= pixels.Length)
                                {
                                    idx -= pixels.Length;
                                }

                                pixels[idx] = brightColor;
                                idx += BurstPixelStide;
                            }
                        }

                        display.SetLeds(pixels);
                    }
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
