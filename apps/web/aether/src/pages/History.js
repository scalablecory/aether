import { useContext } from "react";
import { AetherContext } from "../context/AetherContext";

const History = () => {
  const aetherContext = useContext(AetherContext);
  return (
    <div className="container">
      <table class="table">
        <thead>
          <tr>
            <th scope="col">#</th>
            <th scope="col">Measure</th>
            <th scope="col">Description</th>
            <th scope="col">Device</th>
            <th scope="col">Time</th>
          </tr>
        </thead>
        <tbody>
          {aetherContext.data.map((item, index) => {
            return (
              <tr key={index}>
                <th scope="row">{index + 1}</th>
                <td>{item.measure}%</td>
                <td>{item.description}</td>
                <td>{item.device}</td>
                <td>{item.time}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};

export default History;
