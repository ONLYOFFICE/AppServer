import { makeObservable, observable, computed, action } from "mobx";
import { api, constants } from "asc-web-common";
import SelectedFolderStore from "./SelectedFolderStore";

const { FolderType } = constants;
class TreeFoldersStore {
  selectedFolderStore = null;

  treeFolders = [];
  selectedTreeNode = [];

  constructor() {
    makeObservable(this, {
      selectedFolderStore: observable,

      treeFolders: observable,
      selectedTreeNode: observable,

      myFolderId: computed,
      //shareFolderId: computed,
      //favoritesFolderId: computed,
      //recentFolderId: computed,
      commonFolderId: computed,

      myFolder: computed,
      shareFolder: computed,
      favoritesFolder: computed,
      recentFolder: computed,
      privacyFolder: computed,
      commonFolder: computed,
      recycleBinFolder: computed,

      isMyFolder: computed,
      isShareFolder: computed,
      isFavoritesFolder: computed,
      isRecentFolder: computed,
      isPrivacyFolder: computed,
      isCommonFolder: computed,
      isRecycleBinFolder: computed,

      fetchTreeFolders: action,
      setTreeFolders: action,
    });

    this.selectedFolderStore = new SelectedFolderStore();
  }

  fetchTreeFolders = async () => {
    const treeFolders = await api.files.getFoldersTree();
    this.setTreeFolders(treeFolders);
  };

  setTreeFolders = (treeFolders) => {
    this.treeFolders = treeFolders;
  };

  setSelectedNode = (node) => {
    if (node[0]) {
      this.selectedTreeNode = node;
    }
  };

  /////////////////////////////////////TODO: FOLDER

  get myFolder() {
    return this.treeFolders.find((x) => x.rootFolderName === "@my");
  }

  get shareFolder() {
    return this.treeFolders.find((x) => x.rootFolderName === "@share");
  }

  get favoritesFolder() {
    return this.treeFolders.find((x) => x.rootFolderName === "@favorites");
  }

  get recentFolder() {
    return this.treeFolders.find((x) => x.rootFolderName === "@recent");
  }

  get privacyFolder() {
    return this.treeFolders.find(
      (x) => x.rootFolderType === FolderType.Privacy
    );
  }

  get commonFolder() {
    return this.treeFolders.find((x) => x.rootFolderName === "@common");
  }

  get recycleBinFolder() {
    return this.treeFolders.find((x) => x.rootFolderName === "@trash");
  }

  /////////////////////////////////////TODO: ID

  get myFolderId() {
    return this.myFolder ? this.myFolder.id : null;
  }

  // get shareFolderId() {
  //   return this.shareFolder ?this.shareFolder.id : null;
  // }

  // get favoritesFolderId() {
  //   return this.favoritesFolder ? this.favoritesFolder.id : null;
  // }

  // get recentFolderId() {

  //   return this.recentFolder ? this.recentFolder.id : null;
  // }

  get commonFolderId() {
    return this.commonFolder ? this.commonFolder.id : null;
  }

  /////////////////////////////////////TODO: IS

  get isMyFolder() {
    return this.myFolder && this.myFolder.id === this.selectedFolderStore.id;
  }

  get isShareFolder() {
    return (
      this.shareFolder && this.shareFolder.id === this.selectedFolderStore.id
    );
  }

  get isFavoritesFolder() {
    return (
      this.favoritesFolder &&
      this.selectedFolderStore.id === this.favoritesFolder.id
    );
  }

  get isRecentFolder() {
    return (
      this.recentFolder && this.selectedFolderStore.id === this.recentFolder.id
    );
  }

  get isPrivacyFolder() {
    return (
      this.privacyFolder &&
      this.privacyFolder.rootFolderType ===
        this.selectedFolderStore.rootFolderType
    );
  }

  get isCommonFolder() {
    return (
      this.commonFolder && this.commonFolder.id === this.selectedFolderStore.id
    );
  }

  get isRecycleBinFolder() {
    return (
      this.recycleBinFolder &&
      this.selectedFolderStore.id === this.recycleBinFolder.id
    );
  }
}

export default TreeFoldersStore;

/*

export const getMyDirectoryFolders = createSelector(getMyFolder, (myFolder) => {
  if (myFolder) return myFolder.folders;
});

export const getCommonDirectoryFolders = createSelector(
  getCommonFolder,
  (commonFolder) => {
    if (commonFolder) return commonFolder.folders;
  }
);


*/