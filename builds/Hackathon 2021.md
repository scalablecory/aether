# Hackathon 2021

<a href="http://www.youtube.com/watch?v=VsS-zbfhtrs"><img align="right" src="https://img.youtube.com/vi/VsS-zbfhtrs/0.jpg" style="width:20em"></a>

This was created as part of Microsoft's [2021 Hackathon](https://garagehackbox.azurewebsites.net/hackathons/2356/projects/105003).

See a video of it in action to the right.<br clear="right" />

## Parts List

Parts are chosen to be plug and play, with no soldering required. This build uses the [Qwiic](https://www.sparkfun.com/qwiic) I<sup>2</sup>C wiring system to be very easy to piece together.

Sensors can be left off if uninterested in their measurements.

| Part | Description | Sparkfun Link | Adafruit Link
| ---- | ----------- | ------------- | -------------
| Raspberry Pi 4 Model B 2GB | Main board. | [SF Pi 4](https://www.sparkfun.com/products/15446) | [AF Pi 4](https://www.adafruit.com/product/4292)
| Sensirion SCD40 | CO<sub>2</sub>, Relative Humidity, and Temperature sensor. | [SF SCD40](https://www.sparkfun.com/products/18365) | [AF SCD40](https://www.adafruit.com/product/5187)
| TE MS5637 | Barometric Pressure and Temperature sensor. Used to calibrate the SDC40. | [SF MS5637](https://www.sparkfun.com/products/14688)
| Sensirion SGP40 | VOC change sensor. | [SF SGP40](https://www.sparkfun.com/products/18345) | [AF SGP40](https://www.adafruit.com/product/4829)
| Sensirion SPS30 | PM<sub>0.5</sub>, PM<sub>1.0</sub>, PM<sub>2.5</sub>, PM<sub>4</sub>, and PM<sub>10</sub> sensor. | [SF SPS30](https://www.sparkfun.com/products/15103)
| [Waveshare 2.9" E-ink](https://www.waveshare.com/product/displays/e-paper/epaper-2/2.9inch-e-paper-module.htm) | A display to show measurements.

### Addressable RGB

To wire the LEDs, some breadboarding or soldering will be required.

Other than the LEDs themselves, these parts do not require software support. Feel free to mix and match.

| Part | Description | Mouser Link | Adafruit Link
| ---- | ----------- | ------------- | -------------
| SK9822 / APA102C | Addressable RGB LEDs. | [SF APA102C](https://www.sparkfun.com/products/14015) | [AF SK9822](https://www.adafruit.com/product/2239?length=1)
| 3.3v to 5v level shifter, min two inputs | Converts signals from Pi's 3.3v to the LED's 5v. | [Mouser SN74AHCT125N](https://www.mouser.com/ProductDetail/595-SN74AHCT125N) | [AF SN74AHCT125N](https://www.adafruit.com/product/1787)
| Capacitor, min 5V 1000μF | To smooth out power demand of LEDs. | [Mouser ESW108M6R3AH1AA](https://www.mouser.com/ProductDetail/80-ESW108M6R3AH1AA)

## Operating System Setup

Before wiring everything up, lets setup the software.

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>1. <a href="https://www.raspberrypi.com/software/">Flash Raspberry Pi OS to a MicroSD card</a>.</p>

<p>I recommend using the Lite (32-bit) image, though the full Desktop image will work as well.</p><br clear="left" />

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>2. Enable I2C and SPI.</p>

<p>Each GPIO on the Raspberry Pi can have many different functions. We need to enable the I2C1, SPI0, and SPI1 interfaces.</p>

<p>Open config.txt file from the boot partition of the MicroSD card, and find this section of the file:</p>

<pre># Uncomment some or all of these to enable the optional hardware interfaces
#dtparam=i2c_arm=on
#dtparam=i2s=on
#dtparam=spi=on</pre>

And change that section to look like:

<pre># Uncomment some or all of these to enable the optional hardware interfaces
dtparam=i2c_arm=on
#dtparam=i2s=on
dtparam=spi=on

dtoverlay=spi1-1cs,cs0_pin=23</pre>

<br clear="left" />

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>3. Configure SSH.</p>

<p>To run headless and/or allow you to login to your Pi via SSH, create an empty file with the name "ssh" in the same directory as config.txt.</p><br clear="left" />

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>4. Configure WiFi (Optional).</p>

<p>If you want to connect to the Pi via WiFi, configure it now by adding a file wpa_supplicant.conf to the same directory as config.txt:</p>

<pre>country=US
ctrl_interface=DIR=/var/run/wpa_supplicant GROUP=netdev
update_config=1

network={
ssid="your ssid"
psk="your psk"
key_mgmt=WPA-PSK
}</pre><br clear="left" />

## Wiring Instructions

If this is your first time putting together an IoT project with a Raspberry Pi, take some time to get familiar with its GPIO pins:

![I<sup>2</sup>C, SPI, and other pins](raspberry-pi-pinout.png "Raspberry Pi GPIO pins")

It's not as intimidating as it looks, but please take it easy. It's very possible you'll fry your hardware if you hook things up incorrectly.

 The Hackathon 2021 build makes use of large number of these pins, such as:

- 3V3, 5V, and Ground (the black pins), to power things.
- I2C1, to connect Qwiic devices.
- SPI0, to connect the screen.
- SPI1, to connect addressable RGB LEDs.
- Several of the digital pins.

Lets wire things up step by step. You'll want to have several Qwiic cables and some jumper cables available.

### SCD40

<img src="qwiic-jst-cable.jpg" alt="Qwiic cable with GPIO pin connectors and a JST connector" title="Qwiic cable" style="width:8em;margin:0.75em;float:left" />

<p>1. Grab a Qwiic to Female Jumper cable. The SCD40 runs over I<sup>2</sup>C, and will be connected to I2C1, 3V3, and Ground.</p>

<p>Proceed to the next step to plug it in.</p><br clear="left" />

<img src="qwiic-jst-connector.jpg" alt="Qwiic cable connecting to device via JST" title="Qwiic cable connecting to device" style="width:8em;margin:0.75em;float:left" />

<p>2. Connect the JST end of the Qwiic cable to the SCD40.</p><br clear="left" />

<img src="qwiic-gpio-connectors.jpg" alt="Qwiic cable female jumpbers to connect to Raspberry Pi" title="Qwiic cable female jumpbers" style="width:8em;margin:0.75em;float:left" />

<p>3. Connect the female jumper end of the Qwiic cable to the Raspberry Pi.</p>

<p>The wire is color coded to know which hookup to make:</p>

<ul style="overflow: hidden">
<li>Black = GND (any)</li>
<li>Red = 3V3 (any)</li>
<li>Blue = I2C1 SDA</li>
<li>Yellow = I2C1 SCL</li>
</ul><br clear="left" />

### MS5637

<img src="qwiic-qwiic-cable.jpg" alt="Qwiic cable to connect SCD40 to MS5637 via JST" title="Qwiic device to device cable" style="width:8em;margin:0.75em;float:left" />

<p>4. Time to connect your MS5637. Now grab a Qwiic JST-to-JST cable.</p>

<p>Proceed to the next step to plug it in.</p><br clear="left" />

<img src="qwiic-chained.jpg" alt="Qwiic cable connecting a SCD40 to a MS5637 via JST" title="Qwiic device to device connection" style="width:8em;margin:0.75em;float:left" />

<p>5. Plug one end of the cable into the remaining empty JST header on the SCD40.</p><br clear="left" />

<img src="qwiic-jst-connector.jpg" alt="Qwiic cable connecting to device via JST" title="Qwiic cable connecting to device" style="width:8em;margin:0.75em;float:left" />

<p>6. Connect the other end of the cable into the MS5637.</p>

<p>Qwiic is designed to be easily chained together, and it doesn't matter which device is connected first or last.</p><br clear="left" />

### SGP40

<img src="qwiic-chained.jpg" alt="Qwiic cable connecting a MS5637 to a SGP40 via JST" title="Qwiic device to device connection" style="width:8em;margin:0.75em;float:left" />

<p>7. Repeat the MS5637 instructions, this time chaining the MS5637 to the SGP40.<br clear="left" />

### SPS30

<img src="sps30-cable.jpg" alt="Cable to connect SPS30 to Raspberry Pi via a JST ZHR-5 connector" title="Cable to connect SPS30 to Raspberry Pi" style="width:8em;margin:0.75em;float:left" />

<p>8. Grab the SPS30 cable.</p>

<p>On one side, you have a JST ZHR-5 connector that goes into the SPS30. On the other side are male jumper pins.</p><br clear="left" />

<img src="qwiic-jst-cable.jpg" alt="Qwiic cable with GPIO pin connectors and a JST connector" title="Qwiic cable" style="width:8em;margin:0.75em;float:left" />

<p>9. Grab a Qwiic to Female Jumper cable. Connect three of the female jumpers to the SPS30 cable.</p>

<p>The wire is color coded to know which hookup to make:</p>

<ul style="overflow: hidden">
<li>Black (Qwiic cable) = Black (SPS30 cable)</li>
<li>Blue (Qwiic cable) = White (SPS30 cable)</li>
<li>Yellow (Qwiic cable) = Purple (SPS30 cable)</li>
</ul><br clear="left" />

<img src="jst-jumper-female-female.jpg" alt="A bundle of female to female jumper wires" title="Female to female jumper cables" style="width:8em;margin:0.75em;float:left" />

<p>10. Grab two short Female to Female Jumper cables.</p>

<p>These will be used to connect the remaining two SPS30 wires to the Raspberry Pi. The wire is color coded to know which hookup to make.</p>

<ul style="overflow: hidden">
<li>Red = 5V (any)</li>
<li>Green = GND (any)</li>
</ul><br clear="left" />

<img src="sps30-header.jpg" alt="The JST ZHR-5 header on the SPS30" title="JST ZHR-5 header to connect cable to SPS30" style="width:8em;margin:0.75em;float:left" />

<p>11. Connect the JST ZHR-5 connector from your SPS30 cable to the header on the SPS30.</p>

<p>If the connector seems loose, it may not be plugged in all the way.</p><br clear="left" />

<img src="sps30-intake.jpg" alt="The intake of the SPS30" title="SPS30 intake" style="width:8em;margin:0.75em;float:left" />

<p>When mounting the SPS30, take care to give the intake (with its fibrous filter) good access to air.</p><br clear="left" />

<img src="sps30-outtake.jpg" alt="The outtake of the SPS30" title="SPS30 outtake" style="width:8em;margin:0.75em;float:left" />

<p>When mounting the SPS30, take care to give four outtake holes good access to air.</p>

<p><b>Important:</b> The metal sides of the SPS30 are connected to ground. Be sure it is not touching any conductors, or a short circuit may destroy your devices.</p><br clear="left" />

### 2.9" E-Paper Display

<img src="waveshare-2_9in-front.jpg" alt="The Waveshare 2.9&quot; E-Paper Display" title="The 2.9&quot; E-Paper Display" style="width:8em;margin:0.75em;float:left" />

<p>12. Lets install the 2.9" E-Paper Display.<br clear="left" />

<p>Observe the ordering of the pins on the rear left header of the 2.9" E-Paper Display:</p><img src="waveshare-2_9in-back.jpg" alt="The connectors of the 2.9&quot; E-Paper Display" title="The rear of the 2.9&quot; E-Paper Display" /><br clear="left" />

<img src="waveshare-2_9in-cable.jpg" alt="The cable used to connect the 2.9&quot; E-Paper Display to the Raspberry Pi" title="Cable for the 2.9&quot; E-Paper Display" style="width:8em;margin:0.75em;float:left" />

<p>13. Grab the SPS30 cable.</p>

<p>Connect the cable to the 2.9" E-Paper Display module. Connect the other end to the Raspberry Pi. The wire is color coded to know which hookup to make, but double check with the labels on the back in case they change the colors:</p>

<ul style="overflow: hidden">
<li>VCC = 5V (any)</li>
<li>GND = GND (any)</li>
<li>DIN = SPI0 MOSI</li>
<li>CLK = SPI0 SCK</li>
<li>CS = CE0</li>
<li>DC = 25</li>
<li>RST = 17</li>
<li>BUSY = 24</li>
</ul><br clear="left" />

### LEDs

To connect LEDs, soldering a protoboard is recommended. A breadboard may not be able to handle the power requirements.

TODO

## Aether Software Setup

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>1. Connect to your Raspberry Pi.</p>

<p>If you're running headless, SSH into the Pi remotely. Otherwise, connect a screen/keyboard, login, and open a terminal.</p>

<p>The default username and password for a Raspberry Pi are "pi" and "raspberry".</p><br clear="left" />

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>2. Install .NET 6</p>

<p>Follow the <a href="https://docs.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install">.NET 6 Install instructions</a>.</p><br clear="left" />

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>3. Clone Aether</p>
<pre>git clone https://github.com/scalablecory/aether.git
cd aether/src/Aether</pre><br clear="left" />

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>4. Edit Program.cs</p>

<p>Modify the main command handler based on the hardware you installed. You may need to tweak pin numbers if you didn't wire things exactly the same, or set a different pixel count if you installed addressable RGB.</p>

<p>TODO: replace this step with modifying a config file?</p><br clear="left" />

<img src="RPi-Logo-Reg-SCREEN.png" alt="Raspberry Pi logo" title="Raspberry Pi logo" style="width:8em;margin:0.75em;float:left" />

<p>5. Run Aether</p>

<pre>dotnet run -c Release -- run-device</pre><br clear="left" />

## Copyright Notice

Images &copy; their respective owners. Images &copy; SparkFun are [CC BY 2.0](https://creativecommons.org/licenses/by/2.0/).