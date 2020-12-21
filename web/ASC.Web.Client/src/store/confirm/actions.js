import { api, store } from "asc-web-common";
const {
  setCurrentUser,
  loadInitInfo,
  login,
  getPortalPasswordSettings,
  setNewEmail,
  logout,
  getPortalSettings,
} = store.auth.actions;

export const SET_IS_CONFIRM_LOADED = "SET_IS_CONFIRM_LOADED";

export function setIsConfirmLoaded(isConfirmLoaded) {
  return {
    type: SET_IS_CONFIRM_LOADED,
    isConfirmLoaded,
  };
}

export function getConfirmationInfo(token) {
  return (dispatch) => {
    const requests = [
      getPortalSettings(dispatch),
      getPortalPasswordSettings(dispatch, token),
    ];

    return Promise.all(requests).then(() => dispatch(setIsConfirmLoaded(true)));
  };
}

export function createConfirmUser(registerData, loginData, key) {
  const data = Object.assign({}, registerData, loginData);
  return (dispatch) => {
    return api.people
      .createUser(data, key)
      .then((user) => {
        dispatch(setCurrentUser(user));
      })
      .then(() => {
        const promise = new Promise((resolve, reject) => {
          setTimeout(() => {
            login(
              loginData.userName,
              loginData.passwordHash
            )(dispatch)
              .then(() => {
                resolve(loadInitInfo(dispatch));
              })
              .catch((e) => {
                reject(e);
              });
          }, 1000);
        });

        return promise;
      });
  };
}

export function activateConfirmUser(
  personalData,
  loginData,
  key,
  userId,
  activationStatus
) {
  const changedData = {
    id: userId,
    FirstName: personalData.firstname,
    LastName: personalData.lastname,
  };

  return (dispatch) => {
    return api.people
      .changePassword(userId, loginData.passwordHash, key)
      .then((data) => {
        return api.people.updateActivationStatus(activationStatus, userId, key);
      })
      .then((data) => {
        const promise = new Promise((resolve, reject) => {
          setTimeout(() => {
            login(
              data.userName,
              data.passwordHash
            )(dispatch)
              .then(() => {
                resolve(loadInitInfo(dispatch));
              })
              .catch((e) => {
                reject(e);
              });
          }, 1000);
        });

        return promise;
      })
      .then((data) => {
        return api.people.updateUser(changedData);
      })
      .then((user) => dispatch(setCurrentUser(user)));
  };
}

export function changeEmail(userId, email, key) {
  return (dispatch) => {
    return api.people
      .changeEmail(userId, email, key)
      .then((user) => dispatch(setNewEmail(user.email)));
  };
}

export function changePassword(userId, passwordHash, key) {
  return (dispatch) => {
    return api.people
      .changePassword(userId, passwordHash, key)
      .then(() => logout(dispatch));
  };
}
