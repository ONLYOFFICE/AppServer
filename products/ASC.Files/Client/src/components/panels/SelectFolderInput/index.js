import React from "react";
import { Provider as MobxProvider } from "mobx-react";

import PropTypes from "prop-types";

import stores from "../../../store/index";
import SelectFolderDialog from "../SelectFolderDialog/index";
import StyledComponent from "./StyledSelectFolderInput";
import SimpleFileInput from "../../SimpleFileInput";

let path = "";

class SelectFolderInputBody extends React.PureComponent {
  constructor(props) {
    super(props);
    this.inputRef = React.createRef();
    this.state = {
      isLoading: false,
      baseFolderPath: "",
      fullFolderPath: "",
      fullFolderPathDefault: "",
    };
    this._isMounted = false;
  }
  componentDidMount() {
    this._isMounted = true;
    const { folderPath } = this.props;

    if (folderPath.length !== 0) {
      this._isMounted &&
        this.setState({
          fullFolderPath: folderPath,
          fullFolderPathDefault: folderPath,
        });
    }
  }

  componentWillUnmount() {
    this._isMounted = false;
  }
  componentDidUpdate(prevProps) {
    const { isSetDefaultFolderPath, folderPath } = this.props;

    if (
      isSetDefaultFolderPath &&
      isSetDefaultFolderPath !== prevProps.isSetDefaultFolderPath
    ) {
      this.setState({
        fullFolderPath: this.state.fullFolderPathDefault,
      });
    }
    if (folderPath !== prevProps.folderPath) {
      this.setState({
        fullFolderPath: folderPath,
        fullFolderPathDefault: folderPath,
      });
    }
  }

  onSetFullPath = (pathName) => {
    this.setState({
      fullFolderPath: pathName,
    });
  };

  onSetBaseFolderPath = (pathName) => {
    this.setState({
      baseFolderPath: pathName,
    });
  };

  onSetLoadingInput = (loading) => {
    this.setState({
      isLoading: loading,
    });
  };
  render() {
    const {
      name,
      onClickInput,
      isPanelVisible,
      withoutProvider,
      onClose,
      isError,
      isSavingProcess,
      isDisabled,
      onSelectFolder,
      onSetLoadingData,
      foldersType,
      folderPath,
      isNeedArrowIcon,
      isSetFolderImmediately,
      id,
      selectedFolderId,
      displayType,
      dialogWithFiles,
    } = this.props;
    const { isLoading, baseFolderPath, fullFolderPath } = this.state;
    const zIndex = 310;

    return (
      <StyledComponent>
        <SimpleFileInput
          name={name}
          className="input-with-folder-path"
          textField={fullFolderPath || baseFolderPath}
          isDisabled={isLoading || isSavingProcess || isDisabled}
          isError={isError}
          onClickInput={onClickInput}
        />

        <SelectFolderDialog
          zIndex={zIndex}
          isPanelVisible={isPanelVisible}
          onClose={onClose}
          folderPath={folderPath}
          onSelectFolder={onSelectFolder}
          onSetLoadingData={onSetLoadingData}
          foldersType={foldersType}
          withoutProvider={withoutProvider}
          onSetFullPath={this.onSetFullPath}
          onSetBaseFolderPath={this.onSetBaseFolderPath}
          onSetLoadingInput={this.onSetLoadingInput}
          isNeedArrowIcon={isNeedArrowIcon}
          isSetFolderImmediately={isSetFolderImmediately}
          id={id}
          selectedFolderId={selectedFolderId}
          displayType={displayType}
          dialogWithFiles={dialogWithFiles}
        />
      </StyledComponent>
    );
  }
}

SelectFolderInputBody.propTypes = {
  onClickInput: PropTypes.func.isRequired,
  onClose: PropTypes.func.isRequired,
  onSelectFolder: PropTypes.func.isRequired,
  onSetLoadingData: PropTypes.func,
  isPanelVisible: PropTypes.bool.isRequired,
  name: PropTypes.string,
  withoutProvider: PropTypes.bool,
  isError: PropTypes.bool,
  isSavingProcess: PropTypes.bool,
  foldersType: PropTypes.oneOf(["common", "third-party"]),
};

SelectFolderInputBody.defaultProps = {
  withoutProvider: false,
  isDisabled: false,
  isError: false,
  folderPath: "",
};

class SelectFolderInput extends React.Component {
  static setFullFolderPath = (foldersArray) => {
    path = "";
    if (foldersArray.length > 1) {
      for (let item of foldersArray) {
        if (!path) {
          path = path + `${item.title}`;
        } else path = path + " " + "/" + " " + `${item.title}`;
      }
    } else {
      for (let item of foldersArray) {
        path = `${item.title}`;
      }
    }
    return path;
  };
  render() {
    return (
      <MobxProvider {...stores}>
        <SelectFolderInputBody {...this.props} />
      </MobxProvider>
    );
  }
}

export default SelectFolderInput;