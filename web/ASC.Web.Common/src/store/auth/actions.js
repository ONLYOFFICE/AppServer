import { default as api } from "../../api";
import { isDesktopClient } from "./selectors";
import { logout as logoutDesktop } from "../../desktop/";
import { setWithCredentialsStatus } from "../../api/client";
import history from "../../history";
import { ARTICLE_PINNED_KEY } from "../../constants";

export const LOGIN_POST = "LOGIN_POST";
export const SET_CURRENT_USER = "SET_CURRENT_USER";
export const SET_MODULES = "SET_MODULES";
export const SET_SETTINGS = "SET_SETTINGS";
export const SET_IS_LOADED = "SET_IS_LOADED";
export const SET_IS_LOADED_SECTION = "SET_IS_LOADED_SECTION";
export const LOGOUT = "LOGOUT";
export const SET_PASSWORD_SETTINGS = "SET_PASSWORD_SETTINGS";
export const SET_NEW_EMAIL = "SET_NEW_EMAIL";
export const SET_PORTAL_CULTURES = "SET_PORTAL_CULTURES";
export const SET_PORTAL_LANGUAGE_AND_TIME = "SET_PORTAL_LANGUAGE_AND_TIME";
export const SET_TIMEZONES = "SET_TIMEZONES";
export const SET_CURRENT_PRODUCT_ID = "SET_CURRENT_PRODUCT_ID";
export const SET_CURRENT_PRODUCT_HOME_PAGE = "SET_CURRENT_PRODUCT_HOME_PAGE";
export const SET_GREETING_SETTINGS = "SET_GREETING_SETTINGS";
export const SET_CUSTOM_NAMES = "SET_CUSTOM_NAMES";
export const SET_WIZARD_COMPLETED = "SET_WIZARD_COMPLETED";
export const SET_HEADER_VISIBLE = "SET_HEADER_VISIBLE";
export const FETCH_ENCRYPTION_KEYS = "FETCH_ENCRYPTION_KEYS";
export const SET_IS_ENCRYPTION_SUPPORT = "SET_IS_ENCRYPTION_SUPPORT";
export const SET_IS_AUTHENTICATED = "SET_IS_AUTHENTICATED";
export const SET_IS_TABLET_VIEW = "SET_IS_TABLET_VIEW";
export const SET_ARTICLE_PINNED = "SET_ARTICLE_PINNED";

export function setCurrentUser(user) {
  return {
    type: SET_CURRENT_USER,
    user,
  };
}

export function setModules(modules) {
  return {
    type: SET_MODULES,
    modules,
  };
}

export function setSettings(settings) {
  return {
    type: SET_SETTINGS,
    settings,
  };
}

export function setIsLoaded(isLoaded) {
  return {
    type: SET_IS_LOADED,
    isLoaded,
  };
}

export function setIsLoadedSection(isLoadedSection) {
  return {
    type: SET_IS_LOADED_SECTION,
    isLoadedSection,
  };
}

export function setLogout() {
  return {
    type: LOGOUT,
  };
}

export function setPasswordSettings(passwordSettings) {
  return {
    type: SET_PASSWORD_SETTINGS,
    passwordSettings,
  };
}

export function setNewEmail(email) {
  return {
    type: SET_NEW_EMAIL,
    email,
  };
}

export function setPortalCultures(cultures) {
  return {
    type: SET_PORTAL_CULTURES,
    cultures,
  };
}

export function setPortalLanguageAndTime(newSettings) {
  return {
    type: SET_PORTAL_LANGUAGE_AND_TIME,
    newSettings,
  };
}

export function setTimezones(timezones) {
  return {
    type: SET_TIMEZONES,
    timezones,
  };
}

export function setCurrentProductId(currentProductId) {
  return {
    type: SET_CURRENT_PRODUCT_ID,
    currentProductId,
  };
}

export function setCurrentProductHomePage(homepage) {
  return {
    type: SET_CURRENT_PRODUCT_HOME_PAGE,
    homepage,
  };
}

export function setGreetingSettings(title) {
  return {
    type: SET_GREETING_SETTINGS,
    title,
  };
}

export function setCustomNames(customNames) {
  return {
    type: SET_CUSTOM_NAMES,
    customNames,
  };
}

export function setWizardComplete() {
  return {
    type: SET_WIZARD_COMPLETED,
  };
}

export function fetchEncryptionKeys(keys) {
  return {
    type: FETCH_ENCRYPTION_KEYS,
    keys,
  };
}

export function setIsEncryptionSupport(isSupport) {
  return {
    type: SET_IS_ENCRYPTION_SUPPORT,
    isSupport,
  };
}

export function setIsAuthenticated(isAuthenticated) {
  return {
    type: SET_IS_AUTHENTICATED,
    isAuthenticated,
  };
}

