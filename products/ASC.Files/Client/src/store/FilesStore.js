import { makeAutoObservable } from "mobx";
import api from "@appserver/common/api";
import {
  FolderType,
  FilterType,
  FileType,
  FileAction,
  AppServerConfig,
} from "@appserver/common/constants";
import history from "@appserver/common/history";
import { createTreeFolders } from "../helpers/files-helpers";
import config from "../../package.json";
import { combineUrl } from "@appserver/common/utils";
import { updateTempContent } from "@appserver/common/utils";

const { FilesFilter } = api;

class FilesStore {
  authStore;
  settingsStore;
  userStore;
  fileActionStore;
  selectedFolderStore;
  treeFoldersStore;
  formatsStore;

  isLoaded = false;
  isLoading = false;
  viewAs = "row";
  dragging = false;
  privacyInstructions = "https://www.onlyoffice.com/private-rooms.aspx";
  isInit = false;

  tooltipPageX = 0;
  tooltipPageY = 0;
  startDrag = false;

  firstLoad = true;
  files = [];
  folders = [];
  selection = [];
  selected = "close";
  filter = FilesFilter.getDefault(); //TODO: FILTER
  loadTimeout = null;

  constructor(
    authStore,
    settingsStore,
    userStore,
    fileActionStore,
    selectedFolderStore,
    treeFoldersStore,
    formatsStore
  ) {
    const pathname = window.location.pathname.toLowerCase();
    this.isEditor = pathname.indexOf("doceditor") !== -1;

    makeAutoObservable(this);
    this.authStore = authStore;
    this.settingsStore = settingsStore;
    this.userStore = userStore;
    this.fileActionStore = fileActionStore;
    this.selectedFolderStore = selectedFolderStore;
    this.treeFoldersStore = treeFoldersStore;
    this.formatsStore = formatsStore;
  }

  setIsLoaded = (isLoaded) => {
    this.isLoaded = isLoaded;
  };

  setViewAs = (viewAs) => {
    this.viewAs = viewAs;
  };

  setDragging = (dragging) => {
    this.dragging = dragging;
  };

  setIsLoading = (isLoading) => {
    this.isLoading = isLoading;
  };

  setTooltipPosition = (tooltipPageX, tooltipPageY) => {
    this.tooltipPageX = tooltipPageX;
    this.tooltipPageY = tooltipPageY;
  };

  setStartDrag = (startDrag) => {
    this.startDrag = startDrag;
  };

  get tooltipValue() {
    if (!this.dragging) return null;

    const selectionLength = this.selection.length;
    const elementTitle = selectionLength && this.selection[0].title;
    const singleElement = selectionLength === 1;
    const filesCount = singleElement ? elementTitle : selectionLength;
    const { isShareFolder, isCommonFolder } = this.treeFoldersStore;

    let operationName;

    if (this.authStore.isAdmin && isShareFolder) {
      operationName = "copy";
    } else if (!this.authStore.isAdmin && (isShareFolder || isCommonFolder)) {
      operationName = "copy";
    } else {
      operationName = "move";
    }

    return operationName === "copy"
      ? singleElement
        ? { label: "TooltipElementCopyMessage", filesCount }
        : { label: "TooltipElementsCopyMessage", filesCount }
      : singleElement
      ? { label: "TooltipElementMoveMessage", filesCount }
      : { label: "TooltipElementsMoveMessage", filesCount };
  }

  initFiles = () => {
    if (this.isInit) return;

    const { isAuthenticated } = this.authStore;
    const {
      getPortalCultures,
      isDesktopClient,
      getIsEncryptionSupport,
      getEncryptionKeys,
      setModuleInfo,
    } = this.settingsStore;

    setModuleInfo(config.homepage, config.id);

    const requests = [];

    updateTempContent();
    if (!isAuthenticated) {
      return this.setIsLoaded(true);
    } else {
      updateTempContent(isAuthenticated);
    }

    if (!this.isEditor) {
      requests.push(
        getPortalCultures(),
        this.treeFoldersStore.fetchTreeFolders()
      );

      if (isDesktopClient) {
        requests.push(getIsEncryptionSupport(), getEncryptionKeys());
      }
    }

    return Promise.all(requests).then(() => (this.isInit = true));
  };

