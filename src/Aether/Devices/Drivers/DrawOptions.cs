namespace Aether.Devices.Drivers
{
    /// <summary>
    /// Options for drawing to a <see cref="DisplayDriver"/>.
    /// </summary>
    [Flags]
    public enum DrawOptions
    {
        /// <summary>
        /// Default draw behavior.
        /// </summary>
        None,

        /// <summary>
        /// If supported, perform a refresh that trades quality for improved speed.
        /// </summary>
        PartialRefresh = 1,

        /// <summary>
        /// Rotates the image counter-clockwise by 90 degrees.
        /// </summary>
        Rotate90 = 2,
    }
}
