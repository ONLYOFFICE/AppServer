import React from "react";
import styled from "styled-components";
import PropTypes from "prop-types";
import Text from "../text";
import { mobile } from "../../utils/device";

const EmptyContentBody = styled.div`
  margin: 0 auto;
  padding: 64px 0;

  display: grid;
  grid-template-areas:
    "img headerText"
    ${(props) => props.subheadingText && `"img subheadingText"`}
    ${(props) => props.descriptionText && `"img descriptionText"`}
    "img button";

  grid-column-gap: 16px;
  grid-row-gap: 12px;
  max-width: 800px;

  .ec-image {
    grid-area: img;
    margin: 0 0 0 auto;
  }

  .ec-header {
    grid-area: headerText;
  }

  .ec-subheading {
    grid-area: subheadingText;
  }

  .ec-desc {
    grid-area: descriptionText;
  }

  .ec-buttons {
    grid-area: button;
  }

  @media (orientation: portrait) {
    @media (max-width: 738px) {
      padding-top: 0px;
      max-width: 496px;

      .ec-header {
        padding-top: 16px;
      }

      .ec-image {
        max-height: 100px;
      }
    }

    @media ${mobile} {
      min-width: 343px;
      grid-template-areas:
        "img"
        "headerText"
        ${(props) => props.subheadingText && `"subheadingText"`}
        ${(props) => props.descriptionText && `"descriptionText"`}
        "button";

      .ec-header {
        padding-top: 0px;
      }
      .ec-header,
      .ec-subheading,
      .ec-desc,
      .ec-buttons {
        padding-left: 16px;
      }

      .ec-image {
        height: 75px;
        margin-left: 0;
      }
    }
  }
`;

const EmptyContentImage = styled.img.attrs((props) => ({
  src: props.imageSrc,
  alt: props.imageAlt,
}))`
  background: no-repeat 0 0 transparent;
`;

const EmptyScreenContainer = (props) => {
  const {
    imageSrc,
    imageAlt,
    headerText,
    subheadingText,
    descriptionText,
    buttons,
  } = props;
  return (
    <EmptyContentBody {...props}>
      <EmptyContentImage
        imageSrc={imageSrc}
        imageAlt={imageAlt}
        className="ec-image"
      />

      {headerText && (
        <Text as="span" fontSize="19px" fontWeight="600" className="ec-header">
          {headerText}
        </Text>
      )}

      {subheadingText && (
        <Text as="span" fontWeight="600" className="ec-subheading">
          {subheadingText}
        </Text>
      )}

      {descriptionText && (
        <Text as="span" color="#6A7378" fontSize="12px" className="ec-desc">
          {descriptionText}
        </Text>
      )}

      {buttons && <div className="ec-buttons">{buttons}</div>}
    </EmptyContentBody>
  );
};

EmptyScreenContainer.propTypes = {
  imageSrc: PropTypes.string,
  imageAlt: PropTypes.string,
  headerText: PropTypes.string,
  subheadingText: PropTypes.string,
  descriptionText: PropTypes.string,
  buttons: PropTypes.any,
  className: PropTypes.string,
  id: PropTypes.string,
  style: PropTypes.oneOfType([PropTypes.object, PropTypes.array]),
};

export default EmptyScreenContainer;
