import React, { Suspense } from "react";
import { connect } from "react-redux";
import { Router, Switch, Redirect } from "react-router-dom";
import Home from "./components/pages/Home";
import Profile from "./components/pages/Profile";
import ProfileAction from "./components/pages/ProfileAction";
import GroupAction from "./components/pages/GroupAction";
import Reassign from "./components/pages/Reassign";

import {
  history,
  PrivateRoute,
  PublicRoute,
  Login,
  Error404,
  Error520,
  Offline,
  utils,
  store as commonStore,
  NavMenu,
  Main,
  toastr,
  Layout,
  ScrollToTop,
} from "asc-web-common";
import { getFilterByLocation } from "./helpers/converters";
import { fetchGroups, fetchPeople } from "./store/people/actions";
import config from "../package.json";

const {
  setIsLoaded,
  getUser,
  getPortalSettings,
  getModules,
  setCurrentProductId,
  setCurrentProductHomePage,
  getPortalPasswordSettings,
  getPortalCultures,
  getIsAuthenticated,
} = commonStore.auth.actions;

/*const Profile = lazy(() => import("./components/pages/Profile"));
const ProfileAction = lazy(() => import("./components/pages/ProfileAction"));
const GroupAction = lazy(() => import("./components/pages/GroupAction"));*/

class App extends React.Component {
  componentDidMount() {
    const {
      setModuleInfo,
      getUser,
      getPortalSettings,
      getModules,
      getPortalPasswordSettings,
      getPortalCultures,
      fetchGroups,
      fetchPeople,
      setIsLoaded,
      getIsAuthenticated,
    } = this.props;

    setModuleInfo();
    getIsAuthenticated().then((isAuthenticated) => {
      if (!isAuthenticated) {
        utils.updateTempContent();
        return setIsLoaded();
      } else {
        utils.updateTempContent(isAuthenticated);
      }

      const requests = [
        getUser(),
        getPortalSettings(),
        getModules(),
        getPortalPasswordSettings(),
        getPortalCultures(),
        fetchGroups(),
        fetchPeople(),
      ];

      Promise.all(requests)
        .catch((e) => {
          toastr.error(e);
        })
        .finally(() => {
          utils.updateTempContent();
          setIsLoaded();
        });
    });
  }

  render() {
    const { homepage } = this.props;

    console.log("People App render", this.props);
    return navigator.onLine ? (
      <Layout>
        <Router history={history}>
          <ScrollToTop />
          <NavMenu />
          <Main>
            <Suspense fallback={null}>
              <Switch>
                <Redirect exact from="/" to={`${homepage}`} />
                <PrivateRoute
                  exact
                  path={`${homepage}/view/:userId`}
                  component={Profile}
                />
                <PrivateRoute
                  path={`${homepage}/edit/:userId`}
                  restricted
                  allowForMe
                  component={ProfileAction}
                />
                <PrivateRoute
                  path={`${homepage}/create/:type`}
                  restricted
                  component={ProfileAction}
                />
                <PrivateRoute
                  path={[
                    `${homepage}/group/edit/:groupId`,
                    `${homepage}/group/create`,
                  ]}
                  restricted
                  component={GroupAction}
                />
                <PrivateRoute
                  path={`${homepage}/reassign/:userId`}
                  restricted
                  component={Reassign}
                />
                <PrivateRoute exact path={homepage} component={Home} />
                <PrivateRoute path={`${homepage}/filter`} component={Home} />
                <PublicRoute
                  exact
                  path={[
                    "/login",
                    "/login/error=:error",
                    "/login/confirmed-email=:confirmedEmail",
                  ]}
                  component={Login}
                />
                <PrivateRoute
                  exact
                  path={`/error=:error`}
                  component={Error520}
                />
                <PrivateRoute component={Error404} />
              </Switch>
            </Suspense>
          </Main>
        </Router>
      </Layout>
    ) : (
      <Offline />
    );
  }
}

const mapStateToProps = (state) => {
  const { settings } = state.auth;
  const { homepage } = settings;
  return {
    homepage: homepage || config.homepage,
  };
};

const mapDispatchToProps = (dispatch) => {
  return {
    getIsAuthenticated: () => getIsAuthenticated(dispatch),
    setModuleInfo: () => {
      dispatch(setCurrentProductHomePage(config.homepage));
      dispatch(setCurrentProductId("f4d98afd-d336-4332-8778-3c6945c81ea0"));
    },
    getUser: () => getUser(dispatch),
    getPortalSettings: () => getPortalSettings(dispatch),
    getModules: () => getModules(dispatch),
    getPortalPasswordSettings: () => getPortalPasswordSettings(dispatch),
    getPortalCultures: () => getPortalCultures(dispatch),
    fetchGroups: () => fetchGroups(dispatch),
    fetchPeople: () => {
      var re = new RegExp(`${config.homepage}((/?)$|/filter)`, "gm");
      const match = window.location.pathname.match(re);

      if (match && match.length > 0) {
        const newFilter = getFilterByLocation(window.location);
        return fetchPeople(newFilter, dispatch);
      }

      return Promise.resolve();
    },
    setIsLoaded: () => dispatch(setIsLoaded(true)),
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(App);
