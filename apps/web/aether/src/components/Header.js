import { Link } from "react-router-dom";
import logo from "../assets/logo.png";
import RequestDeviceBtn from "./RequestDeviceBtn";

const Header = () => {
  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-light">
      <div className="container-fluid">
        <Link className="navbar-brand" to="/">
          <img
            src={logo}
            width="30"
            height="30"
            className="d-inline-block align-top"
            alt=""
            style={{ marginLeft: "16px", marginRight: "16px" }}
          />
          Aether
        </Link>
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarScroll"
          aria-controls="navbarScroll"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon"></span>
        </button>
        <div className="collapse navbar-collapse" id="navbarScroll">
          <ul className="navbar-nav me-auto my-2 my-lg-0 navbar-nav-scroll">
            <li className="nav-item">
              <Link
                className="nav-link active"
                aria-current="page"
                to="/history"
              >
                History
              </Link>
            </li>
          </ul>
          <form className="d-flex">
            <RequestDeviceBtn />
          </form>
        </div>
      </div>
    </nav>
  );
};

export default Header;
