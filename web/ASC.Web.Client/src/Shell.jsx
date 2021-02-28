import React, { useEffect } from "react";
import styled from "styled-components";
import { Router, Switch } from "react-router-dom";
import { inject, observer } from "mobx-react";
import NavMenu from "@appserver/common/components/NavMenu";
import Main from "@appserver/common/components/Main";
import Box from "@appserver/components/box";
import PrivateRoute from "@appserver/common/components/PrivateRoute";
import PublicRoute from "@appserver/common/components/PublicRoute";
import ErrorBoundary from "@appserver/common/components/ErrorBoundary";
import Layout from "@appserver/common/components/Layout";
import ScrollToTop from "@appserver/common/components/Layout/ScrollToTop";
import history from "@appserver/common/history";
import toastr from "@appserver/common/components/Toast";
import RectangleLoader from "@appserver/common/components/Loaders/RectangleLoader";
import { updateTempContent } from "@appserver/common/utils";
import { Provider as MobxProvider } from "mobx-react";
import ThemeProvider from "@appserver/components/theme-provider";
import { Base, Dark } from "@appserver/components/themes";
import store from "studio/store";
import config from "../package.json";
import "./custom.scss";
import "./i18n";

const Payments = React.lazy(() => import("./components/pages/Payments"));
const Error404 = React.lazy(() => import("@appserver/common/pages/errors/404"));
const Home = React.lazy(() => import("./components/pages/Home"));
const Login = React.lazy(() => import("login/app"));
const People = React.lazy(() => import("people/app"));
const Files = React.lazy(() => import("files/app"));
const About = React.lazy(() => import("./components/pages/About"));
const Settings = React.lazy(() => import("./components/pages/Settings"));
const ComingSoon = React.lazy(() => import("./components/pages/ComingSoon"));

const LoadingBody = styled.div`
  padding: 20px;
`;
const LoadingShell = () => (
  <LoadingBody>
    <RectangleLoader height="100%" />
  </LoadingBody>
);

const SettingsRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <Settings {...props} />
    </ErrorBoundary>
  </React.Suspense>
);
const PaymentsRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <Payments {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const Error404Route = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <Error404 {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const HomeRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <Home {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const LoginRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <Login {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const PeopleRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <People {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const FilesRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <Files {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const AboutRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <About {...props} />
    </ErrorBoundary>
  </React.Suspense>
);

const ComingSoonRoute = (props) => (
  <React.Suspense fallback={<LoadingShell />}>
    <ErrorBoundary>
      <ComingSoon {...props} />
    </ErrorBoundary>
  </React.Suspense>
);
const Shell = ({ items = [], page = "home", ...rest }) => {
  // useEffect(() => {
  //   //utils.removeTempContent();

  //   const {
  //     getPortalSettings,
  //     getUser,
  //     getModules,
  //     setIsLoaded,
  //     getIsAuthenticated,
  //   } = rest;

  //   getIsAuthenticated()
  //     .then((isAuthenticated) => {
  //       if (isAuthenticated) updateTempContent(isAuthenticated);

  //       const requests = [];
  //       if (!isAuthenticated) {
  //         requests.push(getPortalSettings());
  //       } else if (
  //         !window.location.pathname.includes("confirm/EmailActivation")
  //       ) {
  //         requests.push(getUser());
  //         requests.push(getPortalSettings());
  //         requests.push(getModules());
  //       }

  //       return Promise.all(requests).finally(() => {
  //         updateTempContent();
  //         setIsLoaded();
  //       });
  //     })
  //     .catch((err) => toastr.error(err.message));
  // }, []);

  const { isLoaded, loadBaseInfo, isThirdPartyResponse } = rest;

  useEffect(() => {
    try {
      loadBaseInfo();
    } catch (err) {
      toastr.error(err);
    }
  }, [loadBaseInfo]);

  useEffect(() => {
    console.log("App render", isLoaded);
    if (isLoaded) updateTempContent();
  }, [isLoaded]);

  return (
    <Layout>
      <Router history={history}>
        <Box>
          <NavMenu />
          <ScrollToTop />
          <Main>
            <Switch>
              <PrivateRoute
                exact
                path={["/", "/error=:error"]}
                component={HomeRoute}
              />
              <PrivateRoute
                path={["/products/people", "/products/people/filter"]}
                component={PeopleRoute}
              />
              <PrivateRoute
                path={["/products/files", "/products/files/filter"]}
                component={FilesRoute}
              />
              <PrivateRoute path={["/about"]} component={AboutRoute} />
              <PublicRoute
                exact
                path={[
                  "/login",
                  "/login/error=:error",
                  "/login/confirmed-email=:confirmedEmail",
                ]}
                component={LoginRoute}
              />
              <PrivateRoute
                exact
                path={[
                  "/coming-soon",
                  "/products/mail",
                  "/products/projects",
                  "/products/crm",
                  "/products/calendar",
                  "/products/talk/",
                ]}
                component={ComingSoonRoute}
              />
              <PrivateRoute path="/payments" component={PaymentsRoute} />
              <PrivateRoute
                restricted
                path="/settings"
                component={SettingsRoute}
              />
              <PrivateRoute component={Error404Route} />
            </Switch>
          </Main>
        </Box>
      </Router>
    </Layout>
  );
};

// const mapStateToProps = (state) => {
//   const { modules, isLoaded, settings } = state.auth;
//   const { organizationName } = settings;
//   return {
//     modules,
//     isLoaded,
//     organizationName,
//   };
// };

// const mapDispatchToProps = (dispatch) => {
//   return {
//     getIsAuthenticated: () => getIsAuthenticated(dispatch),
//     getPortalSettings: () => getPortalSettings(dispatch),
//     getUser: () => getUser(dispatch),
//     getModules: () => getModules(dispatch),
//     setIsLoaded: () => dispatch(setIsLoaded(true)),
//   };
// };

// export default connect(mapStateToProps, mapDispatchToProps)(Shell);

const ShellWrapper = inject(({ auth }) => {
  const { init, isLoaded } = auth;

  const pathname = window.location.pathname.toLowerCase();
  const isThirdPartyResponse = pathname.indexOf("thirdparty") !== -1;

  return {
    loadBaseInfo: () => {
      init();
      auth.setProductVersion(config.version);
    },
    isThirdPartyResponse,
    isLoaded,
  };
})(observer(Shell));

export default () => (
  <ThemeProvider theme={Base}>
    <MobxProvider {...store}>
      <ShellWrapper />
    </MobxProvider>
  </ThemeProvider>
);