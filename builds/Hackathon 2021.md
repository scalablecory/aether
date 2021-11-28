# Hackathon 2021

This was created as part of Microsoft's [2021 Hackathon](https://garagehackbox.azurewebsites.net/hackathons/2356/projects/105003).

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

## Instructions

TODO.