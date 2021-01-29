import React, { useEffect, useState } from "react";
import styled, { css } from "styled-components";
import MobileLayout from "./MobileLayout";
import { utils } from "asc-web-components";
import {
  isIOS,
  isFirefox,
  isSafari,
  isMobile,
  isMobileOnly,
  isChrome,
  isTablet,
} from "react-device-detect";
import PropTypes from "prop-types";
import { connect } from "react-redux";
import store from "../../store";

const { setIsTabletView } = store.auth.actions;
const { getIsTabletView, isArticlePinned } = store.auth.selectors;

const { size, isSmallTablet } = utils.device;

const StyledContainer = styled.div`
  width: 100%;
  height: ${(props) =>
    (props.isTabletView || isMobile) && !isFirefox
      ? `${props.contentHeight}px`
      : "100vh"};

  #articleScrollBar {
    > .scroll-body {
      position: ${(props) =>
        isMobile && !isSmallTablet()
          ? props.isArticlePinned
            ? "static"
            : "absolute"
          : "absolute"} !important;

      ${(props) =>
        isMobile &&
        props.isArticlePinned &&
        !isSmallTablet() &&
        css`
          overflow-y: hidden !important;
          overflow-x: hidden !important;
          width: 208px;
        `}
    }

    .nav-thumb-horizontal {
      ${(props) =>
        props.isTabletView &&
        css`
          height: 0 !important;
        `}
    }
  }
`;

const Layout = (props) => {
  const { children, isTabletView, setIsTabletView, isArticlePinned } = props;

  const [contentHeight, setContentHeight] = useState();
  const [isPortrait, setIsPortrait] = useState();

  const intervalTime = 100;
  const endTimeout = 300;
  let intervalHandler;
  let timeoutHandler;

  useEffect(() => {
    setIsPortrait(window.innerHeight > window.innerWidth);
  });
  useEffect(() => {
    const isTablet = window.innerWidth <= size.tablet;
    setIsTabletView(isTablet);

    let mediaQuery = window.matchMedia("(max-width: 1024px)");
    mediaQuery.addEventListener("change", onWidthChange);

    return () => {
      mediaQuery.removeEventListener("change", onWidthChange);
      if (intervalHandler) clearInterval(intervalHandler);
      if (timeoutHandler) clearTimeout(timeoutHandler);
    };
  }, []);

  useEffect(() => {
    if (isTabletView || isMobile) {
      if (isIOS && isSafari) window.addEventListener("resize", onResize);
      else window.addEventListener("orientationchange", onOrientationChange);
      changeRootHeight();
    }

    return () => {
      if (isTabletView || isMobile) {
        if (isIOS && isSafari) window.removeEventListener("resize", onResize);
        else
          window.removeEventListener("orientationchange", onOrientationChange);
      }
    };
  }, [isTabletView]);

  const onWidthChange = (e) => {
    const { matches } = e;
    setIsTabletView(matches);
  };
  const onResize = () => {
    changeRootHeight();
  };
  const onOrientationChange = () => {
    changeRootHeight();
  };
  const changeRootHeight = () => {
    intervalHandler && clearInterval(intervalHandler);
    timeoutHandler && clearTimeout(timeoutHandler);

    let lastInnerHeight, noChangeCount;

    const updateHeight = () => {
      const correctorMobileChrome = 57; // ios
      const correctorTabletSafari = 71; // ios

      clearInterval(intervalHandler);
      clearTimeout(timeoutHandler);

      intervalHandler = null;
      timeoutHandler = null;

      let height = window.innerHeight;

      if (isMobileOnly && isIOS && isChrome) {
        if (window.innerHeight < window.innerWidth && isPortrait) {
          height = window.screen.availWidth - correctorMobileChrome;
        }
      }
      if (isTablet && isIOS && isSafari) {
        if (
          window.innerHeight < window.innerWidth &&
          window.innerWidth > 1024
        ) {
          height = window.screen.availHeight - correctorTabletSafari;
        }
      }
      setContentHeight(height);
    };
    intervalHandler = setInterval(() => {
      //console.log("changeRootHeight setInterval"); TODO: need to refactoring
      if (window.innerHeight === lastInnerHeight) {
        noChangeCount++;

        if (noChangeCount === intervalTime) {
          updateHeight();
        }
      } else {
        lastInnerHeight = window.innerHeight;
        noChangeCount = 0;
      }
    });

    timeoutHandler = setTimeout(() => {
      updateHeight();
    }, endTimeout);
  };

  return (
    <StyledContainer
      className="Layout"
      isTabletView={isTabletView}
      contentHeight={contentHeight}
      isArticlePinned={isArticlePinned}
    >
      {isMobile ? <MobileLayout {...props} /> : children}
    </StyledContainer>
  );
};

Layout.propTypes = {
  children: PropTypes.any,
  isTabletView: PropTypes.bool,
  setIsTabletView: PropTypes.func,
};
const mapStateToProps = (state) => {
  return {
    isTabletView: getIsTabletView(state),
    isArticlePinned: isArticlePinned(state),
  };
};

const mapDispatchToProps = (dispatch) => {
  return {
    setIsTabletView: (isTabletView) => dispatch(setIsTabletView(isTabletView)),
  };
};
export default connect(mapStateToProps, mapDispatchToProps)(Layout);
