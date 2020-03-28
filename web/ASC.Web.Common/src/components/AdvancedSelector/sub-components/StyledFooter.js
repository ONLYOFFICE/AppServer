import styled, { css } from "styled-components";

const StyledFooter = styled.div`
  box-sizing: border-box;
  border-top: 1px solid #eceef1;
  padding: 16px;
  height: 69px;

  ${props =>
    props.withComboBox &&
    css`
      display: flex;
      padding: 16px 0;
    `}

  .ad-selector_combo-box {
    margin-left: 8px;

    .combo-button {
      height: 36px;
    }

    .combo-button-label {
      margin: 0;
    }
  }

  ${props =>
    !props.isVisible &&
    css`
      display: none;
    `}
`;

export default StyledFooter;
