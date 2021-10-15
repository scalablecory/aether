using Aether.Devices.Drivers;
using Aether.Devices.Sensors;
using Aether.Reactive;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System.Reactive.Linq;

namespace Aether.Themes
{
    internal sealed class MultiLineTheme
    {
        private const float MarginInInches = 0.05f;

        public static IDisposable CreateTheme(DisplayDriver driver, IEnumerable<Measure> lines, IObservable<Measurement> source)
        {
            Image image = driver.CreateImage();

            var fontCollection = new FontCollection();
            fontCollection.Install("fonts/Manrope-Regular.ttf");

            FontFamily fontFamily = fontCollection.Find("Manrope");
            Font measurementFont = fontFamily.CreateFont(22.75f);

            DrawingOptions bottomRightAlignDrawingOptions = new DrawingOptions
            {
                TextOptions =
                {
                    DpiX = driver.DpiX,
                    DpiY = driver.DpiY,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                }
            };

            // calculate measurement line height and measurement label width.

            float dpiX = driver.DpiX;
            float dpiY = driver.DpiY;
            float marginInPixelsX = MarginInInches * dpiX;
            float marginInPixelsY = MarginInInches * dpiY;

            Font labelFont = fontFamily.CreateFont(7.4f);

            var labelRendererOptions = new RendererOptions(labelFont)
            {
                DpiX = dpiX,
                DpiY = dpiY
            };

            var measurementRendererOptions = new RendererOptions(measurementFont)
            {
                DpiX = dpiX,
                DpiY = dpiY
            };

            float maxLineHeight = TextMeasurer.Measure("012345,678.9%", measurementRendererOptions).Height;
            float maxLabelWidth = 0.0f;

            var offsets = new Dictionary<Measure, int>();

            int i = 0;
            foreach (Measure measure in lines)
            {
                FontRectangle rect = TextMeasurer.Measure(GetMeasureLabel(measure), labelRendererOptions);

                maxLineHeight = MathF.Max(maxLineHeight, rect.Height);
                maxLabelWidth = MathF.Max(maxLabelWidth, rect.Width);

                offsets[measure] = ++i;
            }

            float labelOffsetX = image.Width - (marginInPixelsX + maxLabelWidth);

            // these two will be used to know our draw areas.

            float measureOffsetX = labelOffsetX - marginInPixelsX;
            float measureHeight = maxLineHeight + marginInPixelsY;

            // draw static labels.

            var bottomLeftAlignDrawingOptions = new DrawingOptions
            {
                TextOptions = { DpiX = dpiX, DpiY = dpiY, VerticalAlignment = VerticalAlignment.Bottom }
            };

            image.Mutate(ctx =>
            {
                ctx.Fill(Color.White);

                foreach ((Measure measure, int offset) in offsets)
                {
                    // This is how much to move the label up to match the baseline of measures.
                    // When ImageSharp supports getting the various measurements of strings, use that instead.
                    const float baselineOffsetY = 7.0f;

                    string text = GetMeasureLabel(measure);
                    float labelOffsetY = offset * measureHeight - baselineOffsetY;
                    var location = new PointF(labelOffsetX, labelOffsetY);

                    ctx.DrawText(bottomLeftAlignDrawingOptions, text, labelFont, Color.Black, location);
                }
            });

            driver.DisplayImage(image);

            // wire up against the source.
            // TODO: abstract and localize stringy bits.

            var seen = new HashSet<Measure>();

            return source.Gate().Subscribe(measurements =>
            {
                for (int i = measurements.Count - 1; i >= 0; --i)
                {
                    Measurement measurement = measurements[i];
                    
                    if (!seen.Add(measurement.Measure))
                    {
                        continue;
                    }

                    if (!offsets.TryGetValue(measurement.Measure, out int measureOffset))
                    {
                        return;
                    }

                    string text = measurement.Measure switch
                    {
                        Measure.Humidity => (measurement.RelativeHumidity.Value * (1.0 / 100.0)).ToString("P0"),
                        Measure.Temperature => measurement.Temperature.DegreesFahrenheit.ToString("N1"),
                        Measure.CO2 => measurement.Co2.PartsPerMillion.ToString("N0"),
                        Measure.BarometricPressure => measurement.BarometricPressure.Atmospheres.ToString("N2"),
                        _ => throw new Exception($"Unsupported measure '{measurement.Measure}'.")
                    };

                    float measureOffsetY = measureOffset * measureHeight;
                    var location = new PointF(measureOffsetX, measureOffsetY);

                    image.Mutate(ctx =>
                    {
                        ctx.Fill(Color.White, new RectangleF(0.0f, measureOffsetY - measureHeight, measureOffsetX, measureHeight));
                        ctx.DrawText(bottomRightAlignDrawingOptions, text, measurementFont, Color.Black, location);
                    });
                }

                seen.Clear();
                driver.DisplayImage(image);
            });
        }

        private static string GetMeasureLabel(Measure measure) => measure switch
        {
            Measure.Humidity => "Rel\nHum",
            Measure.Temperature => "°F",
            Measure.CO2 => "CO₂\nppm",
            Measure.BarometricPressure => "Atm",
            _ => throw new Exception($"Unsupported measure '{measure}'.")
        };
    }
}
