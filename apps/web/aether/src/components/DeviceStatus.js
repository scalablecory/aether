import { useContext } from "react";
import { AetherContext } from "../context/AetherContext";

const DeviceStatus = () => {
  const aetherContext = useContext(AetherContext);

  const renderDissmisBtn = () => {
    return (
      <button
        type="button"
        className="btn-close"
        data-bs-dismiss="alert"
        aria-label="Close"
      ></button>
    );
  };

  const renderStatusAlert = () => {
    if (aetherContext.errorMessage.length) {
      return (
        <div className="alert alert-danger alert-dismissible" role="alert">
          {aetherContext.errorMessage}
          {renderDissmisBtn()}
        </div>
      );
    }

    if (aetherContext.isConnected) {
      return (
        <div className="alert alert-success alert-dismissible" role="alert">
          Aether is succesfuly connected!
          {renderDissmisBtn()}
        </div>
      );
    }

    return (
      <div className="alert alert-warning alert-dismissible" role="alert">
        Aether is not connected. Please connect with Aether!
        {renderDissmisBtn()}
      </div>
    );
  };

  return <div className="container">{renderStatusAlert()}</div>;
};

export default DeviceStatus;
