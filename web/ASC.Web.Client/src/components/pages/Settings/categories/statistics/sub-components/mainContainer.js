import { isMobile, isTablet, mobile } from "@appserver/components/utils/device";
import styled from "styled-components";

const MainContainer = styled.div`
  width: 100%;

  .category-item-wrapper {
    margin-bottom: 40px;

    .category-item-heading {
      display: flex;
      align-items: center;
      margin-bottom: 5px;
    }

    .category-item-subheader {
      font-size: 13px;
      font-weight: 600;
      margin-bottom: 5px;
    }

    .category-item-description {
      color: #555f65;
      font-size: 12px;
      max-width: 1024px;
    }

    .inherit-title-link {
      margin-right: 7px;
      font-size: 19px;
      font-weight: 600;
    }

    .visits-chart {
      height: 230px;
    }

    .visits-selectors-container {
      display: inline-flex;

      @media ${mobile} {
        display: block;
      }

      .scrollbar {
        width: 340px !important;
      }

      .visits-datepicker-container {
        @media ${mobile} {
          display: flex;
          align-items: baseline;
        }

        .visits-datepicker {
          display: inline-block;
          margin-left: 4px;
          margin-right: 4px;
          margin-bottom: 8px;

          @media ${mobile} {
            display: inline-flex;
            width: auto;
            margin-top: 8px;

            :first-of-type {
              margin-left: 0px;
            }

            :last-of-type {
              margin-right: 0px;
            }
          }
        }
      }
    }

    .storage-value-title {
      max-width: 420px;

      .storage-value-current {
        font-family: Open Sans;
        font-size: 13px;
        font-style: normal;
        font-weight: 600;
        line-height: 18px;
        letter-spacing: 0px;
        text-align: left;
        display: inline-block;
      }

      .storage-value-max {
        font-family: Open Sans;
        font-size: 13px;
        font-style: normal;
        font-weight: 600;
        line-height: 18px;
        letter-spacing: 0px;
        text-align: left;
        margin-left: auto;
        float: right;
      }
    }
  }
`;

export default MainContainer;
