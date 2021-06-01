import React, { useEffect, useState } from "react";
import moment from "moment";
import { showLoader, hideLoader } from "@appserver/common/utils";
import TabsContainer from "@appserver/components/tabs-container";
import DatePicker from "@appserver/components/date-picker";
import ComboBox from "@appserver/components/combobox";

const VisitsSelectors = ({ getVisits, i18n, t }) => {
  useEffect(() => {
    (async () => {
      showLoader();

      try {
        await getVisitsData("week");
      } catch (error) {
        toastr.error(error);
      }

      hideLoader();
    })();
  }, []);

  const [isPeriod, setIsPeriod] = useState(false);
  const [selectedPeriod, setSelectedPeriod] = useState(null);
  const [visitsDateFrom, setDateFrom] = useState(
    moment().subtract(6, "months").toDate()
  );
  const [visitsDateTo, setDateTo] = useState(moment().toDate());

  const visitTabs = [
    {
      key: "week",
      title: t("VisitsWeek"),
      label: t("VisitsWeek"),
    },
    {
      key: "month",
      title: t("VisitsMonth"),
      label: t("VisitsMonth"),
    },
    {
      key: "threeMonth",
      title: t("VisitsThreeMonth"),
      label: t("VisitsThreeMonth"),
    },
    {
      key: "period",
      title: t("VisitsPeriod"),
      label: t("VisitsPeriod"),
    },
  ];

  const getVisitsData = (period) => {
    switch (period) {
      case "week":
        return getVisits(
          moment().subtract(7, "days").utc().format(),
          moment().utc().format()
        );
      case "month":
        return getVisits(
          moment().subtract(1, "months").utc().format(),
          moment().utc().format()
        );
      case "threeMonth":
        return getVisits(
          moment().subtract(3, "months").utc().format(),
          moment().utc().format()
        );
      case "period":
        return getVisits(
          moment(visitsDateFrom).utc().format(),
          moment(visitsDateTo).utc().format()
        );
    }
  };

  const setPeriodFromDate = (date) => {
    setDateFrom(date).then(() => getVisitsData("period"));
  };

  const setPeriodToDate = (date) => {
    setDateTo(date).then(() => getVisitsData("period"));
  };

  const setPeriodData = (period) => {
    const { key } = period;

    setIsPeriod(key === "period");
    setSelectedPeriod(period);

    return getVisitsData(key);
  };

  return (
    <div className="visits-selectors-container">
      {window.innerWidth < 1024 ? (
        <ComboBox
          options={visitTabs}
          onSelect={setPeriodData}
          selectedOption={selectedPeriod || visitTabs[0]}
          scaledOptions={true}
          scaled={!window.innerWidth < 1024}
          noBorder={false}
          isDisabled={false}
          showDisabledItems={true}
        />
      ) : (
        <TabsContainer onSelect={setPeriodData} elements={visitTabs} />
      )}
      <div className="visits-datepicker-container">
        <DatePicker
          className="visits-datepicker"
          onChange={setPeriodFromDate}
          selectedDate={visitsDateFrom}
          maxDate={moment().subtract(1, "days").toDate()}
          isDisabled={!isPeriod}
          locale={i18n.language}
        />
        {`-`}
        <DatePicker
          className="visits-datepicker"
          onChange={setPeriodToDate}
          selectedDate={visitsDateTo}
          maxDate={moment().toDate()}
          minDate={moment(visitsDateFrom).add(1, "days").toDate()}
          isDisabled={!isPeriod}
          locale={i18n.language}
        />
      </div>
    </div>
  );
};

export default VisitsSelectors;
