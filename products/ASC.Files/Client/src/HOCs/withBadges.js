import React from "react";
import { inject, observer } from "mobx-react";

import {
  ShareAccessRights,
  AppServerConfig,
} from "@appserver/common/constants";
import toastr from "studio/toastr";
import { combineUrl } from "@appserver/common/utils";
import {
  convertFile,
  getFileConversationProgress,
} from "@appserver/common/api/files";

import Badges from "../components/Badges";
import config from "../../package.json";

export default function withBadges(WrappedComponent) {
  class WithBadges extends React.Component {
    constructor(props) {
      super(props);
      this.state = { showConvertDialog: false };
    }
    onClickLock = () => {
      const { item, lockFileAction, isAdmin } = this.props;
      const { locked, id, access } = item;

      if (!isAdmin || !access === 0) return;
      lockFileAction(id, !locked).catch((err) => toastr.error(err));
    };

    onClickFavorite = () => {
      const { t, item, setFavoriteAction } = this.props;

      setFavoriteAction("remove", item.id)
        .then(() => toastr.success(t("RemovedFromFavorites")))
        .catch((err) => toastr.error(err));
    };

    onShowVersionHistory = () => {
      const {
        homepage,
        isTabletView,
        item,
        setIsVerHistoryPanel,
        fetchFileVersions,
        history,
        isTrashFolder,
      } = this.props;
      if (isTrashFolder) return;

      if (!isTabletView) {
        fetchFileVersions(item.id + "");
        setIsVerHistoryPanel(true);
      } else {
        history.push(
          combineUrl(AppServerConfig.proxyURL, homepage, `/${item.id}/history`)
        );
      }
    };
    onBadgeClick = () => {
      const {
        item,
        selectedFolderPathParts,
        markAsRead,
        setNewFilesPanelVisible,
        setNewFilesIds,
        updateRootBadge,
        updateFileBadge,
      } = this.props;
      if (item.fileExst) {
        markAsRead([], [item.id])
          .then(() => {
            updateRootBadge(selectedFolderPathParts[0], 1);
            updateFileBadge(item.id);
          })
          .catch((err) => toastr.error(err));
      } else {
        setNewFilesPanelVisible(true);
        const newFolderIds = selectedFolderPathParts;
        newFolderIds.push(item.id);
        setNewFilesIds(newFolderIds);
      }
    };

    setConvertDialogVisible = () =>
      this.setState({ showConvertDialog: !this.state.showConvertDialog });

    onConvert = () => {
      const { item, t, setSecondaryProgressBarData } = this.props;
      setSecondaryProgressBarData({
        icon: "file",
        visible: true,
        percent: 0,
        label: t("Convert"),
        alert: false,
      });
      this.setState({ showConvertDialog: false }, () =>
        convertFile(item.id).then((convertRes) => {
          if (convertRes && convertRes[0] && convertRes[0].progress !== 100) {
            this.getConvertProgress(item.id);
          }
        })
      );
    };

    getConvertProgress = (fileId) => {
      const {
        selectedFolderId,
        filter,
        setIsLoading,
        setSecondaryProgressBarData,
        t,
        clearSecondaryProgressData,
        fetchFiles,
      } = this.props;
      getFileConversationProgress(fileId).then((res) => {
        if (res && res[0] && res[0].progress !== 100) {
          setSecondaryProgressBarData({
            icon: "file",
            visible: true,
            percent: res[0].progress,
            label: t("Convert"),
            alert: false,
          });
          setTimeout(() => this.getConvertProgress(fileId), 1000);
        } else {
          if (res[0].error) {
            setSecondaryProgressBarData({
              visible: true,
              alert: true,
            });
            toastr.error(res[0].error);
            setTimeout(() => clearSecondaryProgressData(), TIMEOUT);
          } else {
            setSecondaryProgressBarData({
              icon: "file",
              visible: true,
              percent: 100,
              label: t("Convert"),
              alert: false,
            });
            setTimeout(() => clearSecondaryProgressData(), TIMEOUT);
            const newFilter = filter.clone();
            fetchFiles(selectedFolderId, newFilter)
              .catch((err) => {
                setSecondaryProgressBarData({
                  visible: true,
                  alert: true,
                });
                //toastr.error(err);
                setTimeout(() => clearSecondaryProgressData(), TIMEOUT);
              })
              .finally(() => setIsLoading(false));
          }
        }
      });
    };
    render() {
      const { showConvertDialog } = this.state;
      const {
        t,
        item,
        canWebEdit,
        isTrashFolder,
        canConvert,
        onFilesClick, // from withFileAction HOC
        isAdmin,
      } = this.props;
      const { fileStatus, access } = item;

      const newItems = item.new || fileStatus === 2;
      const showNew = !!newItems;

      const accessToEdit =
        access === ShareAccessRights.FullAccess ||
        access === ShareAccessRights.None; // TODO: fix access type for owner (now - None)

      const badgesComponent = (
        <Badges
          t={t}
          item={item}
          isAdmin={isAdmin}
          showNew={showNew}
          newItems={newItems}
          canWebEdit={canWebEdit}
          canConvert={canConvert}
          isTrashFolder={isTrashFolder}
          accessToEdit={accessToEdit}
          onClickLock={this.onClickLock}
          onClickFavorite={this.onClickFavorite}
          onShowVersionHistory={this.onShowVersionHistory}
          onBadgeClick={this.onBadgeClick}
          setConvertDialogVisible={this.setConvertDialogVisible}
          onFilesClick={onFilesClick}
        />
      );

      return (
        <>
          {showConvertDialog && (
            <ConvertDialog
              visible={showConvertDialog}
              onClose={this.setConvertDialogVisible}
              onConvert={this.onConvert}
            />
          )}
          <WrappedComponent badgesComponent={badgesComponent} {...this.props} />
        </>
      );
    }
  }

  return inject(
    (
      {
        auth,
        formatsStore,
        treeFoldersStore,
        filesActionsStore,
        versionHistoryStore,
        selectedFolderStore,
        dialogsStore,
        filesStore,
        uploadDataStore,
      },
      { item }
    ) => {
      const { docserviceStore } = formatsStore;
      const { isRecycleBinFolder, updateRootBadge } = treeFoldersStore;
      const {
        lockFileAction,
        setFavoriteAction,
        markAsRead,
      } = filesActionsStore;
      const { isTabletView } = auth.settingsStore;
      const { setIsVerHistoryPanel, fetchFileVersions } = versionHistoryStore;
      const { setNewFilesPanelVisible, setNewFilesIds } = dialogsStore;
      const { updateFileBadge, filter, setIsLoading, fetchFiles } = filesStore;
      const { secondaryProgressDataStore } = uploadDataStore;
      const {
        setSecondaryProgressBarData,
        clearSecondaryProgressData,
      } = secondaryProgressDataStore;

      const canWebEdit = docserviceStore.canWebEdit(item.fileExst);
      const canConvert = docserviceStore.canConvert(item.fileExst);

      return {
        isAdmin: auth.isAdmin,
        canWebEdit,
        canConvert,
        isTrashFolder: isRecycleBinFolder,
        lockFileAction,
        setFavoriteAction,
        homepage: config.homepage,
        isTabletView,
        setIsVerHistoryPanel,
        fetchFileVersions,
        selectedFolderPathParts: selectedFolderStore.pathParts,
        markAsRead,
        setNewFilesPanelVisible,
        setNewFilesIds,
        updateRootBadge,
        updateFileBadge,
        setSecondaryProgressBarData,
        selectedFolderId: selectedFolderStore.id,
        filter,
        setIsLoading,
        clearSecondaryProgressData,
        fetchFiles,
      };
    }
  )(observer(WithBadges));
}
