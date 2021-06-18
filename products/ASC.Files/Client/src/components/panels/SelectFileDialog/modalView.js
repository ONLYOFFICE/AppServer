import React from "react";
import { Provider as MobxProvider } from "mobx-react";
import { inject, observer } from "mobx-react";
import { I18nextProvider } from "react-i18next";

import PropTypes from "prop-types";
import stores from "../../../store/index";
import i18n from "../SelectFileInput/i18n";
import { StyledAsidePanel, StyledSelectFilePanel } from "../StyledPanels";
import ModalDialog from "@appserver/components/modal-dialog";
import SelectFolderDialog from "../SelectFolderDialog";
import FolderTreeBody from "../SelectFolderDialog/folderTreeBody";
import FileListBody from "./fileListBody";
class SelectFileDialogModalViewBody extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      isLoadingData: false,
      isAvailableFolders: true,
      certainFolders: true,
    };
    this.backupList;
    this.convertedData = [];
    this.folderList = "";
  }

  componentDidMount() {
    const { foldersType, onSetLoadingData, onSelectFolder } = this.props;
    switch (foldersType) {
      case "common":
        SelectFolderDialog.getCommonFolders()
          .then((commonFolder) => {
            this.folderList = commonFolder;
          })
          .then(() => onSelectFolder(`${this.folderList[0].id}`))
          .finally(() => {
            onSetLoadingData && onSetLoadingData(false);

            this.setState({
              isLoadingData: false,
            });
          });
        break;
      case "third-party":
        SelectFolderDialog.getCommonThirdPartyList()
          .then(
            (commonThirdPartyArray) => (this.folderList = commonThirdPartyArray)
          )
          .finally(() => {
            onSetLoadingData && onSetLoadingData(false);

            this.setState({
              isLoadingData: false,
            });
          });
        break;
    }
  }

  onSetLoadingData = (loading) => {
    this.setState({
      isLoadingData: loading,
    });
  };
  onSelect = (folder) => {
    const { onSelectFolder } = this.props;
    onSelectFolder && onSelectFolder(folder[0]);
  };
  render() {
    const {
      t,
      isPanelVisible,
      onClose,
      zIndex,
      isCommonWithoutProvider,
      expandedKeys,
      filter,
      onFileClick,
      filesList,
      isLoadingData,
    } = this.props;
    const { isAvailableFolders } = this.state;
    console.log("filesList", filesList);
    return (
      <StyledAsidePanel visible={isPanelVisible}>
        <ModalDialog
          visible={isPanelVisible}
          zIndex={zIndex}
          onClose={onClose}
          className="select-file-modal-dialog"
          //style={{ maxWidth: "1000px" }}
          displayType="modal"
        >
          <ModalDialog.Header>{t("SelectFile")}</ModalDialog.Header>
          <ModalDialog.Body>
            <StyledSelectFilePanel>
              <div className="modal-dialog_body">
                <div className="modal-dialog_tree-body">
                  <FolderTreeBody
                    expandedKeys={expandedKeys}
                    folderList={this.folderList}
                    onSelect={this.onSelect}
                    isCommonWithoutProvider={isCommonWithoutProvider}
                    certainFolders
                    isAvailableFolders={isAvailableFolders}
                    filter={filter}
                  />
                </div>
                <div className="modal-dialog_files-body">
                  <FileListBody
                    isLoadingData={isLoadingData}
                    filesList={filesList}
                    onFileClick={onFileClick}
                  />
                </div>
              </div>
            </StyledSelectFilePanel>
          </ModalDialog.Body>
          <ModalDialog.Footer>
            <Button
              className="modal-dialog-button"
              primary
              size="big"
              label={t("Common:CloseButton")}
              tabIndex={1}
              onClick={onModalClose}
            />
          </ModalDialog.Footer>
        </ModalDialog>
      </StyledAsidePanel>
    );
  }
}

const SelectFileDialogModalViewWrapper = inject(
  ({ filesStore, treeFoldersStore, selectedFolderStore }) => {
    const { getBackupFiles, filter } = filesStore;
    const { expandedPanelKeys } = treeFoldersStore;
    return {
      getBackupFiles,
      expandedKeys: expandedPanelKeys
        ? expandedPanelKeys
        : selectedFolderStore.pathParts,
      filter,
    };
  }
)(observer(SelectFileDialogModalViewBody));

class SelectFileDialogModalView extends React.Component {
  render() {
    return (
      <MobxProvider {...stores}>
        <I18nextProvider i18n={i18n}>
          <SelectFileDialogModalViewWrapper {...this.props} />
        </I18nextProvider>
      </MobxProvider>
    );
  }
}

export default SelectFileDialogModalView;