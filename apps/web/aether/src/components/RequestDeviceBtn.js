const RequestDeviceBtn = () => {
  const requestDevice = () => {
    navigator.bluetooth
      .requestDevice({ acceptAllDevices: true })
      .then((device) => {
        console.log(device);
      })
      .catch((error) => {
        console.error(error);
      });
  };

  return (
    <div className="connectBtn">
      <button
        type="button"
        className="btn btn-outline-success"
        onClick={requestDevice}
      >
        Connect Aether
      </button>
    </div>
  );
};

export default RequestDeviceBtn;
