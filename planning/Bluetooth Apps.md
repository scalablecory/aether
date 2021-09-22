# Bluetooth Apps

There will be two apps to pair and communicate with the Aether.

The first, a PWA-enabled website, will use the still experimental Web Bluetooth standard to pair and communicate with the Aether without any server-side support at all, so it can be a pure static website hosted via Github.

The second, a .NET MAUI app, will also use Bluetooth to talk to the Aether.

These apps will provide real-time and historic charts, and be used to configure the device and run one-off commands against the sensors.

# Bluetooth API

The API/protocol over Bluetooth will need to provide:

- Real-time data
- Historic data, within some window of dates.
- Configuration
- TODO
