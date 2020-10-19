import React from "react";
import PropTypes from "prop-types";
import styled from "styled-components";

const LogoItem = styled.div`
  display: flex;
  min-width: 56px;
  min-height: 56px;
  align-items: center;
  padding: 0 16px;
  cursor: pointer;

  .nav-logo-wrapper {
    -webkit-tap-highlight-color: rgba(0, 0, 0, 0);
  }

  .nav-logo-icon {
    display: ${(props) => (props.opened ? "block" : "none")};
  }
`;

const NavLogoItem = React.memo((props) => {
  //console.log("NavLogoItem render");
  return (
    <LogoItem opened={props.opened}>
      <a className="nav-logo-wrapper" href="/">
        <img className="nav-logo-icon" src="images/nav.logo.opened.react.svg" />
      </a>
    </LogoItem>
  );
});

NavLogoItem.displayName = "NavLogoItem";

NavLogoItem.propTypes = {
  opened: PropTypes.bool,
  onClick: PropTypes.func,
};

export default NavLogoItem;
