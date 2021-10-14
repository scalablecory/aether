import { createContext, useState } from "react";

const AetherContext = createContext({
  isConnected: false,
  bleDevice: null,
  data: [],
  errorMessage: "",
  connect: (device) => {},
  disconnect: () => {},
  readData: (value) => {},
  setError: (error) => {},
});

const AetherContextProvider = ({ children }) => {
  const [isConnected, setIsConnected] = useState(false);
  const [bleDevice, setBleDevice] = useState();
  const [data, setData] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");

  const connect = (device) => {
    setBleDevice(device);
    setIsConnected(true);
  };

  const disconnect = () => {
    setIsConnected(false);
    setBleDevice(null);
  };

  const readData = (value) => {
    const newValue = {
      measure: value,
      description: getDescription(value),
      device: bleDevice.name,
      time: new Date().toLocaleString("en-US", { timeZone: "UTC" }),
    };

    let currentList = [...data];
    if (currentList.length >= 20) {
      // remove the last item
      currentList.pop();
    }

    // Add the new value at the begining of the list
    setData([newValue, ...currentList]);
  };

  const getDescription = (value) => {
    if (value >= 0 && value <= 16) {
      return "Good";
    }

    if (value > 16 && value <= 32) {
      return "Moderate";
    }

    if (value > 32 && value <= 48) {
      return "Unhealthy for sensitive Groups";
    }

    if (value > 48 && value <= 64) {
      return "Unhealthy";
    }

    if (value > 64 && value <= 80) {
      return "Very Unhealthy";
    }

    if (value > 80 && value <= 100) {
      return "Hazardous";
    }
  };

  const setError = (error) => {
    console.error(error);
    if (error) {
      setErrorMessage(error.message);
    } else {
      throw new Error("Oups! Something bad happened.");
    }
  };

  const contextValue = {
    isConnected,
    bleDevice,
    data,
    errorMessage,
    setError,
    connect,
    disconnect,
    readData,
  };

  return (
    <AetherContext.Provider value={contextValue}>
      {children}
    </AetherContext.Provider>
  );
};

export { AetherContext, AetherContextProvider };
