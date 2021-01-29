import {
  SET_CURRENT_USER,
  SET_MODULES,
  SET_SETTINGS,
  SET_IS_LOADED,
  SET_IS_LOADED_SECTION,
  LOGOUT,
  SET_PASSWORD_SETTINGS,
  SET_NEW_EMAIL,
  SET_PORTAL_CULTURES,
  SET_PORTAL_LANGUAGE_AND_TIME,
  SET_TIMEZONES,
  SET_CURRENT_PRODUCT_ID,
  SET_CURRENT_PRODUCT_HOME_PAGE,
  SET_GREETING_SETTINGS,
  SET_CUSTOM_NAMES,
  SET_WIZARD_COMPLETED,
  SET_HEADER_VISIBLE,
  FETCH_ENCRYPTION_KEYS,
  SET_IS_ENCRYPTION_SUPPORT,
  SET_IS_AUTHENTICATED,
  SET_IS_TABLET_VIEW,
  SET_ARTICLE_PINNED,
} from "./actions";
import {
  LANGUAGE,
  ARTICLE_PINNED_KEY,
  AUTH_KEY,
  HEADER_VISIBLE_KEY,
} from "../../constants";

const desktop = window["AscDesktopEditor"] !== undefined;
const desktopEncryption =
  desktop && typeof window.AscDesktopEditor.cloudCryptoCommand === "function";
const lang = localStorage["language"]
  ? localStorage
      .getItem("language")
      .split("-")
      .find((el) => el[0])
  : "en";

const initialState = {
  isAuthenticated: false,
  isLoaded: false,
  isLoadedSection: true,

  user: {},
  modules: [],
  settings: {
    currentProductId: "",
    culture: "en-US",
    cultures: [],
    trustedDomains: [],
    trustedDomainsType: 1,
    timezone: "UTC",
    timezones: [],
    utcOffset: "00:00:00",
    utcHoursOffset: 0,
    defaultPage: "/products/files",
    homepage: "", //config.homepage,
    datePattern: "M/d/yyyy",
    datePatternJQ: "00/00/0000",
    dateTimePattern: "dddd, MMMM d, yyyy h:mm:ss tt",
    datepicker: {
      datePattern: "mm/dd/yy",
      dateTimePattern: "DD, mm dd, yy h:mm:ss tt",
      timePattern: "h:mm tt",
    },
    organizationName: "ONLYOFFICE",
    greetingSettings: "Web Office Applications",
    enableAdmMess: false,
    urlLicense: "https://gnu.org/licenses/gpl-3.0.html",
    urlSupport: "https://helpdesk.onlyoffice.com/",
    urlAuthKeys: `https://helpcenter.onlyoffice.com/${lang}/installation/groups-authorization-keys.aspx`,
    logoUrl: "",
    customNames: {
      id: "Common",
      userCaption: "User",
      usersCaption: "Users",
      groupCaption: "Group",
      groupsCaption: "Groups",
      userPostCaption: "Title",
      regDateCaption: "Registration Date",
      groupHeadCaption: "Head",
      guestCaption: "Guest",
      guestsCaption: "Guests",
    },
    isDesktopClient: desktop,
    //isDesktopEncryption: desktopEncryption,
    isEncryptionSupport: false,
    encryptionKeys: null,

    isHeaderVisible: false,
    isTabletView: false,
    isArticlePinned: localStorage.getItem(ARTICLE_PINNED_KEY) || false,
  },
};

const authReducer = (state = initialState, action) => {
  switch (action.type) {
    case SET_CURRENT_USER:
      action.user.cultureName &&
        localStorage.getItem(LANGUAGE) !== action.user.cultureName &&
        localStorage.setItem(LANGUAGE, action.user.cultureName);
      return Object.assign({}, state, {
        user: action.user,
      });
    case SET_IS_AUTHENTICATED:
      return Object.assign({}, state, {
        isAuthenticated: action.isAuthenticated,
      });
    case SET_MODULES:
      return Object.assign({}, state, {
        modules: action.modules,
      });
    case SET_SETTINGS:
      if (!localStorage.getItem(LANGUAGE)) {
        localStorage.setItem(LANGUAGE, action.settings.culture);
      }
      return Object.assign({}, state, {
        settings: {
          ...state.settings,
          ...action.settings,
        },
      });
    case SET_PORTAL_CULTURES:
      return Object.assign({}, state, {
        settings: { ...state.settings, cultures: action.cultures },
      });
    case SET_PASSWORD_SETTINGS:
      return Object.assign({}, state, {
        settings: {
          ...state.settings,
          passwordSettings: action.passwordSettings,
        },
      });
    case SET_IS_LOADED:
      return Object.assign({}, state, {
        isLoaded: action.isLoaded,
      });
    case SET_IS_LOADED_SECTION:
      return Object.assign({}, state, {
        isLoadedSection: action.isLoadedSection,
      });
    case SET_NEW_EMAIL:
      return Object.assign({}, state, {
        user: { ...state.user, email: action.email },
      });
    case SET_PORTAL_LANGUAGE_AND_TIME:
      if (!state.user.cultureName) {
        localStorage.setItem(LANGUAGE, action.newSettings.lng);
      }
      return Object.assign({}, state, {
        settings: {
          ...state.settings,
          culture: action.newSettings.lng,
          timezone: action.newSettings.timeZoneID,
        },
      });
    case SET_TIMEZONES:
      return Object.assign({}, state, {
        settings: { ...state.settings, timezones: action.timezones },
      });
    case SET_CURRENT_PRODUCT_ID:
      return Object.assign({}, state, {
        settings: {
          ...state.settings,
          currentProductId: action.currentProductId,
        },
      });
    case SET_CURRENT_PRODUCT_HOME_PAGE:
      return Object.assign({}, state, {
        settings: { ...state.settings, homepage: action.homepage },
      });
    case SET_GREETING_SETTINGS:
      return Object.assign({}, state, {
        settings: { ...state.settings, greetingSettings: action.title },
      });
    case SET_CUSTOM_NAMES:
      return Object.assign({}, state, {
        settings: { ...state.settings, customNames: action.customNames },
      });
    case LOGOUT:
      return Object.assign({}, initialState, {
        isLoaded: true,
        settings: state.settings,
      });
    case SET_WIZARD_COMPLETED:
      return Object.assign({}, state, {
        settings: { ...state.settings, wizardCompleted: true },
      });
    case FETCH_ENCRYPTION_KEYS:
      return {
        ...state,
        settings: {
          ...state.settings,
          encryptionKeys: action.keys,
        },
      };
    case SET_IS_ENCRYPTION_SUPPORT:
      return {
        ...state,
        settings: {
          ...state.settings,
          isEncryptionSupport: action.isSupport,
          //isEncryptionSupport: state.isDesktopEncryption && action.isSupport,
        },
      };

    case SET_HEADER_VISIBLE:
      return Object.assign({}, state, {
        settings: {
          ...state.settings,
          isHeaderVisible: action.isHeaderVisible,
        },
      });
    case SET_IS_TABLET_VIEW:
      return Object.assign({}, state, {
        settings: {
          ...state.settings,
          isTabletView: action.isTabletView,
        },
      });

    case SET_ARTICLE_PINNED:
      return Object.assign({}, state, {
        settings: { ...state.settings, isArticlePinned: action.isPinned },
      });
    default:
      return state;
  }
};

export default authReducer;
