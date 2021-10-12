import GaugeChart from "react-gauge-chart";
import AirQualityTable from "./components/AirQualityTable";
import "./App.css";

function App() {
  return (
    <div className="app">
      <h1>Aether</h1>
      <GaugeChart
        id="gauge-chart6"
        animate={false}
        nrOfLevels={6}
        percent={0.56}
        needleColor="#345243"
        colors={["green", "yellow", "orange", "red", "purple", "maroon"]}
      />
      <AirQualityTable />
    </div>
  );
}

export default App;