  setFirstLoad = (firstLoad) => {
    this.firstLoad = firstLoad;
  };

  setFiles = (files) => {
    this.files = files;
  };

  setFolders = (folders) => {
    this.folders = folders;
  };

  setFile = (file) => {
    const index = this.files.findIndex((x) => x.id === file.id);
    if (index !== -1) this.files[index] = file;
  };

  setFolder = (folder) => {
    const index = this.folders.findIndex((x) => x.id === folder.id);
    if (index !== -1) this.folders[index] = folder;
  };

  getFilesChecked = (file, selected) => {
    const type = file.fileType;
    switch (selected) {
      case "all":
        return true;
      case FilterType.FoldersOnly.toString():
        return file.parentId;
      case FilterType.DocumentsOnly.toString():
        return type === FileType.Document;
      case FilterType.PresentationsOnly.toString():
        return type === FileType.Presentation;
      case FilterType.SpreadsheetsOnly.toString():
        return type === FileType.Spreadsheet;
      case FilterType.ImagesOnly.toString():
        return type === FileType.Image;
      case FilterType.MediaOnly.toString():
        return type === FileType.Video || type === FileType.Audio;
      case FilterType.ArchiveOnly.toString():
        return type === FileType.Archive;
      case FilterType.FilesOnly.toString():
        return type || !file.parentId;
      default:
        return false;
    }
  };

  getFilesBySelected = (files, selected) => {
    let newSelection = [];
    files.forEach((file) => {
      const checked = this.getFilesChecked(file, selected);

      if (checked) newSelection.push(file);
    });

    return newSelection;
  };

  setSelected = (selected) => {
    this.selected = selected;
    const files = this.files.concat(this.folders);
    this.selection = this.getFilesBySelected(files, selected);
  };

  setSelection = (selection) => {
    this.selection = selection;
  };

  //TODO: FILTER
  setFilesFilter = (filter) => {
    this.setFilterUrl(filter);
    this.filter = filter;
  };

  setFilter = (filter) => {
    this.filter = filter;
  };

  setFilesOwner = (folderIds, fileIds, ownerId) => {
    return api.files.setFileOwner(folderIds, fileIds, ownerId);
  };

  setFilterUrl = (filter) => {
    const urlFilter = filter.toUrlParams();
    history.push(
      combineUrl(
        AppServerConfig.proxyURL,
        config.homepage,
        `/filter?${urlFilter}`
      )
    );
  };

  fetchFiles = (folderId, filter, clearFilter = true) => {
    const filterData = filter ? filter.clone() : FilesFilter.getDefault();
    filterData.folder = folderId;
    const {
      privacyFolder,
      expandedKeys,
      setExpandedKeys,
      setSelectedNode,
    } = this.treeFoldersStore;
    setSelectedNode([folderId + ""]);

    if (privacyFolder && privacyFolder.id === +folderId) {
      if (!this.settingsStore.isEncryptionSupport) {
        const newExpandedKeys = createTreeFolders(
          privacyFolder.pathParts,
          expandedKeys
        );
        setExpandedKeys(newExpandedKeys);
        filterData.total = 0;
        this.setFilesFilter(filterData); //TODO: FILTER
        if (clearFilter) {
          this.setFolders([]);
          this.setFiles([]);
          this.fileActionStore.setAction({ type: null });
          this.setSelected("close");

          this.selectedFolderStore.setSelectedFolder({
            folders: [],
            ...privacyFolder,
            pathParts: privacyFolder.pathParts,
            ...{ new: 0 },
          });
        }
        return Promise.resolve();
      }
    }

    //TODO: fix @my
    let requestCounter = 1;
    const request = () =>
      api.files
        .getFolder(folderId, filter)
        .then((data) => {
          const isPrivacyFolder =
            data.current.rootFolderType === FolderType.Privacy;

          const newExpandedKeys = createTreeFolders(
            data.pathParts,
            expandedKeys
          );
          setExpandedKeys(newExpandedKeys);
          filterData.total = data.total;
          this.setFilesFilter(filterData); //TODO: FILTER
          this.setFolders(
            isPrivacyFolder && !this.settingsStore.isEncryptionSupport
              ? []
              : data.folders
          );
          this.setFiles(
            isPrivacyFolder && !this.settingsStore.isEncryptionSupport
              ? []
              : data.files
          );
          if (clearFilter) {
            this.fileActionStore.setAction({ type: null });
            this.setSelected("close");
          }

          this.selectedFolderStore.setSelectedFolder({
            folders: data.folders,
            ...data.current,
            pathParts: data.pathParts,
            ...{ new: data.new },
          });

          const selectedFolder = {
            selectedFolder: { ...this.selectedFolderStore },
          };
          return Promise.resolve(selectedFolder);
        })
        .catch(() => {
          if (folderId === "@my" && requestCounter !== 0 && !this.isInit) {
            requestCounter--;
            setTimeout(() => {
              return request();
            }, 5000);
          }
        });

    return request();
  };

