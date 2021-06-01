import React, { lazy, Suspense } from "react";
import { Route, Switch } from "react-router-dom";
import { withRouter } from "react-router";
import Loader from "@appserver/components/loader";
import { combineUrl } from "@appserver/common/utils";
import AppServerConfig from "@appserver/common/constants/AppServerConfig";

const StatisticsPage = lazy(() => import("./statistics"));

const PROXY_STATISTICS_URL = combineUrl(
  AppServerConfig.proxyURL,
  "/settings/statistics"
);

const Statistics = ({ match }) => {
  return (
    <Suspense
      fallback={<Loader className="pageLoader" type="rombs" size="40px" />}
    >
      <Switch>
        <Route exact path={PROXY_STATISTICS_URL} component={StatisticsPage} />
      </Switch>
    </Suspense>
  );
};

export default withRouter(Statistics);
