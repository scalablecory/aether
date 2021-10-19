import { BrowserRouter as Router, Switch, Route } from "react-router-dom";
import "./App.css";
import Header from "./components/Header";
import DeviceStatus from "./components/DeviceStatus";
import History from "./pages/History";
import Home from "./pages/Home";

function App() {
  return (
    <Router>
      <Header />
      <DeviceStatus />
      <Switch>
        <Route path="/history">
          <History />
        </Route>
        <Route path="/">
          <Home />
        </Route>
      </Switch>
    </Router>
  );
}

export default App;