export function setIsTabletView(isTabletView) {
  return {
    type: SET_IS_TABLET_VIEW,
    isTabletView,
  };
}

export function getUser(dispatch) {
  return api.people
    .getUser()
    .then((user) => dispatch(setCurrentUser(user)))
    .then(() => dispatch(setIsAuthenticated(true)))
    .catch((err) => dispatch(setCurrentUser({})));
}

export function getIsAuthenticated(dispatch) {
  return api.user.checkIsAuthenticated().then((success) => {
    dispatch(setIsAuthenticated(success));
    return success;
  });
}

export function getPortalSettings(dispatch) {
  return api.settings.getSettings().then((settings) => {
    const { passwordHash: hashSettings, ...otherSettings } = settings;
    const logoSettings = { logoUrl: "images/nav.logo.opened.react.svg" };
    dispatch(
      setSettings(
        hashSettings
          ? { ...logoSettings, ...otherSettings, hashSettings }
          : { ...logoSettings, ...otherSettings }
      )
    );

    otherSettings.nameSchemaId &&
      getCurrentCustomSchema(dispatch, otherSettings.nameSchemaId);
  });
}
export function getCurrentCustomSchema(dispatch, id) {
  return api.settings
    .getCurrentCustomSchema(id)
    .then((customNames) => dispatch(setCustomNames(customNames)));
}

export function getModules(dispatch) {
  return api.modules
    .getModulesList()
    .then((modules) => dispatch(setModules(modules)));
}

export const loadInitInfo = (dispatch) => {
  return getPortalSettings(dispatch).then(() => getModules(dispatch));
};

export function getUserInfo(dispatch) {
  return getUser(dispatch).finally(() => loadInitInfo(dispatch));
}

export function login(user, hash) {
  return (dispatch) => {
    return api.user
      .login(user, hash)
      .then(() => dispatch(setIsLoaded(false)))
      .then(() => {
        setWithCredentialsStatus(true);
        return dispatch(setIsAuthenticated(true));
      })
      .then(() => {
        getUserInfo(dispatch);
        getEncryptionKeys(dispatch);
      });
  };
}

export function logout(withoutRedirect) {
  return (dispatch, getState) => {
    const state = getState();
    const isDesktop = isDesktopClient(state);
    return api.user.logout().then(() => {
      setWithCredentialsStatus(false);
      isDesktop && logoutDesktop();
      dispatch(setLogout());
      if (!withoutRedirect) {
        history.push("/login");
      }
    });
  };
}

export function getPortalCultures(dispatch = null) {
  return dispatch
    ? api.settings.getPortalCultures().then((cultures) => {
        dispatch(setPortalCultures(cultures));
      })
    : (dispatch) => {
        return api.settings.getPortalCultures().then((cultures) => {
          dispatch(setPortalCultures(cultures));
        });
      };
}

export function getPortalPasswordSettings(dispatch, confirmKey = null) {
  return api.settings.getPortalPasswordSettings(confirmKey).then((settings) => {
    dispatch(setPasswordSettings(settings));
  });
}

export const reloadPortalSettings = () => {
  return (dispatch) => getPortalSettings(dispatch);
};
export function setHeaderVisible(isHeaderVisible) {
  return {
    type: SET_HEADER_VISIBLE,
    isHeaderVisible,
  };
}

export function setEncryptionKeys(keys) {
  return (dispatch) => {
    return api.files
      .setEncryptionKeys(keys)
      .then(() => {
        console.log(
          "%c%s",
          "color: green; font: 1.1em/1 bold;",
          "Encryption keys successfully set"
        );
        dispatch(fetchEncryptionKeys(keys ?? {}));
      })
      .catch((err) => console.error(err));
  };
}

export function getEncryptionKeys(dispatch) {
  return api.files
    .getEncryptionKeys()
    .then((res) => dispatch(fetchEncryptionKeys(res ?? {})))
    .catch((err) => console.error(err));
}

export function getEncryptionAccess(fileId) {
  return api.files
    .getEncryptionAccess(fileId)
    .then((keys) => {
      return Promise.resolve(keys);
    })
    .catch((err) => console.error(err));
}

export function getIsEncryptionSupport(dispatch) {
  return api.files
    .getIsEncryptionSupport()
    .then((res) => dispatch(setIsEncryptionSupport(res)))
    .catch((err) => console.error(err));
}

export function replaceFileStream(fileId, file, encrypted, forcesave) {
  return (dispatch) => {
    return api.files.updateFileStream(file, fileId, encrypted, forcesave);
  };
}

export function setArticlePinned(isPinned) {
  isPinned
    ? localStorage.setItem(ARTICLE_PINNED_KEY, isPinned)
    : localStorage.removeItem(ARTICLE_PINNED_KEY);
  return {
    type: SET_ARTICLE_PINNED,
    isPinned,
  };
}
