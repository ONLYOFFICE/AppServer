import api from "@appserver/common/api";
import { makeAutoObservable } from "mobx";
const { Filter } = api;

class SettingsSetupStore {
  common = {
    whiteLabel: {
      logoSizes: [],
      logoText: null,
      logoUrls: [],
    },
  };

  security = {
    accessRight: {
      options: [],
      users: [],
      admins: [],
      owner: {},
      filter: Filter.getDefault(),
    },
  };

  integration = {
    consumers: [],
    selectedConsumer: {},
  };

  statistic = {
    quota: {},
    visits: [],
  };

  constructor() {
    makeAutoObservable(this);
  }

  setOptions = (options) => {
    this.security.accessRight.options = options;
  };

  setUsers = (users) => {
    this.security.accessRight.users = users;
  };

  setAdmins = (admins) => {
    this.security.accessRight.admins = admins;
  };

  setOwner = (owner) => {
    this.security.accessRight.owner = owner;
  };

  setFilter = (filter) => {
    this.security.accessRight.filter = filter;
  };

  setLogoText = (text) => {
    this.common.whiteLabel.logoText = text;
  };

  setLogoSizes = (sizes) => {
    this.common.whiteLabel.logoSizes = sizes;
  };

  setLogoUrls = (urls) => {
    this.common.whiteLabel.logoUrls = urls;
  };

  setConsumers = (consumers) => {
    this.integration.consumers = consumers;
  };

  setQuota = (quota) => {
    this.statistic.quota = quota;
  };

  setVisits = (visits) => {
    this.statistic.visits = visits;
  };

  setSelectedConsumer = (selectedConsumerName) => {
    this.integration.selectedConsumer =
      this.integration.consumers.find((c) => c.name === selectedConsumerName) ||
      {};
  };

  changeAdmins = async (userIds, productId, isAdmin, filter) => {
    let filterData = filter && filter.clone();
    if (!filterData) {
      filterData = Filter.getDefault();
    }

    const requests = userIds.map((userId) =>
      api.people.changeProductAdmin(userId, productId, isAdmin)
    );

    await Promise.all(requests);

    const admins = await api.people.getListAdmins(filterData);

    filterData.total = admins.total;

    this.setAdmins(admins.items);
    this.setFilter(filterData);
  };

  getPortalOwner = async (userId) => {
    const owner = await api.people.getUserById(userId);

    this.setOwner(owner);
  };

  fetchPeople = async (filter) => {
    let filterData = filter && filter.clone();
    if (!filterData) {
      filterData = Filter.getDefault();
    }

    const admins = await api.people.getListAdmins(filterData);

    filterData.total = admins.total;
    this.setAdmins(admins.items);
    this.setFilter(filterData);
  };

  getUpdateListAdmin = async (filter) => {
    let filterData = filter && filter.clone();
    if (!filterData) {
      filterData = Filter.getDefault();
    }
    const admins = await api.people.getListAdmins(filterData);

    filterData.total = admins.total;

    this.setAdmins(admins.items);
    this.setFilter(filterData);
  };

  getWhiteLabelLogoText = async () => {
    const res = await api.settings.getLogoText();
    this.setLogoText(res);
  };

  getWhiteLabelLogoSizes = async () => {
    const res = await api.settings.getLogoSizes();
    this.setLogoSizes(res);
  };

  getWhiteLabelLogoUrls = async () => {
    const res = await api.settings.getLogoUrls();
    this.setLogoUrls(Object.values(res));
  };

  setLanguageAndTime = async (lng, timeZoneID) => {
    const res = await api.settings.setLanguageAndTime(lng, timeZoneID);
    console.log("setLanguageAndTime", res);
    if (res) this.setLanguageAndTime({ lng, timeZoneID });
  };

  setGreetingTitle = async (greetingTitle) => {
    const res = await api.settings.setGreetingSettings(greetingTitle);

    if (res) this.setGreetingSettings(greetingTitle);
  };

  setCurrentSchema = async (id) => {
    return api.settings.setCurrentSchema(id);
  };

  setCustomSchema = async (
    userCaption,
    usersCaption,
    groupCaption,
    groupsCaption,
    userPostCaption,
    regDateCaption,
    groupHeadCaption,
    guestCaption,
    guestsCaption
  ) => {
    return api.settings.setCustomSchema(
      userCaption,
      usersCaption,
      groupCaption,
      groupsCaption,
      userPostCaption,
      regDateCaption,
      groupHeadCaption,
      guestCaption,
      guestsCaption
    );
  };

  restoreGreetingTitle = async () => {
    const res = await api.settings.restoreGreetingSettings();
    if (res) this.setGreetingSettings(res.Content);
  };

  getConsumers = async () => {
    const res = await api.settings.getConsumersList();
    this.setConsumers(res);
  };

  updateConsumerProps = async (newProps) => {
    const res = await api.settings.updateConsumerProps(newProps);
    console.log("updateConsumerProps", res);
    await this.getConsumers();
  };

  changePassword = (userId, hash, key) => {
    return api.people.changePassword(userId, hash, key);
  };

  sendOwnerChange = (id) => {
    return api.settings.sendOwnerChange(id);
  };

  getQuota = async () => {
    const res = await api.settings.getPortalQuota();
    if (res) this.setQuota(res);
  };

  getVisits = async (fromDate, toDate) => {
    const res = await api.settings.getPortalVisits(fromDate, toDate);
    if (res) this.setVisits(res);
  };
}

export default SettingsSetupStore;
