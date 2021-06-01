import styled, { css } from "styled-components";
import Base from "../themes/base";

const StyledChart = styled.div`
  position: relative;
`;

StyledChart.defaultProps = { theme: Base };

export default StyledChart;