  isFileSelected = (fileId, parentId) => {
    const item = this.selection.find(
      (x) => x.id === fileId && x.parentId === parentId
    );

    return item !== undefined;
  };

  selectFile = (file) => {
    const { id, parentId } = file;
    const isFileSelected = this.isFileSelected(id, parentId);
    if (!isFileSelected) this.selection.push(file);
  };

  deselectFile = (file) => {
    const { id, parentId } = file;
    const isFileSelected = this.isFileSelected(id, parentId);
    if (isFileSelected)
      this.selection = this.selection.filter((x) => x.id !== id);
  };

  removeOptions = (options, toRemoveArray) =>
    options.filter((o) => !toRemoveArray.includes(o));

  getFilesContextOptions = (item, canOpenPlayer) => {
    const isVisitor =
      (this.userStore.user && this.userStore.user.isVisitor) || false;
    const isFile = !!item.fileExst || item.contentLength;
    const isFavorite = item.fileStatus === 32;
    const isFullAccess = item.access < 2;
    const withoutShare = false; //TODO: need this prop
    const isThirdPartyItem = item.providerKey;
    const hasNew = item.new > 0;
    const canConvert = false; //TODO: fix of added convert check;
    const isEncrypted = item.encrypted;
    const isDocuSign = false; //TODO: need this prop;
    const isEditing = false; //TODO: need this prop;

    const {
      isRecycleBinFolder,
      isPrivacyFolder,
      isRecentFolder,
      isShareFolder,
      isCommonFolder,
      isFavoritesFolder,
      isThirdPartyFolder,
      isMyFolder,
    } = this.treeFoldersStore;

    const { isDesktopClient } = this.settingsStore;

    if (isFile) {
      let fileOptions = [
        //"open",
        "edit",
        "preview",
        "view",
        "separator0",
        "sharing-settings",
        "external-link",
        "owner-change",
        "link-for-portal-users",
        "send-by-email",
        "docu-sign",
        "version", //category
        "finalize-version",
        "show-version-history",
        "block-unblock-version", //need split
        "separator1",
        "open-location",
        "mark-read",
        "mark-as-favorite",
        "download",
        "download-as",
        "convert",
        "move", //category
        "move-to",
        "copy-to",
        "copy",
        "restore",
        "rename",
        "separator2",
        "remove-from-favorites",
        "unsubscribe",
        "delete",
      ];

      if (!this.isWebEditSelected) {
        fileOptions = this.removeOptions(fileOptions, ["download-as"]);
      }

      if (!canConvert || isEncrypted) {
        fileOptions = this.removeOptions(fileOptions, ["convert"]);
      }

      if (!canOpenPlayer) {
        fileOptions = this.removeOptions(fileOptions, ["view"]);
      } else {
        fileOptions = this.removeOptions(fileOptions, ["edit", "preview"]);
      }

      if (!isDocuSign) {
        fileOptions = this.removeOptions(fileOptions, ["docu-sign"]);
      }

      if (isEditing) {
        fileOptions = this.removeOptions(fileOptions, [
          "finalize-version",
          "move-to",
          "delete",
        ]);
        if (isThirdPartyFolder) {
          fileOptions = this.removeOptions(fileOptions, ["rename"]);
        }
      }

      if (isFavorite) {
        fileOptions = this.removeOptions(fileOptions, ["mark-as-favorite"]);
      } else {
        fileOptions = this.removeOptions(fileOptions, [
          "remove-from-favorites",
        ]);

        if (isFavoritesFolder) {
          fileOptions = this.removeOptions(fileOptions, ["mark-as-favorite"]);
        }
      }

      if (isFavoritesFolder) {
        fileOptions = this.removeOptions(fileOptions, [
          "move-to",
          "delete",
          "copy",
        ]);

        if (!isFavorite) {
          fileOptions = this.removeOptions(fileOptions, ["separator2"]);
        }
      }

      if (isEncrypted) {
        fileOptions = this.removeOptions(fileOptions, [
          "open",
          "edit",
          "link-for-portal-users",
          "external-link",
          "send-by-email",
          "block-unblock-version", //need split
          "version", //category
          "finalize-version",
          "copy-to",
          "copy",
          "mark-as-favorite",
        ]);
      }

      if (
        isCommonFolder ||
        isFavoritesFolder ||
        isPrivacyFolder ||
        isRecentFolder
      ) {
        fileOptions = this.removeOptions(fileOptions, ["copy"]);
      }

      if (isRecycleBinFolder) {
        fileOptions = this.removeOptions(fileOptions, [
          "open",
          "open-location",
          "edit",
          "link-for-portal-users",
          "sharing-settings",
          "external-link",
          "send-by-email",
          "block-unblock-version", //need split
          "version", //category
          "finalize-version",
          "show-version-history",
          "move", //category
          "move-to",
          "copy-to",
          "copy",
          "mark-read",
          "mark-as-favorite",
          "remove-from-favorites",
          "rename",
          "separator0",
          "separator1",
        ]);
      } else {
        fileOptions = this.removeOptions(fileOptions, ["restore"]);
      }

      if (!isFullAccess) {
        fileOptions = this.removeOptions(fileOptions, [
          "finalize-version",
          "rename",
          "block-unblock-version",
          "copy",
        ]);
      }

      if (isVisitor) {
        fileOptions = this.removeOptions(fileOptions, [
          "block-unblock-version",
          "finalize-version",
          "mark-as-favorite",
          "remove-from-favorites",
        ]);

        if (!isFullAccess) {
          fileOptions = this.removeOptions(fileOptions, ["rename"]);
        }
      }

      if (!this.canShareOwnerChange(item)) {
        fileOptions = this.removeOptions(fileOptions, ["owner-change"]);
      }

      if (isThirdPartyItem) {
        fileOptions = this.removeOptions(fileOptions, [
          "owner-change",
          "finalize-version",
          "copy",
        ]);
      }

      if (isCommonFolder)
        if (!this.userAccess) {
          fileOptions = this.removeOptions(fileOptions, [
            "owner-change",
            "move-to",
            "delete",
            "copy",
          ]);
          if (!isFavorite) {
            fileOptions = this.removeOptions(fileOptions, ["separator2"]);
          }
        }

      if (withoutShare) {
        fileOptions = this.removeOptions(fileOptions, [
          "sharing-settings",
          "external-link",
        ]);
      }

      if (!hasNew) {
        fileOptions = this.removeOptions(fileOptions, ["mark-read"]);
      }

      if (!isRecentFolder) {
        fileOptions = this.removeOptions(fileOptions, ["open-location"]);
      } else if (!isFavorite) {
        fileOptions = this.removeOptions(fileOptions, ["separator2"]);
      }

      return fileOptions;
    } else {
      let folderOptions = [
        "open",
        "separator0",
        "sharing-settings",
        "owner-change",
        "link-for-portal-users",
        "separator1",
        "download",
        "move", //category
        "move-to",
        "copy-to",
        "mark-read",
        "restore",
        "rename",
        "change-thirdparty-info",
        "separator2",
        "unsubscribe",
        "delete",
      ];

      if (isPrivacyFolder) {
        folderOptions = this.removeOptions(folderOptions, ["copy"]);
      }

      if (isRecycleBinFolder) {
        folderOptions = this.removeOptions(folderOptions, [
          "open",
          "link-for-portal-users",
          "sharing-settings",
          "move",
          "move-to",
          "copy-to",
          "mark-read",
          "rename",
          "separator0",
          "separator1",
        ]);
      } else {
        folderOptions = this.removeOptions(folderOptions, ["restore"]);
      }

      if (!isFullAccess) {
        //TODO: if added Projects, add project folder check
        folderOptions = this.removeOptions(folderOptions, [
          "rename",
          "change-thirdparty-info",
        ]);
      }

      if (!this.canShareOwnerChange(item)) {
        folderOptions = this.removeOptions(folderOptions, ["owner-change"]);
      }

      if (!isFullAccess) {
        folderOptions = this.removeOptions(folderOptions, [
          "owner-change",
          "move-to",
          "delete",
          "change-thirdparty-info",
          "separator2",
        ]);

        if (isVisitor) {
          folderOptions = this.removeOptions(folderOptions, ["rename"]);
        }
      }

      if (withoutShare) {
        folderOptions = this.removeOptions(folderOptions, ["sharing-settings"]);
      }

      if (!hasNew) {
        folderOptions = this.removeOptions(folderOptions, ["mark-read"]);
      }

      if (!isCommonFolder) {
        folderOptions = this.removeOptions(folderOptions, ["unsubscribe"]);
      }

      if (isThirdPartyItem) {
        folderOptions = this.removeOptions(folderOptions, ["owner-change"]);

        if (isShareFolder) {
          folderOptions = this.removeOptions(folderOptions, [
            "change-thirdparty-info",
          ]);
        } else {
          if (isDesktopClient) {
            folderOptions = this.removeOptions(folderOptions, [
              "change-thirdparty-info",
            ]);
          }

          folderOptions = this.removeOptions(folderOptions, [
            "remove",
            "move-to",
          ]);

          if (!item) {
            //For damaged items
            folderOptions = this.removeOptions(folderOptions, [
              "open",
              "download",
              "copy-to",
              "rename",
            ]);
          }
        }
      } else {
        folderOptions = this.removeOptions(folderOptions, [
          "change-thirdparty-info",
        ]);
      }

      return folderOptions;
    }
  };

