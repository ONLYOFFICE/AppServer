import styled, { css } from "styled-components";
import { Scrollbar } from "asc-web-components";

const PanelStyles = css`
  .panel_combo-box {
    margin-left: 8px;

    .combo-button {
      height: 36px;
    }

    .combo-button-label {
      margin: 0;
    }
  }
`;

const StyledAsidePanel = styled.div`
  z-index: 310;
  .modal-dialog-aside {
    padding: 0;
    transform: translateX(${(props) => (props.visible ? "0" : "500px")});
    width: 500px;

    @media (max-width: 550px) {
      width: 320px;
      transform: translateX(${(props) => (props.visible ? "0" : "320px")});
    }
  }

  .header_aside-panel {
    transition: unset;
    transform: translateX(${(props) => (props.visible ? "0" : "500px")});
    width: 500px;
    overflow-y: hidden;

    @media (max-width: 550px) {
      width: 320px;
      transform: translateX(${(props) => (props.visible ? "0" : "320px")});
    }
  }
  ${PanelStyles}
`;

const StyledAddUsersPanelPanel = styled.div`
  .header_aside-panel {
    transform: translateX(${(props) => (props.visible ? "0" : "500px")});
    width: 500px;

    @media (max-width: 550px) {
      width: 320px;
      transform: translateX(${(props) => (props.visible ? "0" : "320px")});
    }
  }
  ${PanelStyles}
`;

const StyledAddGroupsPanel = styled.div`
  .header_aside-panel {
    transform: translateX(${(props) => (props.visible ? "0" : "500px")});
    width: 500px;

    @media (max-width: 550px) {
      width: 320px;
      transform: translateX(${(props) => (props.visible ? "0" : "320px")});
    }
  }
  ${PanelStyles}
`;

const StyledEmbeddingPanel = styled.div`
  .header_aside-panel {
    transform: translateX(${(props) => (props.visible ? "0" : "500px")});
    width: 500px;

    @media (max-width: 550px) {
      width: 320px;
      transform: translateX(${(props) => (props.visible ? "0" : "320px")});
    }
  }
  ${PanelStyles}
`;

const StyledContent = styled.div`
  box-sizing: border-box;
  position: relative;
  width: 100%;
  background-color: #fff;
  padding: 0 16px;

  .files-operations-panel {
    border-bottom: 1px solid #dee2e6;
  }

  .header_aside-panel-header {
    max-width: 500px;
    margin: 0 0 0 16px;
    line-height: 56px;
    font-weight: 700;
  }

  .header_aside-panel-plus-icon {
    margin-left: auto;
  }
`;

const StyledHeaderContent = styled.div`
  display: flex;
  align-items: center;
`;

const StyledBody = styled.div`
  .files-operations-body {
    padding: 0 16px;
  }

  .selector-wrapper {
    position: fixed;
    height: 94%;

    .column-options {
      padding: 0 0 16px 0;
      width: 470px;

      .header-options {
        .combo-button-label {
          max-width: 435px;

          @media (max-width: 550px) {
            width: 255px;
          }
        }
      }

      .row-option {
        .option_checkbox {
          width: 440px;

          @media (max-width: 550px) {
            width: 265px;
          }
        }
      }

      @media (max-width: 550px) {
        width: 320px;
        padding: 0 28px 16px 0;
      }

      .body-options {
        width: 100%;
      }
    }
    .footer {
      @media (max-width: 550px) {
        padding: 16px 28px 16px 0;
      }
    }
  }

  .embedding-panel_links-container {
    display: flex;
    .embedding-panel_link {
      margin-right: 8px;
      height: 32px;
      background-color: #eceef1;
      line-height: 30px;
      padding: 0px 8px;
    }
  }

  .embedding-panel_inputs-container {
    display: flex;

    .embedding-panel_input {
      margin-right: 8px;
      width: 94px;
    }
  }

  .embedding-panel_text {
    padding: 8px 0 4px 0;
  }

  .embedding-panel_copy-icon {
    position: absolute;
    z-index: 1;
    margin: 8px;
    right: 16px;
  }
`;

const StyledSharingHeaderContent = styled.div`
  display: flex;
  align-items: center;
  border-bottom: 1px solid #dee2e6;

  .sharing_panel-icons-container {
    display: flex;
    margin-left: auto;

    .sharing_panel-drop-down-wrapper {
      position: relative;

      .sharing_panel-drop-down {
        padding: 0px;
      }
      .sharing_panel-plus-icon {
        margin-right: 12px;
      }
    }
  }
`;

const StyledSharingBody = styled(Scrollbar)`
  position: relative;
  padding: 16px 0;

  .nav-thumb-vertical {
    opacity: 0;
    transition: opacity 200ms ease;
  }

  :hover {
    .nav-thumb-vertical {
      opacity: 1;
    }
  }

  .sharing_panel-text {
    line-height: 24px;
  }

  .sharing_panel-link {
    a {
      text-decoration: none !important;
    }
  }

  .sharing_panel-link-combo-box {
    margin-left: auto;
    .combo-button {
      height: 24px;
      width: 94px;

      svg {
        bottom: 6px;
        position: absolute;
        height: 8px;
        width: 8px;
      }
    }
  }

  .sharing-access-combo-box-icon {
    path {
      fill: #a3a9ae;
    }
  }

  .sharing_panel-owner-icon {
    padding: 0px 24px 0px 8px;
  }

  .sharing_panel-remove-icon {
    margin-left: auto;
    line-height: 24px;

    svg {
      width: 16px;
      height: 16px;
    }
  }

  .panel_combo-box {
    margin-left: 8px;

    .combo-button {
      height: 30px;
      margin: 0;
      padding: 0;
      border: none;
    }

    .combo-button-label {
      margin: 0;
    }
  }

  .sharing_panel-text-area {
    position: fixed;
    bottom: 70px;
    width: 94%;
  }
`;

const StyledFooter = styled.div`
  display: flex;
  position: fixed;
  bottom: 0;
  padding: 16px 0;
  width: 94%;
  background-color: #fff;

  .sharing_panel-button {
    margin-left: auto;
  }

  @media (max-width: 550px) {
    width: 90%;
  }
`;

export {
  StyledAsidePanel,
  StyledAddGroupsPanel,
  StyledAddUsersPanelPanel,
  StyledEmbeddingPanel,
  StyledContent,
  StyledHeaderContent,
  StyledBody,
  StyledSharingHeaderContent,
  StyledSharingBody,
  StyledFooter,
};
