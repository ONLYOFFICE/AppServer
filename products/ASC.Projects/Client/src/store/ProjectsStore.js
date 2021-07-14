import { action, makeObservable, observable } from "mobx";
import config from "../../package.json";
import { updateTempContent } from "@appserver/common/utils";
import api from "@appserver/common/api";

const { ProjectsFilter } = api;

class ProjectsStore {
  authStore;
  settingsStore;
  userStore;
  projectsFilterStore;
  filter = ProjectsFilter.getDefault();
  treeFoldersStore;
  isLoading = false;
  isLoaded = false;
  isInit = false;
  items = [];
  // filterCommonOptions = [];

  firstLoad = true;

  constructor(
    authStore,
    settingsStore,
    userStore,
    treeFoldersStore,
    projectsFilterStore
  ) {
    this.authStore = authStore;
    this.settingsStore = settingsStore;
    this.userStore = userStore;
    this.treeFoldersStore = treeFoldersStore;
    this.projectsFilterStore = projectsFilterStore;

    makeObservable(this, {
      isLoading: observable,
      firstLoad: observable,
      items: observable,
      // filterCommonOptions: observable,
      filter: observable,
      isLoaded: observable,
      setIsLoading: action,
      setFirstLoad: action,
      setIsLoaded: action,
      setItems: action,
      init: action,
      setFilter: action,
      // setFilterCommonOptions: action,
    });
  }

  init = () => {
    //if (this.isInit) return;

    const { isAuthenticated } = this.authStore;
    const { getPortalCultures, setModuleInfo } = this.settingsStore;
    const { fetchTreeFolders } = this.treeFoldersStore;

    setModuleInfo(config.homepage, config.id);

    const requests = [];

    updateTempContent();

    if (!isAuthenticated) {
      return this.setIsLoaded(true);
    } else {
      updateTempContent(isAuthenticated);
    }

    requests.push(getPortalCultures(), fetchTreeFolders());

    this.setIsLoaded(true);

    return Promise.all(requests).then(() => {
      this.setIsLoaded(true);
      this.isInit = true;
    });
  };

  setIsLoading = (loading) => {
    this.isLoading = loading;
  };

  setFirstLoad = (firstLoad) => {
    this.firstLoad = firstLoad;
  };

  setIsLoaded = (isLoaded) => {
    this.isLoaded = isLoaded;
  };

  setItems = (items) => {
    this.items = items;
  };

  setFilter = (filter) => {
    console.log(filter);
    this.filter = filter;
  };
}

export default ProjectsStore;
