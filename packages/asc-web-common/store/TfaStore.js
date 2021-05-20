import { makeAutoObservable } from "mobx";
import api from "../api";
import history from "../history";

class TfaStore {
  tfaSettings = null;
  tfaAndroidAppUrl =
    "https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2";
  tfaIosAppUrl = "https://apps.apple.com/app/google-authenticator/id388497605";
  tfaWinAppUrl =
    "https://www.microsoft.com/ru-ru/p/authenticator/9wzdncrfj3rj?rtc=1&activetab=pivot:overviewtab";

  constructor() {
    makeAutoObservable(this);
  }

  getTfaSettings = async () => {
    const res = await api.settings.getTfaSettings();
    const sms = res[0].enabled;
    const app = res[1].enabled;

    const type = sms ? "sms" : app ? "app" : "none";
    this.tfaSettings = type;

    return type;
  };

  setTfaSettings = async (type) => {
    return await api.settings.setTfaSettings(type);
  };

  getTfaConfirmLink = async (res, type) => {
    if (res && type !== "none") {
      return await api.settings.getTfaConfirmLink();
    }
  };

  getSecretKeyAndQR = async (confirmKey) => {
    return api.settings.getTfaSecretKeyAndQR(confirmKey);
  };

  loginWithCode = async (userName, passwordHash, code) => {
    return api.user.loginWithTfaCode(userName, passwordHash, code);
  };

  loginWithCodeAndCookie = async (code) => {
    return api.settings.loginWithTfaCodeAndCookie(code);
  };

  getBackupCodes = async () => {
    return api.settings.getTfaBackupCodes();
  };

  getNewBackupCodes = async () => {
    return api.settings.getTfaNewBackupCodes();
  };

  unlinkApp = async () => {
    return api.settings.unlinkTfaApp();
  };
}

export default TfaStore;
