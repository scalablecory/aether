# Aether

Aether is an air quality monitoring software and device, Open Source and powered by .NET 6.

This project is a WIP.

## Features / Ideas

- Small standalone device with a custom 3D-printed case.
- Monitor air quality over a Web Bluetooth enabled website.
- Monitor air quality over Bluetooth/WiFi via a .NET MAUI app.
- No-solder plug-and-play hardware and SD card image for easy deployment.
- (Optionally) Join a WiFi network for multi-room monitoring.
- (Maybe) MQTT support.
- (Maybe) LoRa / LoRaWAN support.

## Contributing

Contributions are welcome. Please [contact me](https://github.com/scalablecory) with any questions. The desired workflow is:

1. File an issue describing the bug or desired enhancement.
2. If you intend to perform the work on an issue, ask for it to be assigned to you.
3. If the issue is for an enhancement, wait for it to be approved.
4. File a PR. Make sure to link the issue it will close in the top comment.

Aether could use contributions for:

- Web and mobile dev, for [apps](Bluetooth%20Apps.md).
- Linux deployment (install instructions and/or SD image creation)
- Linux Bluetooth (need to make the Aether into a BLE device)
- Driver dev (C#).
- General .NET dev (C#).
- 3D modeling for case design.

> If you are a Microsoft employee, Aether is participating in [Hackathon 2021](planning/Hackathon%202021.md). Please sign up at [The Garage](https://garagehackbox.azurewebsites.net/hackathons/2356/projects/105003).

## Hardware Support

Initial support will use specific hardware to support [Build A](builds/a.md). In the future, lower cost options will be supported.

- [Raspberry Pi](https://www.raspberrypi.org/)
- [Waveshare 2.9" E-ink display](https://www.waveshare.com/product/displays/e-paper/epaper-2/2.9inch-e-paper-module.htm)
- [Sensirion SCD4x](https://www.sensirion.com/en/environmental-sensors/carbon-dioxide-sensors/carbon-dioxide-sensor-scd4x/) - CO<sub>2</sub>, temperature, relative humidity
- [Sensirion SHT4x](https://www.sensirion.com/en/environmental-sensors/humidity-sensors/humidity-sensor-sht4x/) - temperature, relative humidity
- [Sensirion SPS30](https://www.sensirion.com/en/environmental-sensors/particulate-matter-sensors-pm25/) - PM<sub>0.5</sub>, PM<sub>1.0</sub>, PM<sub>2.5</sub>, PM<sub>4</sub>, PM<sub>10</sub>
- [Sensirion SGP40](https://www.sensirion.com/en/environmental-sensors/gas-sensors/sgp40/) - VOC detector
- [TE MS5637](https://www.te.com/commerce/DocumentDelivery/DDEController?Action=srchrtrv&DocNm=MS5637-02BA03&DocType=Data+Sheet&DocLang=English) - barometric pressure
- [Dongguan OPSCO Optoelectronics SK9822](https://www.opscoled.com/en/product/details.html?id=19) - Addressable RGB LED

## .NET Contributions

After being proven in Aether, drivers are contributed to [dotnet/iot](https://github.com/dotnet/iot/). So far, this has been:

- [Sensirion SCD4x](https://github.com/dotnet/iot/tree/main/src/devices/Scd4x) - CO<sub>2</sub>, temperature, relative humidity
- [Sensirion SHT4x](https://github.com/dotnet/iot/tree/main/src/devices/Sht4x) - temperature, relative humidity

# License

Aether is licensed under the [MIT license](https://opensource.org/licenses/MIT).
