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

        public static IDisposable Run(DisplayDriver driver, IEnumerable<Measure> lines, IObservable<Measurement> source, bool vertical)
        {
            // setup dimensions and common measurements.

            (int imgWidth, int imgHeight, float dpiX, float dpiY, DrawOrientation drawOrientation) =
                (vertical && driver.Height >= driver.Width) || (!vertical && driver.Width >= driver.Height)
                ? (driver.Width, driver.Height, driver.DpiX, driver.DpiY, DrawOrientation.Default)
                : (driver.Height, driver.Width, driver.DpiY, driver.DpiX, DrawOrientation.Rotate90);

            float marginInPixelsX = MarginInInches * dpiX;
            float marginInPixelsY = MarginInInches * dpiY;

            // create image and load fonts.

            Image image = driver.CreateImage(imgWidth, imgHeight);

            var fontCollection = new FontCollection();
            fontCollection.Install("fonts/Manrope-Regular.ttf");

            FontFamily fontFamily = fontCollection.Find("Manrope");
            Font measurementFont = fontFamily.CreateFont(20.0f);
            Font labelFont = fontFamily.CreateFont(7.0f);

            // find maximum measurement size.

            var measurementRendererOptions = new RendererOptions(measurementFont)
            {
                DpiX = dpiX,
                DpiY = dpiY
            };

            FontRectangle measurementRect = TextMeasurer.Measure("19,888", measurementRendererOptions);

            // find maximum label size.

            float maxLabelWidth = 0.0f;
            float maxLabelHeight = 0.0f;

            var labelRendererOptions = new RendererOptions(labelFont)
            {
                DpiX = dpiX,
                DpiY = dpiY
            };

            foreach (Measure measure in lines)
            {
                FontRectangle rect = TextMeasurer.Measure(GetMeasureLabel(measure), labelRendererOptions);

                maxLabelWidth = MathF.Max(maxLabelWidth, rect.Width);
                maxLabelHeight = MathF.Max(maxLabelHeight, rect.Height);
            }

            // find line size. with '-' being margin, 'M' being measurement, and 'L' being label, this looks like:
            // ----
            // M-L-

            float labelAndMarginWidth = MathF.Ceiling(maxLabelWidth + marginInPixelsX * 2.0f);
            float lineWidth = MathF.Ceiling(measurementRect.Width + labelAndMarginWidth);
            float lineHeight = MathF.Ceiling(Math.Max(measurementRect.Height, maxLabelHeight) + MarginInInches);

            // find number of columns/rows to fit on the display.

            int columnCount = Math.Max(imgWidth / (int)lineWidth, 1);
            int rowCount = Math.Max(imgHeight / (int)lineHeight, 1);

            lineWidth = (imgWidth + columnCount - 1) / columnCount;

            // map measures to column/row.

            var offsets = new Dictionary<Measure, PointF>();

            int x = 0, y = 0;
            foreach (Measure measure in lines)
            {
                float offsetX = (x + 1) * lineWidth - labelAndMarginWidth;
                float offsetY = (y + 1) * lineHeight;
                offsets[measure] = new PointF(offsetX, offsetY);

                if (++x == columnCount)
                {
                    ++y;
                    x = 0;
                }
            }

            // setup drawing options.
            // bottom left for labels, bottom right for measurements.

            var labelDrawingOptions = new DrawingOptions
            {
                GraphicsOptions = { Antialias = false },
                TextOptions =
                {
                    DpiX = dpiX,
                    DpiY = dpiY,
                    VerticalAlignment = VerticalAlignment.Bottom
                }
            };

            var measurementDrawingOptions = new DrawingOptions
            {
                GraphicsOptions = { Antialias = false },
                TextOptions =
                {
                    DpiX = dpiX,
                    DpiY = dpiY,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                }
            };

            // draw static labels.

            image.Mutate(ctx =>
            {
                ctx.Fill(Color.White);

                foreach ((Measure measure, PointF location) in offsets)
                {
                    var adjustedLocation = new PointF(location.X + marginInPixelsX, location.Y - 6.0f);

                    string text = GetMeasureLabel(measure);
                    ctx.DrawText(labelDrawingOptions, text, labelFont, Color.Black, adjustedLocation);
                }
            });

            driver.DisplayImage(image, drawOrientation);

            // wire up against the source.
            // TODO: abstract and localize stringy bits.

            var seen = new HashSet<Measure>();

            return source.Gate().Subscribe(measurements =>
            {
                bool draw = false;

                for (int i = measurements.Count - 1; i >= 0; --i)
                {
                    Measurement measurement = measurements[i];
                    
                    if (!seen.Add(measurement.Measure) || !offsets.TryGetValue(measurement.Measure, out PointF location))
                    {
                        continue;
                    }

                    draw = true;

                    string text = measurement.Measure switch
                    {
                        Measure.Humidity => (measurement.RelativeHumidity.Value * (1.0 / 100.0)).ToString("P0"),
                        Measure.Temperature => measurement.Temperature.DegreesFahrenheit.ToString("N1"),
                        Measure.CO2 => measurement.Co2.PartsPerMillion.ToString("N0"),
                        Measure.BarometricPressure => measurement.BarometricPressure.Atmospheres.ToString("N2"),
                        Measure.VOC => measurement.Voc.Value.ToString(),
                        Measure.PM1_0 or Measure.PM2_5 or Measure.PM4_0 or Measure.PM10_0 => measurement.MassConcentration.MicrogramsPerCubicMeter.ToString("N0"),
                        Measure.P1_0 or Measure.P2_5 or Measure.P4_0 or Measure.P10_0 => measurement.NumberConcentration.Value.ToString("N0"),
                        Measure.TypicalParticleSize => measurement.Length.Micrometers.ToString("N1"),
                        _ => throw new Exception($"Unsupported measure '{measurement.Measure}'.")
                    };

                    image.Mutate(ctx =>
                    {
                        ctx.Fill(Color.White, new RectangleF(
                            MathF.Floor(location.X - measurementRect.Width),
                            MathF.Floor(location.Y - lineHeight),
                            MathF.Ceiling(measurementRect.Width),
                            MathF.Ceiling(lineHeight)
                            ));

                        // TODO: it's possible super large values (PM2.5 seems to have this issue) will overwrite the labels.
                        ctx.DrawText(measurementDrawingOptions, text, measurementFont, Color.Black, location);
                    });
                }

                seen.Clear();

                if (draw)
                {
                    driver.DisplayImage(image, drawOrientation);
                }
            });
        }

        private static string GetMeasureLabel(Measure measure) => measure switch
        {
            Measure.Humidity => "Rel\nHum",
            Measure.Temperature => "°F",
            Measure.CO2 => "CO₂\nppm",
            Measure.BarometricPressure => "Atm",
            Measure.VOC => "VOC\nIdx",
            Measure.PM1_0 => "PM₁.₀\nμg/m³",
            Measure.PM2_5 => "PM₂.₅\nμg/m³",
            Measure.PM4_0 => "PM₄.₀\nμg/m³",
            Measure.PM10_0 => "PM₁₀.₀\nμg/m³",
            Measure.P1_0 => "PM₁.₀\n#/cm³",
            Measure.P2_5 => "PM₂.₅\n#/cm³",
            Measure.P4_0 => "PM₄.₀\n#/cm³",
            Measure.P10_0 => "PM₁₀.₀\n#/cm³",
            Measure.TypicalParticleSize => "P.Sz.\nμm",
            _ => throw new Exception($"Unsupported measure '{measure}'.")
        };
    }
}
