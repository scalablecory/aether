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
        private const float OuterMarginInInches = 0.1f;
        private const float InnerMarginInInches = 0.0f;
        private const float MeasurementSizeInPoints = 20.0f;
        private const float LabelSizeInPoints = 7.0f;

        public static IDisposable Run(DisplayDriver driver, IEnumerable<Measure> lines, IObservable<Measurement> source, bool vertical)
        {
            // setup dimensions and common measurements.

            (int imgWidth, int imgHeight, float dpiX, float dpiY, DrawOptions drawOptions) =
                (vertical && driver.Height >= driver.Width) || (!vertical && driver.Width >= driver.Height)
                ? (driver.Width, driver.Height, driver.DpiX, driver.DpiY, DrawOptions.None)
                : (driver.Height, driver.Width, driver.DpiY, driver.DpiX, DrawOptions.Rotate90);

            int outerMarginInPixelsX = (int)MathF.Ceiling(OuterMarginInInches * dpiX);
            int outerMarginInPixelsY = (int)MathF.Ceiling(OuterMarginInInches * dpiY);

            int innerMarginInPixelsX = (int)MathF.Ceiling(InnerMarginInInches * dpiX);
            int innerMarginInPixelsY = (int)MathF.Ceiling(InnerMarginInInches * dpiY);

            int workingWidth = imgWidth - outerMarginInPixelsX * 2;
            int workingHeight = imgHeight - outerMarginInPixelsY * 2;

            // create image and load fonts.

            Image image = driver.CreateImage(imgWidth, imgHeight);

            var fontCollection = new FontCollection();
            fontCollection.Install("fonts/Manrope-Regular.ttf");

            FontFamily fontFamily = fontCollection.Find("Manrope");
            Font measurementFont = fontFamily.CreateFont(MeasurementSizeInPoints);
            Font labelFont = fontFamily.CreateFont(LabelSizeInPoints);

            // find maximum measurement size.

            var measurementRendererOptions = new RendererOptions(measurementFont)
            {
                DpiX = dpiX,
                DpiY = dpiY
            };

            FontRectangle measurementRect = TextMeasurer.Measure("19,888", measurementRendererOptions);
            int measurementWidth = (int)MathF.Ceiling(measurementRect.Width);

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

            // find line size.

            int labelAndMarginWidth = (int)MathF.Ceiling(maxLabelWidth + innerMarginInPixelsX);
            int lineWidth = (int)MathF.Ceiling(measurementWidth + labelAndMarginWidth);
            int lineHeight = (int)MathF.Ceiling(Math.Max(measurementRect.Height, maxLabelHeight));

            // find number of columns/rows to fit on the display.

            int columnCount = Math.Max((workingWidth + innerMarginInPixelsX) / (lineWidth + innerMarginInPixelsX), 1);
            int rowCount = Math.Max((workingHeight + innerMarginInPixelsY) / (lineHeight + innerMarginInPixelsY), 1);

            // adjust line width to take up full screen.

            lineWidth = (workingWidth - (innerMarginInPixelsX * (columnCount - 1))) / columnCount;

            // map measures to column/row.

            var offsets = new Dictionary<Measure, Point>();

            int x = 0, y = 0;
            foreach (Measure measure in lines)
            {
                int offsetX = imgWidth - outerMarginInPixelsX - (columnCount - x - 1) * (lineWidth + innerMarginInPixelsX) - labelAndMarginWidth;
                int offsetY = outerMarginInPixelsY + (lineHeight + innerMarginInPixelsY) * y + lineHeight;
                offsets[measure] = new Point(offsetX, offsetY);

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

                foreach ((Measure measure, Point location) in offsets)
                {
                    var adjustedLocation = new PointF(location.X + innerMarginInPixelsX, location.Y - 6);

                    string text = GetMeasureLabel(measure);
                    ctx.DrawText(labelDrawingOptions, text, labelFont, Color.Black, adjustedLocation);
                }
            });

            driver.DisplayImage(image, drawOptions);

            drawOptions |= DrawOptions.PartialRefresh;

            // wire up against the source.
            // TODO: abstract and localize stringy bits.

            var seen = new HashSet<Measure>();
            var previousValues = new Dictionary<Measure, string>();
            Point location = default;
            string text;

            void Draw(IImageProcessingContext ctx)
            {
                ctx.Fill(Color.White, new RectangleF(
                    location.X - measurementWidth,
                    location.Y - lineHeight,
                    measurementWidth,
                    lineHeight
                    ));

                // TODO: it's possible super large values (PM2.5 seems to have this issue) will overwrite the labels.
                ctx.DrawText(measurementDrawingOptions, text, measurementFont, Color.Black, location);
            }

            return source.Gate().Subscribe(measurements =>
            {
                bool draw = false;

                for (int i = measurements.Count - 1; i >= 0; --i)
                {
                    Measurement measurement = measurements[i];
                    
                    if (!seen.Add(measurement.Measure) || !offsets.TryGetValue(measurement.Measure, out location))
                    {
                        continue;
                    }

                    text = measurement.Measure switch
                    {
                        Measure.Humidity => (measurement.RelativeHumidity.Value * (1.0 / 100.0)).ToString("P0"),
                        Measure.Temperature => measurement.Temperature.DegreesFahrenheit.ToString("N1"),
                        Measure.CO2 => measurement.Co2.PartsPerMillion.ToString("N0"),
                        Measure.BarometricPressure => measurement.BarometricPressure.Atmospheres.ToString("N2"),
                        Measure.VOC => measurement.Voc.Value.ToString("N0"),
                        Measure.PM1_0 or Measure.PM2_5 or Measure.PM4_0 or Measure.PM10_0 => Math.Min(measurement.MassConcentration.MicrogramsPerCubicMeter, 9999).ToString("N0"),
                        Measure.P0_5 or Measure.P1_0 or Measure.P2_5 or Measure.P4_0 or Measure.P10_0 => Math.Min(measurement.NumberConcentration.Value, 9999).ToString("N0"),
                        Measure.TypicalParticleSize => measurement.Length.Micrometers.ToString("N1"),
                        Measure.AirQualityIndex => measurement.AirQualityIndex.Value.ToString("N0"),
                        _ => throw new Exception($"Unsupported measure '{measurement.Measure}'.")
                    };

                    if (previousValues.TryGetValue(measurement.Measure, out string? prevText) && prevText == text)
                    {
                        continue;
                    }

                    previousValues[measurement.Measure] = text;

                    draw = true;

                    image.Mutate(Draw);
                }

                seen.Clear();

                if (draw)
                {
                    driver.DisplayImage(image, drawOptions);
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
            Measure.P0_5 => "PM₀.₅\n#/cm³",
            Measure.P1_0 => "PM₁.₀\n#/cm³",
            Measure.P2_5 => "PM₂.₅\n#/cm³",
            Measure.P4_0 => "PM₄.₀\n#/cm³",
            Measure.P10_0 => "PM₁₀.₀\n#/cm³",
            Measure.TypicalParticleSize => "P.Sz.\nμm",
            Measure.AirQualityIndex => "AQI",
            _ => throw new Exception($"Unsupported measure '{measure}'.")
        };
    }
}
