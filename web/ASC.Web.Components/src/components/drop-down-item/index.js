import React from 'react'
import styled, { css } from 'styled-components'
import PropTypes from 'prop-types'
import { Icons } from '../icons'

const itemTruncate = css`
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const fontStyle = css`
    font-family: 'Open Sans', sans-serif, Arial;
    font-style: normal;
`;

const disabledAndHeaderStyle = css`
    color: #A3A9AE;
              
    &:hover {
      cursor: default;
      background-color: white;
    }
`;

const StyledDropdownItem = styled.div`
    display: block;
    width: 100%;
    border: 0px;
    cursor: pointer;
    margin: 0px;
    padding: 0 16px;
    line-height: 36px;
    box-sizing: border-box;
    text-align: left;
    background: none;
    text-decoration: none;
    user-select: none;

    ${fontStyle}

    font-weight: 600;
    font-size: ${props => props.fontSize}px;
    color: ${props => props.color};
    text-transform: none;

    ${itemTruncate}

    &:hover {
      background-color: ${props => props.noHover ? 'white' : '#F8F9F9'};
      text-align: left;

      &:first-of-type {
        border-radius: 6px 6px 0 0;
      }

      &:last-of-type {
        border-radius: 0 0 6px 6px;
      }
    }

    ${props => props.isSeparator && 
      `
        border: 0.5px solid #ECEEF1;
        cursor: default;
        margin: 0px 16px;
        line-height: 1px;
        height: 1px;
        width: calc(100% - 32px);
  
        &:hover {
          cursor: default;
        }
      `
    }
  
    ${props => props.isHeader &&
      `
        ${disabledAndHeaderStyle}

        text-transform: uppercase;
      `
    }
  
    ${props => props.disabled && disabledAndHeaderStyle }
`;

const IconWrapper = styled.span`
    display: inline-block;
    width: 16px;
    margin-right: 8px;
    line-height: 14px;
`;

const DropDownItem = props => {
  //console.log("DropDownItem render");
  const { isSeparator, label, icon, children, disabled , onClick } = props;
  const color = disabled ? '#A3A9AE' : '#333333';

  const onClickAction = () => {
    onClick && !disabled && onClick();
  }

  return (
    <StyledDropdownItem {...props} onClick={onClickAction}>
      {icon &&
        <IconWrapper>
          {React.createElement(Icons[icon], { size: "scale", color: color, isfill: true })}
        </IconWrapper>
      }
      {isSeparator ? '\u00A0' : label ? label : children && children}
    </StyledDropdownItem>
  );
};

DropDownItem.propTypes = {
  isSeparator: PropTypes.bool,
  isHeader: PropTypes.bool,
  tabIndex: PropTypes.number,
  label: PropTypes.string,
  disabled: PropTypes.bool,
  icon: PropTypes.string,
  noHover: PropTypes.bool,
  onClick: PropTypes.func,
  children: PropTypes.any,
  fontSize: PropTypes.number,
  color: PropTypes.string,
};

DropDownItem.defaultProps = {
  isSeparator: false,
  isHeader: false,
  tabIndex: -1,
  label: '',
  disabled: false,
  noHover: false,
  fontSize: 13,
  color: '#333333'
};

export default DropDownItem