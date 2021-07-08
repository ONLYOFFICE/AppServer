import React from "react";
import { inject, observer, Provider as MobxProvider } from "mobx-react";
import { I18nextProvider } from "react-i18next";
import { withTranslation } from "react-i18next";
import PropTypes from "prop-types";
import throttle from "lodash/throttle";

import stores from "../../../store/index";
import i18n from "./i18n";
import SelectFileDialogModalView from "./ModalView";
import SelectFileDialogAsideView from "./AsideView";

import utils from "@appserver/components/utils";
import SelectFolderDialog from "../SelectFolderDialog";
import { getFolder } from "@appserver/common/api/files";
const { desktop } = utils.device;
class SelectFileDialogBody extends React.Component {
  constructor(props) {
    super(props);
    const { folderId, storeFolderId, fileInfo, filter } = this.props;

    this.state = {
      isVisible: false,
      selectedFolder: storeFolderId || "",
      passedId: folderId,
      selectedFile: fileInfo || "",
      fileName: (fileInfo && fileInfo.title) || "",
      filesList: [],
      hasNextPage: true,
      isNextPageLoading: false,
      displayType: this.getDisplayType(),
      page: 0,
      filterParams: this.getFilterParameters(),
    };
    this.throttledResize = throttle(this.setDisplayType, 300);
    this.newFilter = filter.clone();
  }

  getFilterParameters = () => {
    const {
      isImageOnly,
      isDocumentsOnly,
      isArchiveOnly,
      isPresentationOnly,
      isTablesOnly,
      isMediaOnly,
      searchParam = "",
    } = this.props;

    if (isImageOnly) {
      return { filterType: "7", filterValue: searchParam };
    }
    if (isDocumentsOnly) {
      return { filterType: "3", filterValue: searchParam };
    }
    if (isArchiveOnly) {
      return { filterType: "10", filterValue: searchParam };
    }
    if (isPresentationOnly) {
      return { filterType: "4", filterValue: searchParam };
    }
    if (isTablesOnly) {
      return { filterType: "5", filterValue: searchParam };
    }
    if (isMediaOnly) {
      return { filterType: "12", filterValue: searchParam };
    }
    return { filterType: "1", filterValue: "" };
  };

  setFilter = () => {
    const { filterParams } = this.state;
    const { withSubfolders } = this.props;

    this.newFilter.filterType = filterParams.filterType;
    this.newFilter.search = filterParams.filterValue;
    this.newFilter.withSubfolders = withSubfolders;
  };

  componentDidMount() {
    window.addEventListener("resize", this.throttledResize);
    this.setFilter();
  }
  componentWillUnmount() {
    this.throttledResize && this.throttledResize.cancel();
    window.removeEventListener("resize", this.throttledResize);
  }

  getDisplayType = () => {
    const displayType =
      window.innerWidth < desktop.match(/\d+/)[0] ? "aside" : "modal";

    return displayType;
  };

  setDisplayType = () => {
    const displayType = this.getDisplayType();

    this.setState({ displayType: displayType });
  };

  onClickInput = () => {
    this.setState({
      isVisible: true,
    });
  };

  onCloseSelectFolderDialog = () => {
    this.setState({
      isVisible: false,
    });
  };

  onSelectFolder = (id) => {
    const { setFolderId } = this.props;
    setFolderId(id);

    this.setState({
      selectedFolder: id,
      hasNextPage: true,
      filesList: [],
      page: 0,
    });
  };

  onSelectFile = (e) => {
    const { filesList } = this.state;
    const { setFile } = this.props;
    const index = e.target.dataset.index || e.target.name;

    if (!index) return;
    setFile(filesList[+index]);
    this.setState({
      selectedFile: filesList[+index],
      fileName: filesList[+index].title,
    });
  };

  onClickSave = () => {
    const { onSetFileName, onClose, onSelectFile } = this.props;
    const { fileName, selectedFile } = this.state;

    onSetFileName & onSetFileName(fileName);
    onSelectFile && onSelectFile(selectedFile);
    onClose && onClose();
  };

