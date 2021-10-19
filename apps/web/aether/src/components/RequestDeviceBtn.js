import React from "react";
import { AetherContext } from "../context/AetherContext";

const events = [];

export default class RequestDeviceBtn extends React.Component {
  state = {
    currentDevice: null,
  };

  connectDevice = () => {
    navigator.bluetooth
      .requestDevice({
        acceptAllDevices: true,
        optionalServices: ["battery_service"], // Required to access service later.
      })
      .then((device) => {
        console.log("device: ", device);

        // Set up event listener for when device gets disconnected.
        device.addEventListener("gattserverdisconnected", this.onDisconnected);

        this.setState({ currentDevice: device });
        return device.gatt.connect();
      })
      .then((server) => {
        console.log("server: ", server);

        // Getting Aether Service
        this.context.connect(this.state.currentDevice);
        return server.getPrimaryService("battery_service");
      })
      .then((service) => {
        console.log("service: ", service);

        // Getting Aether Characteristic
        return service.getCharacteristic("battery_level");
      })
      .then((characteristic) => {
        console.log("characteristic: ", characteristic);

        // Set up event listener for when characteristic value changes.
        events.push(
          characteristic.addEventListener(
            "characteristicvaluechanged",
            this.handleMeasureChanged
          )
        );

        // Reading Aether current measure
        return characteristic.readValue();
      })
      .catch((error) => {
        this.context.setError(error);
      });
  };

  disconnectDevice = () => {
    this.state.currentDevice.gatt.disconnect();
    this.context.disconnect();
  };

  handleMeasureChanged = (event) => {
    const measure = event.target.value.getUint8(0);
    this.context.readData(measure);
  };

  onDisconnected = (event) => {
    const device = event.target;
    console.log(`Device ${device.name} is disconnected.`);
    this.context.disconnect();
  };

  componentWillUnmount() {
    this.disconnectDevice();
    events.forEach((e) => e.remove());
  }

  render() {
    const btnText = this.context.isConnected
      ? "Disconnect Aether"
      : "Connect Aether";

    const onClickCallback = this.context.isConnected
      ? this.disconnectDevice
      : this.connectDevice;

    return (
      <div className="connectBtn">
        <button
          type="button"
          className="btn btn-outline-success"
          onClick={onClickCallback}
        >
          {btnText}
        </button>
      </div>
    );
  }
}

RequestDeviceBtn.contextType = AetherContext;
