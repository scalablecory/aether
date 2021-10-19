function AirQualityList() {
  return (
    <div className="accordion" id="accordionAirQuality">
      <div className="accordion-item">
        <h2 className="accordion-header" id="headingGreen">
          <button
            className="accordion-button"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#collapseGreen"
            aria-expanded="true"
            aria-controls="collapseGreen"
          >
            Green - Good
          </button>
        </h2>
        <div
          id="collapseGreen"
          className="accordion-collapse collapse show"
          aria-labelledby="headingGreen"
          data-bs-parent="#accordionAirQuality"
        >
          <div className="accordion-body">
            Air quality is satisfactory, and air pollution poses little or no
            risk.
          </div>
        </div>
      </div>
      <div className="accordion-item">
        <h2 className="accordion-header" id="headingYellow">
          <button
            className="accordion-button collapsed"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#collapseYellow"
            aria-expanded="false"
            aria-controls="collapseYellow"
          >
            Yellow - Moderate
          </button>
        </h2>
        <div
          id="collapseYellow"
          className="accordion-collapse collapse"
          aria-labelledby="headingYellow"
          data-bs-parent="#accordionAirQuality"
        >
          <div className="accordion-body">
            Air quality is acceptable. However, there may be a risk for some
            people, particularly those who are unusually sensitive to air
            pollution.
          </div>
        </div>
      </div>
      <div className="accordion-item">
        <h2 className="accordion-header" id="headingOrange">
          <button
            className="accordion-button collapsed"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#collapseOrange"
            aria-expanded="false"
            aria-controls="collapseOrange"
          >
            Orange - Unhealthy for Sensitive Groups
          </button>
        </h2>
        <div
          id="collapseOrange"
          className="accordion-collapse collapse"
          aria-labelledby="headingOrange"
          data-bs-parent="#accordionAirQuality"
        >
          <div className="accordion-body">
            Members of sensitive groups may experience health effects. The
            general public is less likely to be affected.
          </div>
        </div>
      </div>
      <div className="accordion-item">
        <h2 className="accordion-header" id="headingRed">
          <button
            className="accordion-button collapsed"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#collapseRed"
            aria-expanded="false"
            aria-controls="collapseRed"
          >
            Red - Unhealthy
          </button>
        </h2>
        <div
          id="collapseRed"
          className="accordion-collapse collapse"
          aria-labelledby="headingRed"
          data-bs-parent="#accordionAirQuality"
        >
          <div className="accordion-body">
            Some members of the general public may experience health effects;
            members of sensitive groups may experience more serious health
            effects.
          </div>
        </div>
      </div>
      <div className="accordion-item">
        <h2 className="accordion-header" id="headingPurple">
          <button
            className="accordion-button collapsed"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#collapsePurple"
            aria-expanded="false"
            aria-controls="collapsePurple"
          >
            Purple - Very Unhealthy
          </button>
        </h2>
        <div
          id="collapsePurple"
          className="accordion-collapse collapse"
          aria-labelledby="headingPurple"
          data-bs-parent="#accordionAirQuality"
        >
          <div className="accordion-body">
            Health alert: The risk of health effects is increased for everyone.
          </div>
        </div>
      </div>
      <div className="accordion-item">
        <h2 className="accordion-header" id="headingMaroon">
          <button
            className="accordion-button collapsed"
            type="button"
            data-bs-toggle="collapse"
            data-bs-target="#collapseMaroon"
            aria-expanded="false"
            aria-controls="collapseMaroon"
          >
            Maroon - Hazardous
          </button>
        </h2>
        <div
          id="collapseMaroon"
          className="accordion-collapse collapse"
          aria-labelledby="headingMaroon"
          data-bs-parent="#accordionAirQuality"
        >
          <div className="accordion-body">
            Health warning of emergency conditions: everyone is more likely to
            be affected.
          </div>
        </div>
      </div>
    </div>
  );
}

export default AirQualityList;
