import React from "react";
import PropTypes from "prop-types";

import { Icons } from "../../icons";
import Text from "../../text";
import {
  StyledArrowIcon,
  StyledIcon,
  StyledOptionalItem,
  StyledComboButton,
} from "./styled-combobutton";

class ComboButton extends React.Component {
  render() {
    const {
      noBorder,
      onClick,
      isDisabled,
      innerContainer,
      innerContainerClassName,
      selectedOption,
      optionsLength,
      withOptions,
      withAdvancedOptions,
      isOpen,
      scaled,
      size,
    } = this.props;

    const defaultOption = selectedOption.default;

    const SelectedIcon = Icons[selectedOption.icon];

    return (
      <StyledComboButton
        isOpen={isOpen}
        isDisabled={isDisabled}
        noBorder={noBorder}
        containOptions={optionsLength}
        withAdvancedOptions={withAdvancedOptions}
        onClick={onClick}
        scaled={scaled}
        size={size}
        className="combo-button"
      >
        {innerContainer && (
          <StyledOptionalItem
            className={innerContainerClassName}
            isDisabled={isDisabled}
            defaultOption={defaultOption}
          >
            {innerContainer}
          </StyledOptionalItem>
        )}
        {selectedOption && selectedOption.icon && (
          <StyledIcon
            className="forceColor"
            isDisabled={isDisabled}
            defaultOption={defaultOption}
          >
            <SelectedIcon size="scale" className="combo-button_selected-icon" />
          </StyledIcon>
        )}
        <Text
          noBorder={noBorder}
          title={selectedOption.label}
          as="div"
          truncate={true}
          fontWeight={600}
          className="combo-button-label"
        >
          {selectedOption.label}
        </Text>
        <StyledArrowIcon
          needDisplay={withOptions || withAdvancedOptions}
          noBorder={noBorder}
          isOpen={isOpen}
          className="combo-buttons_arrow-icon"
        >
          {(withOptions || withAdvancedOptions) && (
            <Icons.ExpanderDownIcon
              size="scale"
              className="combo-buttons_expander-icon"
            />
          )}
        </StyledArrowIcon>
      </StyledComboButton>
    );
  }
}

ComboButton.propTypes = {
  noBorder: PropTypes.bool,
  isDisabled: PropTypes.bool,
  selectedOption: PropTypes.oneOfType([
    PropTypes.arrayOf(PropTypes.object),
    PropTypes.object,
  ]),
  withOptions: PropTypes.bool,
  optionsLength: PropTypes.number,
  withAdvancedOptions: PropTypes.bool,
  innerContainer: PropTypes.oneOfType([
    PropTypes.arrayOf(PropTypes.node),
    PropTypes.node,
  ]),
  innerContainerClassName: PropTypes.string,
  isOpen: PropTypes.bool,
  size: PropTypes.oneOf(["base", "middle", "big", "huge", "content"]),
  scaled: PropTypes.bool,
  onClick: PropTypes.func,
};

ComboButton.defaultProps = {
  noBorder: false,
  isDisabled: false,
  withOptions: true,
  withAdvancedOptions: false,
  innerContainerClassName: "innerContainer",
  isOpen: false,
  size: "content",
  scaled: false,
};

export default ComboButton;