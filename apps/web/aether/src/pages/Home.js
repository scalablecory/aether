import { useContext } from "react";
import GaugeChart from "react-gauge-chart";
import AirQualityList from "../components/AirQualityList";
import { AetherContext } from "../context/AetherContext";

function Home() {
  const aetherContext = useContext(AetherContext);
  const getPercent = () => {
    if (aetherContext.data.length) {
      return aetherContext.data[0].measure / 100;
    }
    return 0;
  };

  return (
    <>
      <div className="container">
        <div className="row">
          <div className="col-lg-8 col-md-12 col-sm-12 chart-container mt-4">
            <GaugeChart
              id="gauge-chart6"
              animate={true}
              nrOfLevels={6}
              percent={getPercent()}
              needleColor="#345243"
              colors={["green", "yellow", "orange", "red", "purple", "maroon"]}
            />
          </div>
          <div className="col-lg-4 col-md-12 col-sm-12 mt-4">
            <AirQualityList />
          </div>
        </div>
      </div>
      <footer>
        <div className="text-center py-3">
          <a href="https://github.com/scalablecory/aether">Aether on Github</a>{" "}
          is licensed under the
          <a href="https://opensource.org/licenses/MIT"> MIT license</a>
        </div>
      </footer>
    </>
  );
}

export default Home;
