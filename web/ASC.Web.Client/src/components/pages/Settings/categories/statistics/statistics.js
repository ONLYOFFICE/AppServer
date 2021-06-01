import React, { useEffect, useState } from "react";
import PropTypes from "prop-types";
import { withTranslation } from "react-i18next";
import moment from "moment";
import isEmpty from "lodash/isEmpty";
import Text from "@appserver/components/text";
import toastr from "@appserver/components/toast/toastr";
import Chart from "@appserver/components/chart";
import Loader from "@appserver/components/loader";
import { showLoader, hideLoader } from "@appserver/common/utils";
import { inject, observer } from "mobx-react";
import { setDocumentTitle } from "../../../../../helpers/utils";
import Storage from "./sub-components/storage";
import Users from "./sub-components/users";
import VisitsSelectors from "./sub-components/visits";
import MainContainer from "./sub-components/mainContainer";

const Statistics = ({ ...props }) => {
  const { t, tReady, i18n, quota, visits, getQuota, getVisits } = props;

  useEffect(() => {
    setDocumentTitle(t("ManagementCategoryStatistic"));
  }, [setDocumentTitle]);

  useEffect(() => {
    (async () => {
      showLoader();

      if (isEmpty(quota, true)) {
        try {
          await getQuota();
        } catch (error) {
          toastr.error(error);
        }
      }

      hideLoader();
    })();
  }, []);

  const chartData = {
    labels: visits.map((item) => moment(item.date).format("D MMM")),
    datasets: [
      {
        label: t("Visits"),
        data: visits.map((item) => item.hits),
      },
    ],
  };

  return tReady ? (
    <MainContainer className="not-selectable">
      <Storage quota={quota} t={t} />
      <div className="category-item-wrapper">
        <div className="category-item-heading">
          <Text className="inherit-title-link header" truncate={true}>
            {t("VisitsGraph")}
          </Text>
        </div>
        <Users quota={quota} t={t} />
        <VisitsSelectors t={t} i18n={i18n} getVisits={getVisits} />
        <Chart className="visits-chart" data={chartData} />
      </div>
    </MainContainer>
  ) : (
    <Loader className="pageLoader" type="rombs" size="40px" />
  );
};

Statistics.propTypes = {
  t: PropTypes.func,
  i18n: PropTypes.object,
};

export default inject(({ setup }) => {
  const { getQuota, getVisits } = setup;
  const { quota, visits } = setup.statistic;

  return {
    quota,
    visits,
    getQuota,
    getVisits,
  };
})(withTranslation("Settings")(observer(Statistics)));
