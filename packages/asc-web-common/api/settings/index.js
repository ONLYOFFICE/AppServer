import { request } from "../client";

export function getSettings() {
  return request({
    method: "get",
    url: "/settings.json",
  });
}

export function getPortalCultures() {
  return request({
    method: "get",
    url: "/settings/cultures.json",
  });
}

export function getPortalPasswordSettings(confirmKey = null) {
  const options = {
    method: "get",
    url: "/settings/security/password",
  };

  if (confirmKey) options.headers = { confirm: confirmKey };

  return request(options);
}

export function getPortalTimezones(confirmKey = null) {
  const options = {
    method: "get",
    url: "/settings/timezones.json",
  };

  if (confirmKey) options.headers = { confirm: confirmKey };

  return request(options);
}

export function setLanguageAndTime(lng, timeZoneID) {
  return request({
    method: "put",
    url: "/settings/timeandlanguage.json",
    data: { lng, timeZoneID },
  });
}

export function setGreetingSettings(title) {
  return request({
    method: "post",
    url: `/settings/greetingsettings.json`,
    data: { title },
  });
}

export function restoreGreetingSettings() {
  return request({
    method: "post",
    url: `/settings/greetingsettings/restore.json`,
  });
}

export function getLogoText() {
  return request({
    method: "get",
    url: `/settings/whitelabel/logotext.json`,
  });
}
export function getLogoSizes() {
  return request({
    method: "get",
    url: `/settings/whitelabel/sizes.json`,
  });
}

export function getLogoUrls() {
  return request({
    method: "get",
    url: `/settings/whitelabel/logos.json`,
  });
}

export function getCustomSchemaList() {
  return request({
    method: "get",
    url: `settings/customschemas`,
  });
}

export function setCurrentSchema(id) {
  return request({
    method: "post",
    url: "settings/customschemas",
    data: { id },
  });
}
export function setCustomSchema(
  userCaption,
  usersCaption,
  groupCaption,
  groupsCaption,
  userPostCaption,
  regDateCaption,
  groupHeadCaption,
  guestCaption,
  guestsCaption
) {
  const data = {
    userCaption,
    usersCaption,
    groupCaption,
    groupsCaption,
    userPostCaption,
    regDateCaption,
    groupHeadCaption,
    guestCaption,
    guestsCaption,
  };
  return request({
    method: "put",
    url: `settings/customschemas`,
    data,
  });
}

export function getCurrentCustomSchema(id) {
  return request({
    method: "get",
    url: `settings/customschemas/${id}.json`,
  });
}

export function sendRecoverRequest(email, message) {
  const data = { email, message };
  return request({
    method: "post",
    url: `/settings/sendadmmail`,
    data,
  });
}

export function sendRegisterRequest(email) {
  const data = { email };
  return request({
    method: "post",
    url: `/settings/sendjoininvite`,
    data,
  });
}

export function sendOwnerChange(ownerId) {
  const data = { ownerId };
  return request({
    method: "post",
    url: `/settings/owner.json`,
    data,
  });
}

export function getMachineName(confirmKey = null) {
  const options = {
    method: "get",
    url: "/settings/machine.json",
  };

  if (confirmKey) options.headers = { confirm: confirmKey };

  return request(options);
}

export function setPortalOwner(
  email,
  hash,
  lng,
  timeZone,
  confirmKey = null,
  analytics
) {
  const options = {
    method: "put",
    url: "/settings/wizard/complete.json",
    data: {
      email: email,
      PasswordHash: hash,
      lng: lng,
      timeZone: timeZone,
      analytics: analytics,
    },
  };

  if (confirmKey) {
    options.headers = { confirm: confirmKey };
  }
  return request(options);
}

export function getIsLicenseRequired() {
  return request({
    method: "get",
    url: "/settings/license/required.json",
  });
}

export function setLicense(confirmKey = null, data) {
  const options = {
    method: "post",
    url: `/settings/license`,
    data,
  };

  if (confirmKey) {
    options.headers = { confirm: confirmKey };
  }

  return request(options);
}

export function getPaymentSettings() {
  return request({
    method: "get",
    url: `/settings/payment.json`,
  });
}
export function acceptLicense() {
  return request({
    method: "post",
    url: `/settings/license/accept.json`,
  });
}
export function getConsumersList() {
  return request({
    method: "get",
    url: `/settings/authservice`,
  });
}

export function getAuthProviders() {
  return request({
    method: "get",
    url: `/people/thirdparty/providers`,
  });
}

export function updateConsumerProps(newProps) {
  const options = {
    method: "post",
    url: `/settings/authservice`,
    data: newProps,
  };

  return request(options);
}

export function getPortalQuota() {
  return request({
    method: "get",
    url: `/settings/quota`,
  });
}

export function getPortalVisits(fromDate, toDate) {
  const options = {
    method: "get",
    url: `/settings/statistics/visit?fromDate=${fromDate}&toDate=${toDate}`,
  };
  return request(options);
}