  addFileToRecentlyViewed = (fileId) => {
    if (this.treeFoldersStore.isPrivacyFolder) return Promise.resolve();
    return api.files.addFileToRecentlyViewed(fileId);
  };

  createFile = (folderId, title) => {
    return api.files.createFile(folderId, title).then((file) => {
      return Promise.resolve(file);
    });
  };

  createFolder(parentFolderId, title) {
    return api.files.createFolder(parentFolderId, title);
  }

  setFile = (file) => {
    const fileIndex = this.files.findIndex((f) => f.id === file.id);
    if (fileIndex !== -1) this.files[fileIndex] = file;
  };

  setFolder = (folder) => {
    const folderIndex = this.folders.findIndex((f) => f.id === folder.id);
    if (folderIndex !== -1) this.folders[folderIndex] = folder;
  };

  updateFolderBadge = (id, count) => {
    const folder = this.folders.find((x) => x.id === id);
    if (folder) folder.new -= count;
  };

  updateFileBadge = (id) => {
    const file = this.files.find((x) => x.id === id);
    if (file) file.fileStatus = 0;
  };

  updateFilesBadge = () => {
    for (let file of this.files) {
      file.fileStatus = 0;
    }
  };

  updateFoldersBadge = () => {
    for (let folder of this.folders) {
      folder.new = 0;
    }
  };

