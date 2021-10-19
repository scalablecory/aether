function AirQualityTable() {
  return (
    <div className="air-quality-container">
      <table id="air-quality">
        <tr>
          <th>Color</th>
          <th>Levels of Concern</th>
          <th>Description of Air Quality</th>
        </tr>
        <tr className="green">
          <td>Green</td>
          <td>Good</td>
          <td>
            Air quality is satisfactory, and air pollution poses little or no
            risk.
          </td>
        </tr>
        <tr className="yellow">
          <td>Yellow</td>
          <td>Moderate</td>
          <td>
            Air quality is acceptable. However, there may be a risk for some
            people, particularly those who are unusually sensitive to air
            pollution.
          </td>
        </tr>
        <tr className="orange">
          <td>Orange</td>
          <td>Unhealthy for Sensitive Groups</td>
          <td>
            Members of sensitive groups may experience health effects. The
            general public is less likely to be affected.
          </td>
        </tr>
        <tr className="red">
          <td>Red</td>
          <td>Unhealthy</td>
          <td>
            Some members of the general public may experience health effects;
            members of sensitive groups may experience more serious health
            effects.
          </td>
        </tr>
        <tr className="purple">
          <td>Purple</td>
          <td>Very Unhealthy</td>
          <td>
            Health alert: The risk of health effects is increased for everyone.
          </td>
        </tr>
        <tr className="maroon">
          <td>Maroon</td>
          <td>Hazardous</td>
          <td>
            Health warning of emergency conditions: everyone is more likely to
            be affected.
          </td>
        </tr>
      </table>
    </div>
  );
}

export default AirQualityTable;
