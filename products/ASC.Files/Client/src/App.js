import React, { Suspense } from "react";
import { connect } from "react-redux";
import { Router, Switch, Redirect, Route } from "react-router-dom";
import Home from "./components/pages/Home";
import DocEditor from "./components/pages/DocEditor";
import Settings from "./components/pages/Settings";
import VersionHistory from "./components/pages/VersionHistory";
import { fetchTreeFolders } from "./store/files/actions";
import config from "../package.json";

import {
  store as commonStore,
  history,
  PrivateRoute,
  PublicRoute,
  Login,
  Error404,
  Error520,
  Offline,
  NavMenu,
  Main,
  utils,
  toastr,
  Layout,
  ScrollToTop,
  regDesktop,
} from "asc-web-common";

const {
  setIsLoaded,
  getUser,
  getPortalSettings,
  getModules,
  setCurrentProductId,
  setCurrentProductHomePage,
  getPortalCultures,
  setEncryptionKeys,
  getIsEncryptionSupport,
  getEncryptionKeys,
  getIsAuthenticated,
} = commonStore.auth.actions;
const {
  getCurrentUser,
  isEncryptionSupport,
  isDesktopClient,
  getIsLoaded,
} = commonStore.auth.selectors;

class App extends React.Component {
  constructor(props) {
    super(props);

    const pathname = window.location.pathname.toLowerCase();
    this.isEditor = pathname.indexOf("doceditor") !== -1;
    this.isDesktopInit = false;
  }

  componentDidMount() {
    const {
      setModuleInfo,
      getUser,
      getPortalSettings,
      getModules,
      getPortalCultures,
      fetchTreeFolders,
      setIsLoaded,
      getIsEncryptionSupport,
      getEncryptionKeys,
      isDesktop,
      getIsAuthenticated,
    } = this.props;

    setModuleInfo();

    if (this.isEditor) {
      setIsLoaded();
      return;
    }

    getIsAuthenticated().then((isAuthenticated) => {
      if (!isAuthenticated) {
        utils.updateTempContent();
        return setIsLoaded();
      } else {
        utils.updateTempContent(isAuthenticated);
      }

      const requests = [getUser()];
      if (!this.isEditor) {
        requests.push(
          getPortalSettings(),
          getModules(),
          getPortalCultures(),
          fetchTreeFolders()
        );
        if (isDesktop) {
          requests.push(getIsEncryptionSupport(), getEncryptionKeys());
        }
      }

      Promise.all(requests)
        .then(() => {
          if (this.isEditor) return Promise.resolve();
        })
        .catch((e) => {
          toastr.error(e);
        })
        .finally(() => {
          utils.updateTempContent();
          setIsLoaded();
        });
    });
  }

  componentDidUpdate(prevProps) {
    const {
      isAuthenticated,
      user,
      isEncryption,
      encryptionKeys,
      setEncryptionKeys,
      isLoaded,
    } = this.props;
    //console.log("componentDidUpdate: ", this.props);
    if (isAuthenticated && !this.isDesktopInit && isEncryption && isLoaded) {
      this.isDesktopInit = true;
      regDesktop(
        user,
        isEncryption,
        encryptionKeys,
        setEncryptionKeys,
        this.isEditor
      );
      console.log(
        "%c%s",
        "color: green; font: 1.2em bold;",
        "Current keys is: ",
        encryptionKeys
      );
    }
  }

  render() {
    const { homepage, isDesktop } = this.props;
    //console.log(Layout);

    return navigator.onLine ? (
      <Layout>
        <Router history={history}>
          <ScrollToTop />
          {!this.isEditor && <NavMenu />}
          <Main isDesktop={isDesktop}>
            <Suspense fallback={null}>
              <Switch>
                <Redirect exact from="/" to={`${homepage}`} />
                <PrivateRoute
                  exact
                  path={`${homepage}/settings/:setting`}
                  component={Settings}
                />
                <Route
                  exact
                  path={[
                    `${homepage}/doceditor`,
                    `/Products/Files/DocEditor.aspx`,
                  ]}
                  component={DocEditor}
                />
                <PrivateRoute
                  exact
                  path={`${homepage}/:fileId/history`}
                  component={VersionHistory}
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
    user: getCurrentUser(state),
    isAuthenticated: state.auth.isAuthenticated,
    isLoaded: getIsLoaded(state),
    isEncryption: isEncryptionSupport(state),
    isDesktop: isDesktopClient(state),
    encryptionKeys: settings.encryptionKeys,
  };
};

const mapDispatchToProps = (dispatch) => {
  return {
    getIsAuthenticated: () => getIsAuthenticated(dispatch),
    setModuleInfo: () => {
      dispatch(setCurrentProductHomePage(config.homepage));
      dispatch(setCurrentProductId("e67be73d-f9ae-4ce1-8fec-1880cb518cb4"));
    },
    getUser: () => getUser(dispatch),
    getPortalSettings: () => getPortalSettings(dispatch),
    getModules: () => getModules(dispatch),
    getPortalCultures: () => getPortalCultures(dispatch),
    fetchTreeFolders: () => dispatch(fetchTreeFolders()),
    setIsLoaded: () => dispatch(setIsLoaded(true)),
    getIsEncryptionSupport: () => getIsEncryptionSupport(dispatch),
    getEncryptionKeys: () => getEncryptionKeys(dispatch),
    setEncryptionKeys: (keys) => dispatch(setEncryptionKeys(keys)),
  };
};

export default connect(mapStateToProps, mapDispatchToProps)(App);