  updateFile = (fileId, title) => {
    return api.files
      .updateFile(fileId, title)
      .then((file) => this.setFile(file));
  };

  renameFolder = (folderId, title) => {
    return api.files.renameFolder(folderId, title).then((folder) => {
      this.setFolder(folder);
    });
  };

  getFilesCount = () => {
    const { filesCount, foldersCount } = this.selectedFolderStore;
    return filesCount + this.folders ? this.folders.length : foldersCount;
  };

  getServiceFilesCount = () => {
    const filesLength = this.files ? this.files.length : 0;
    const foldersLength = this.folders ? this.folders.length : 0;
    return filesLength + foldersLength;
  };

  canShareOwnerChange = (item) => {
    const userId = this.userStore.user && this.userStore.user.id;
    const isCommonFolder =
      this.treeFoldersStore.commonFolder &&
      this.selectedFolderStore.pathParts &&
      this.treeFoldersStore.commonFolder.id ===
        this.selectedFolderStore.pathParts[0];

    if (item.providerKey || !isCommonFolder) {
      return false;
    } else if (this.authStore.isAdmin) {
      return true;
    } else if (item.createdBy.id === userId) {
      return true;
    } else {
      return false;
    }
  };

  get canShare() {
    const folderType = this.selectedFolderStore.rootFolderType;
    const isVisitor =
      (this.userStore.user && this.userStore.user.isVisitor) || false;

    if (isVisitor) {
      return false;
    }

    switch (folderType) {
      case FolderType.USER:
        return true;
      case FolderType.SHARE:
        return false;
      case FolderType.COMMON:
        return this.authStore.isAdmin;
      case FolderType.TRASH:
        return false;
      case FolderType.Favorites:
        return false;
      case FolderType.Recent:
        return false;
      case FolderType.Privacy:
        return true;
      default:
        return false;
    }
  }

