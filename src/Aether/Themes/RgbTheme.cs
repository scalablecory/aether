using System.Diagnostics;
using System.Reactive.Linq;
using Aether.Devices.Drivers;
using Aether.Devices.Sensors;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;

namespace Aether.Themes
{
    internal sealed class RgbTheme
    {
        private const double FrameTimeInSeconds = 1.0 / 30.0;
        private const double MaxAqiChangePerSecond = 1.0 / 20.0;

        private const double AlertAqiChangeThreshold = 0.2;
        private const double AlertAnimationLengthInSeconds = 2.0;
        private const int AlertPixelCount = 1;
        private const int AlertPixelStide = 1;
        private const double AlertPixelMovementPerSecond = 15.0;

        public static IDisposable Run(AddressableRgbDriver display, IObservable<Measurement> source)
        {
            var colorConverter = new ColorSpaceConverter();

            LinearRgb blue = colorConverter.ToLinearRgb(new Rgb(0.0f, 0.0f, 1.0f));
            LinearRgb green = colorConverter.ToLinearRgb(new Rgb(0.0f, 1.0f, 0.0f));
            LinearRgb yellow = colorConverter.ToLinearRgb(new Rgb(1.0f, 1.0f, 0.0f));
            LinearRgb orange = colorConverter.ToLinearRgb(new Rgb(1.0f, 0.5f, 0.0f));
            LinearRgb red = colorConverter.ToLinearRgb(new Rgb(1.0f, 0.0f, 0.0f));

            var pixels = new LedPixel[display.LedCount];
            double pixelCount = pixels.Length;

            double prevAqi = 0.0f;
            double alertCounter = 0.0f;

            int prevFirstAlertPixelIdx = -1;
            double firstAlertPixelIdxAcc = 0.0;

            LedPixel prevColor = default;

            long prevFrameTime = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
            double frameTimeToSecondsScale = 1.0 / Stopwatch.Frequency;

            // Normalize AQI to between 0..1.
            IObservable<double> aqiObservable = source
                .Where(x => x.Measure == Measure.AirQualityIndex)
                .Select(x => Math.Min(x.AirQualityIndex.Value * (1.0 / 200.0), 1.0));

            return Observable.Interval(TimeSpan.FromSeconds(FrameTimeInSeconds))
                .WithLatestFrom(aqiObservable, static (_, aqi) => aqi)
                .Subscribe(nextAqi =>
                {
                    // Find how much time has passed since the last frame.

                    long curFrameTime = Stopwatch.GetTimestamp();
                    double stepTimeInSeconds = (curFrameTime - prevFrameTime) * frameTimeToSecondsScale;
                    prevFrameTime = curFrameTime;

                    // Find the next AQI to render.
                    // To smooth rendering out a bit, only change up a certain amount per second.

                    double maxAqiChange = MaxAqiChangePerSecond * stepTimeInSeconds;
                    double actualAqiChange = nextAqi - prevAqi;

                    nextAqi = prevAqi + Math.Clamp(actualAqiChange, -maxAqiChange, maxAqiChange);
                    prevAqi = nextAqi;

                    // Adjust the alert counter.

                    alertCounter = actualAqiChange >= AlertAqiChangeThreshold
                        // When the AQI changes very quickly by more than a certain threshold,
                        // reset the alert counter.
                        ? AlertAnimationLengthInSeconds
                        // Otherwise, move the alert counter toward 0.
                        : Math.Max(alertCounter - stepTimeInSeconds, 0.0);

                    // Find the first pixel to be lit up by the alert.
                    // This animates the pixels over time.

                    int firstAlertPixelIdx;

                    if (alertCounter > 0.0)
                    {
                        firstAlertPixelIdxAcc = (firstAlertPixelIdxAcc + stepTimeInSeconds * AlertPixelMovementPerSecond) % pixelCount;
                        firstAlertPixelIdx = (int)firstAlertPixelIdxAcc;
                    }
                    else
                    {
                        firstAlertPixelIdx = -1;
                    }

                    // Translate the AQI to being an offset between two colors.

                    (LinearRgb from, LinearRgb to, double offset) = nextAqi switch
                    {
                        < 0.25 => (blue, green, 0.0),
                        < 0.50 => (green, yellow, 1.0),
                        < 0.75 => (yellow, orange, 2.0),
                        _ => (orange, red, 3.0)
                    };

                    // Find the color at that offset.

                    double lerp = Math.Clamp(nextAqi * 4.0 - offset, 0.0, 1.0);

                    float r = (float)(from.R + (to.R - from.R) * lerp);
                    float g = (float)(from.G + (to.G - from.G) * lerp);
                    float b = (float)(from.B + (to.B - from.B) * lerp);

                    Rgb rgb = colorConverter.ToRgb(new LinearRgb(r, g, b));

                    // And convert the sRGB color to a pixel color.

                    byte ri = (byte)Convert.ToInt32(Math.Clamp(rgb.R * 255.0f, 0.0f, 255.0f));
                    byte gi = (byte)Convert.ToInt32(Math.Clamp(rgb.G * 255.0f, 0.0f, 255.0f));
                    byte bi = (byte)Convert.ToInt32(Math.Clamp(rgb.B * 255.0f, 0.0f, 255.0f));

                    var color = new LedPixel(Brightness: 16, ri, gi, bi);

                    // Update LEDs, if there's a change.

                    if (color != prevColor || firstAlertPixelIdx != prevFirstAlertPixelIdx)
                    {
                        prevColor = color;
                        prevFirstAlertPixelIdx = firstAlertPixelIdx;

                        // The entire string gets a base pixel color.

                        pixels.AsSpan().Fill(color);

                        // And then if an alert is happening, some are updated to be brighter.

                        if (firstAlertPixelIdx != -1)
                        {
                            LedPixel brightColor = color with { Brightness = 255 };
                            int idx = firstAlertPixelIdx;

                            for (int i = 0; i < AlertPixelCount; ++i)
                            {
                                while (idx >= pixels.Length)
                                {
                                    idx -= pixels.Length;
                                }

                                pixels[idx] = brightColor;
                                idx += AlertPixelStide;
                            }
                        }

                        // And finally display the pixel buffer.

                        display.SetLeds(pixels);
                    }
                });
        }
    }
}
