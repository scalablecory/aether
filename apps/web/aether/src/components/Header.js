import logo from "../assets/logo.png";
import RequestDeviceBtn from "./RequestDeviceBtn";

const Header = () => {
  return (
    <nav className="navbar navbar-light bg-light">
      <a className="navbar-brand" href="/">
        <img
          src={logo}
          width="30"
          height="30"
          className="d-inline-block align-top"
          alt=""
          style={{ marginLeft: "16px", marginRight: "16px" }}
        />
        Aether
      </a>
      <RequestDeviceBtn />
    </nav>
  );
};

export default Header;