  get currentFilesCount() {
    const serviceFilesCount = this.getServiceFilesCount();
    const filesCount = this.getFilesCount();
    return this.selectedFolderStore.providerItem
      ? serviceFilesCount
      : filesCount;
  }

  get iconOfDraggedFile() {
    const { getIcon } = this.formatsStore.iconFormatsStore;

    if (this.selection.length === 1) {
      const icon = getIcon(
        24,
        this.selection[0].fileExst,
        this.selection[0].providerKey
      );

      return icon;
    }
    return null;
  }

  get isHeaderVisible() {
    return this.selection.length > 0 || this.selected !== "close";
  }

  get isHeaderIndeterminate() {
    const items = [...this.files, ...this.folders];
    return this.isHeaderVisible && this.selection.length
      ? this.selection.length < items.length
      : false;
  }

  get isHeaderChecked() {
    const items = [...this.files, ...this.folders];
    return this.isHeaderVisible && this.selection.length === items.length;
  }

  get canCreate() {
    switch (this.selectedFolderStore.rootFolderType) {
      case FolderType.USER:
        return true;
      case FolderType.SHARE:
        const canCreateInSharedFolder = this.selectedFolderStore.access === 1;
        return (
          !this.selectedFolderStore.isRootFolder && canCreateInSharedFolder
        );
      case FolderType.Privacy:
        return (
          this.settingsStore.isDesktopClient &&
          this.settingsStore.isEncryptionSupport
        );
      case FolderType.COMMON:
        return this.authStore.isAdmin;
      case FolderType.TRASH:
      default:
        return false;
    }
  }

  onCreateAddTempItem = (items) => {
    const { getFileIcon, getFolderIcon } = this.formatsStore.iconFormatsStore;

    if (items.length && items[0].id === -1) return; //TODO: if change media collection from state remove this;
    const icon = this.fileActionStore.extension
      ? getFileIcon(`.${this.fileActionStore.extension}`, 24)
      : getFolderIcon(null, 24);

    items.unshift({
      id: -1,
      title: "",
      parentId: this.selectedFolderStore.id,
      fileExst: this.fileActionStore.extension,
      icon,
    });
  };

