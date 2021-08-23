# Aether

Aether is an air quality monitoring software and device, Open Source and powered by .NET.

This project is a WIP. Contributions welcome -- file issues or [contact me](https://github.com/scalablecory).

## Features / Ideas

- Small standalone device with a custom 3D-printed case.
- Monitor air quality over a Web Bluetooth enabled website.
- Monitor air quality over Bluetooth/WiFi via a .NET MAUI app.
- No-solder plug-and-play hardware and SD card image for easy deployment.
- (Optionally) Join a WiFi network for multi-room monitoring.
- (Maybe) MQTT support.
- (Maybe) LoRa / LoRaWAN support.

## Hardware Support (Planned)

Initial support will use specific high-quality hardware. In the future, lower cost options will be supported.

- [Raspberry Pi](https://www.raspberrypi.org/)
- [Waveshare 2.9" E-ink display](https://www.waveshare.com/product/displays/e-paper/epaper-2/2.9inch-e-paper-module.htm)
- [Sensirion SCD4x](https://www.sensirion.com/en/environmental-sensors/carbon-dioxide-sensors/carbon-dioxide-sensor-scd4x/) - CO<sub>2</sub>, temperature, relative humidity
- [Sensirion SHT4x](https://www.sensirion.com/en/environmental-sensors/humidity-sensors/humidity-sensor-sht4x/) - temperature, relative humidity
- [Sensirion SPS30](https://www.sensirion.com/en/environmental-sensors/particulate-matter-sensors-pm25/) - PM<sub>0.5</sub>, PM<sub>1.0</sub>, PM<sub>2.5</sub>, PM<sub>4</sub>, PM<sub>10</sub>
- [TE MS5637](https://www.te.com/commerce/DocumentDelivery/DDEController?Action=srchrtrv&DocNm=MS5637-02BA03&DocType=Data+Sheet&DocLang=English) - barometric pressure

# License

Aether is licensed under the [GPL version 3](https://www.gnu.org/licenses/gpl-3.0.en.html) or any later version.
