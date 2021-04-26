import React from "react";
import styled from "styled-components";
import { withTranslation } from "react-i18next";
import DragAndDrop from "@appserver/components/drag-and-drop";
import Row from "@appserver/components/row";
import FilesRowContent from "./FilesRowContent";
import { withRouter } from "react-router-dom";
import { createSelectable } from "react-selectable-fast";

import withFileActions from "../../../../../HOCs/withFileActions";
import withContextOptions from "../../../../../HOCs/withContextOptions";

const StyledSimpleFilesRow = styled(Row)`
  margin-top: -2px;
  ${(props) =>
    !props.contextOptions &&
    `
    & > div:last-child {
        width: 0px;
      }
  `}

  .share-button-icon {
    margin-right: 7px;
    margin-top: -1px;
  }

  .share-button:hover,
  .share-button-icon:hover {
    cursor: pointer;
    color: #657077;
    path {
      fill: #657077;
    }
  }
  -webkit-tap-highlight-color: rgba(0, 0, 0, 0);

  @media (max-width: 1312px) {
    .share-button {
      padding-top: 3px;
    }
  }

  .styled-element {
    margin-right: 7px;
  }
`;

const SimpleFilesRow = createSelectable((props) => {
  const {
    item,
    sectionWidth,
    dragging,
    onContentRowSelect,
    rowContextClick,
    onDrop,
    onMouseDown,
    className,
    isDragging,
    value,
    displayShareButton,
    isPrivacy,
    sharedButton,
    contextOptionsProps,
    checkedProps,
    element,
    onFilesClick,
  } = props;

  return (
    <div ref={props.selectableRef}>
      <DragAndDrop
        data-title={item.title}
        value={value}
        className={className}
        onDrop={onDrop}
        onMouseDown={onMouseDown}
        dragging={dragging && isDragging}
        {...contextOptionsProps}
      >
        <StyledSimpleFilesRow
          key={item.id}
          data={item}
          element={element}
          sectionWidth={sectionWidth}
          contentElement={sharedButton}
          onSelect={onContentRowSelect}
          rowContextClick={rowContextClick}
          isPrivacy={isPrivacy}
          {...checkedProps}
          {...contextOptionsProps}
          contextButtonSpacerWidth={displayShareButton}
        >
          <FilesRowContent
            item={item}
            sectionWidth={sectionWidth}
            onFilesClick={onFilesClick}
          />
        </StyledSimpleFilesRow>
      </DragAndDrop>
    </div>
  );
});

export default withTranslation("Home")(
  withFileActions(withContextOptions(withRouter(SimpleFilesRow)))
);