  get filesList() {
    const { mediaViewersFormatsStore, iconFormatsStore } = this.formatsStore;
    const { getIcon } = iconFormatsStore;
    //return [...this.folders, ...this.files];

    const items = [...this.folders, ...this.files];
    const newItem = items.map((item) => {
      const {
        access,
        comment,
        contentLength,
        created,
        createdBy,
        fileExst,
        filesCount,
        fileStatus,
        fileType,
        folderId,
        foldersCount,
        id,
        locked,
        parentId,
        pureContentLength,
        rootFolderType,
        shared,
        title,
        updated,
        updatedBy,
        version,
        versionGroup,
        viewUrl,
        webUrl,
        providerKey,
      } = item;

      const canOpenPlayer = mediaViewersFormatsStore.isMediaOrImage(
        item.fileExst
      );

      const contextOptions = this.getFilesContextOptions(item, canOpenPlayer);

      //const isCanWebEdit = canWebEdit(item.fileExst);
      const icon = getIcon(24, fileExst, providerKey, contentLength);

      return {
        access,
        //checked,
        comment,
        contentLength,
        contextOptions,
        created,
        createdBy,
        fileExst,
        filesCount,
        fileStatus,
        fileType,
        folderId,
        foldersCount,
        icon,
        id,
        //isFolder,
        locked,
        new: item.new,
        parentId,
        pureContentLength,
        rootFolderType,
        //selectedItem,
        shared,
        title,
        updated,
        updatedBy,
        version,
        versionGroup,
        viewUrl,
        webUrl,
        providerKey,
        canOpenPlayer,
        //canWebEdit: isCanWebEdit,
        //canShare,
      };
    });

    if (this.fileActionStore.type === FileAction.Create) {
      this.onCreateAddTempItem(newItem);
    }

    return newItem;
  }

  get sortedFiles() {
    const {
      isSpreadsheet,
      isPresentation,
    } = this.formatsStore.iconFormatsStore;
    const { canWebEdit } = this.formatsStore.docserviceStore;

    const formatKeys = Object.freeze({
      OriginalFormat: 0,
    });

    let sortedFiles = {
      documents: [],
      spreadsheets: [],
      presentations: [],
      other: [],
    };

    for (let item of this.selection) {
      item.checked = true;
      item.format = formatKeys.OriginalFormat;

      if (item.fileExst) {
        if (isSpreadsheet(item.fileExst)) {
          sortedFiles.spreadsheets.push(item);
        } else if (isPresentation(item.fileExst)) {
          sortedFiles.presentations.push(item);
        } else if (item.fileExst !== ".pdf" && canWebEdit(item.fileExst)) {
          sortedFiles.documents.push(item);
        } else {
          sortedFiles.other.push(item);
        }
      } else {
        sortedFiles.other.push(item);
      }
    }

    return sortedFiles;
  }

  get userAccess() {
    switch (this.selectedFolderStore.rootFolderType) {
      case FolderType.USER:
        return true;
      case FolderType.SHARE:
        return false;
      case FolderType.COMMON:
        return (
          this.authStore.isAdmin ||
          this.selection.some((x) => x.access === 0 || x.access === 1)
        );
      case FolderType.Privacy:
        return true;
      case FolderType.TRASH:
        return true;
      default:
        return false;
    }
  }

  get isAccessedSelected() {
    return (
      (this.selection.length &&
        this.selection.every((x) => x.access === 1 || x.access === 0)) ||
      (this.authStore.isAdmin && this.selection.length)
    );
  }

  get isOnlyFoldersSelected() {
    return this.selection.every((selected) => selected.fileExst === undefined);
  }

  get isThirdPartySelection() {
    const withProvider = this.selection.find((x) => !x.providerKey);
    return !withProvider && this.selectedFolderStore.isRootFolder;
  }

  get isWebEditSelected() {
    const { editedDocs } = this.formatsStore.docserviceStore;

    return this.selection.some((selected) => {
      if (selected.isFolder === true || !selected.fileExst) return false;
      return editedDocs.find((format) => selected.fileExst === format);
    });
  }

  get selectionTitle() {
    if (this.selection.length === 0) return null;
    return this.selection.find((el) => el.title).title;
  }