  loadNextPage = () => {
    const { setSelectedNode, setSelectedFolder } = this.props;
    const { selectedFolder, page } = this.state;

    //console.log(`loadNextPage(startIndex=${page}")`);

    const pageCount = 30;
    this.newFilter.page = page;
    this.newFilter.pageCount = pageCount;
    this.setState({ isNextPageLoading: true }, () => {
      getFolder(selectedFolder, this.newFilter)
        .then((data) => {
          let newFilesList = page
            ? this.state.filesList.concat(data.files)
            : data.files;

          setSelectedNode([selectedFolder + ""]);
          const newPathParts = SelectFolderDialog.convertPathParts(
            data.pathParts
          );

          setSelectedFolder({
            folders: data.folders,
            ...data.current,
            pathParts: newPathParts,
            ...{ new: data.new },
          });
          this.setState({
            hasNextPage: newFilesList.length < data.total,
            isNextPageLoading: false,
            filesList: newFilesList,
            page: page + 1,
          });
        })

        .catch((error) => console.log(error));
    });
  };
  render() {
    const {
      t,
      isPanelVisible,
      onClose,
      zIndex,
      foldersType,
      withoutProvider,
      header,
      modalHeightContent,
      loadingLabel,
      folderId,
      onSetFileName,
      tReady,
    } = this.props;
    const {
      isVisible,
      filesList,
      hasNextPage,
      isNextPageLoading,
      selectedFolder,
      displayType,
      selectedFile,
      fileName,
      passedId,
    } = this.state;

    const loadingText = loadingLabel
      ? loadingLabel
      : `${t("Common:LoadingProcessing")} ${t("Common:LoadingDescription")}`;

    return displayType === "aside" ? (
      <SelectFileDialogAsideView
        t={t}
        isPanelVisible={isPanelVisible}
        zIndex={zIndex}
        onClose={onClose}
        isVisible={isVisible}
        withoutProvider={withoutProvider}
        foldersType={foldersType}
        filesList={filesList}
        onSelectFile={this.onSelectFile}
        onClickInput={this.onClickInput}
        onClickSave={this.onClickSave}
        onCloseSelectFolderDialog={this.onCloseSelectFolderDialog}
        onSelectFolder={this.onSelectFolder}
        hasNextPage={hasNextPage}
        isNextPageLoading={isNextPageLoading}
        loadNextPage={this.loadNextPage}
        selectedFolder={selectedFolder}
        header={header}
        loadingText={loadingText}
        selectedFile={selectedFile}
        folderId={folderId}
        onSetFileName={onSetFileName}
        fileName={fileName}
        displayType={displayType}
        isTranslationsReady={tReady}
        passedId={passedId}
      />
    ) : (
      <SelectFileDialogModalView
        t={t}
        isPanelVisible={isPanelVisible}
        onClose={onClose}
        onSelectFolder={this.onSelectFolder}
        onSelectFile={this.onSelectFile}
        foldersType={foldersType}
        onClickSave={this.onClickSave}
        filesList={filesList}
        hasNextPage={hasNextPage}
        isNextPageLoading={isNextPageLoading}
        loadNextPage={this.loadNextPage}
        selectedFolder={selectedFolder}
        withoutProvider={withoutProvider}
        header={header}
        modalHeightContent={modalHeightContent}
        loadingText={loadingText}
        selectedFile={selectedFile}
        folderId={folderId}
        passedId={passedId}
      />
    );
  }
}
SelectFileDialogBody.propTypes = {
  onClose: PropTypes.func.isRequired,
  isPanelVisible: PropTypes.bool.isRequired,
  onSelectFile: PropTypes.func.isRequired,
  foldersType: PropTypes.oneOf(["common", "third-party"]),
  folderId: PropTypes.string,
  withoutProvider: PropTypes.bool,
  header: PropTypes.string,
  zIndex: PropTypes.number,
};

SelectFileDialogModalView.defaultProps = {
  folderId: "",
  header: "",
  withoutProvider: false,
  zIndex: 310,
};

const SelectFileDialogWrapper = inject(
  ({
    filesStore,
    selectedFilesStore,
    treeFoldersStore,
    selectedFolderStore,
  }) => {
    const {
      folderId: storeFolderId,
      fileInfo,
      setFolderId,
      setFile,
    } = selectedFilesStore;
    const { setSelectedNode } = treeFoldersStore;
    const { filter } = filesStore;
    const { setSelectedFolder } = selectedFolderStore;
    return {
      storeFolderId,
      fileInfo,
      setFile,
      setFolderId,
      setSelectedFolder,
      setSelectedNode,
      filter,
    };
  }
)(
  observer(
    withTranslation(["SelectFile", "Common", "Translations"])(
      SelectFileDialogBody
    )
  )
);
class SelectFileDialog extends React.Component {
  render() {
    return (
      <MobxProvider {...stores}>
        <I18nextProvider i18n={i18n}>
          <SelectFileDialogWrapper {...this.props} />
        </I18nextProvider>
      </MobxProvider>
    );
  }
}

export default SelectFileDialog;