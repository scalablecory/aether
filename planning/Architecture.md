# Architecture

This describes each distinct building block in Aether, and how they tie together.
This is intended to be a living document.

## Driver

Drivers perform low-level I/O with devices attached via e.g. I<sup>2</sup>C, SPI, UART.

## Sensor

A sensor wraps a [driver](#driver). A sensor provides an abstraction to describe and execute commands against the driver, an observable loop of measurements, and manages its depenencies on other sensors.

## Display Driver

A [driver](#driver) for a display, such as an LCD or a string of LEDs. Provides an abstraction for painting images to a display.

# Display Theme

Paints measurements from [sensors](#sensor) onto a [display](#display-driver).

# Config

A serializable configuration for Aether. Describes things like:

- Which [sensors](#sensor) are enabled.
- I<sup>2</sup>C addresses of sensors.
- Dependencies between sensors and other sensors.
- Mapping a [display](#display-driver) and sensors to a [theme](#display-theme).

# Control API

An API, exposed over Bluetooth, that provides all functionality required by [Bluetooth apps](#bluetooth-apps).

# Bluetooth Apps

Apps that provide things like [configuration](#config), historical measurement charting and command execution for [sensors](#sensor), etc.

Bluetooth apps are merely a presentation layer, and derive their functionality from the [control API](#control-api) over Bluetooth.