  getOptions = (selection, externalAccess = false) => {
    const {
      canWebEdit,
      canWebComment,
      canWebReview,
      canFormFillingDocs,
      canWebFilterEditing,
    } = this.formatsStore.docserviceStore;

    let AccessOptions = [];

    AccessOptions.push("ReadOnly", "DenyAccess");

    const webEdit = selection.find((x) => canWebEdit(x.fileExst));

    const webComment = selection.find((x) => canWebComment(x.fileExst));

    const webReview = selection.find((x) => canWebReview(x.fileExst));

    const formFillingDocs = selection.find((x) =>
      canFormFillingDocs(x.fileExst)
    );

    const webFilter = selection.find((x) => canWebFilterEditing(x.fileExst));

    if (webEdit || !externalAccess) AccessOptions.push("FullAccess");

    if (webComment) AccessOptions.push("Comment");
    if (webReview) AccessOptions.push("Review");
    if (formFillingDocs) AccessOptions.push("FormFilling");
    if (webFilter) AccessOptions.push("FilterEditing");

    return AccessOptions;
  };

  getAccessOption = (selection) => {
    return this.getOptions(selection);
  };

  getExternalAccessOption = (selection) => {
    return this.getOptions(selection, true);
  };

  setSelections = (items) => {
    if (!items.length && !this.selection.length) return;

    if (items.length !== this.selection.length) {
      this.setSelection(items);
    } else if (items.length === 0) {
      const item = this.selection.find(
        (x) => x.id === item[0].id && x.fileExst === item.fileExst
      );
      if (!item) this.setSelection(items);
    }
  };

  getShareUsers(folderIds, fileIds) {
    return api.files.getShareFiles(fileIds, folderIds);
  }

  setShareFiles = (
    folderIds,
    fileIds,
    share,
    notify,
    sharingMessage,
    externalAccess,
    ownerId
  ) => {
    let externalAccessRequest = [];
    if (fileIds.length === 1 && externalAccess !== null) {
      externalAccessRequest = fileIds.map((id) =>
        api.files.setExternalAccess(id, externalAccess)
      );
    }

    const ownerChangeRequest = ownerId
      ? [this.setFilesOwner(folderIds, fileIds, ownerId)]
      : [];

    const shareRequest = !!share.length
      ? [
          api.files.setShareFiles(
            fileIds,
            folderIds,
            share,
            notify,
            sharingMessage
          ),
        ]
      : [];

    const requests = [
      ...ownerChangeRequest,
      ...shareRequest,
      ...externalAccessRequest,
    ];

    return Promise.all(requests);
  };

  markItemAsFavorite = (id) => api.files.markAsFavorite(id);

  removeItemFromFavorite = (id) => api.files.removeFromFavorite(id);

  fetchFavoritesFolder = async (folderId) => {
    const favoritesFolder = await api.files.getFolder(folderId);
    this.setFolders(favoritesFolder.folders);
    this.setFiles(favoritesFolder.files);

    this.selectedFolderStore.setSelectedFolder({
      folders: favoritesFolder.folders,
      ...favoritesFolder.current,
      pathParts: favoritesFolder.pathParts,
    });
  };

  getFileInfo = async (id) => {
    const fileInfo = await api.files.getFileInfo(id);
    this.setFile(fileInfo);
  };

  openDocEditor = (id, providerKey = null, tab = null, url = null) => {
    if (providerKey) {
      tab
        ? (tab.location = url)
        : window.open(
            combineUrl(
              AppServerConfig.proxyURL,
              config.homepage,
              `/doceditor?fileId=${id}`
            ),
            "_blank"
          );
    } else {
      return this.addFileToRecentlyViewed(id)
        .then(() => console.log("Pushed to recently viewed"))
        .catch((e) => console.error(e))
        .finally(
          tab
            ? (tab.location = url)
            : window.open(
                combineUrl(
                  AppServerConfig.proxyURL,
                  config.homepage,
                  `/doceditor?fileId=${id}`
                ),
                "_blank"
              )
        );
    }
  };
}

export default FilesStore;
