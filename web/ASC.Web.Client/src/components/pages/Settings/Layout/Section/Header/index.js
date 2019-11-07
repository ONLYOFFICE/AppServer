import React from "react";
import { withRouter } from "react-router";
import { Text, utils } from 'asc-web-components';
import styled from 'styled-components';
import { withTranslation } from 'react-i18next';
import { getKeyByLink, settingsTree, getTKeyByKey } from '../../../utils';

const Header = styled(Text.ContentHeader)`
  margin-right: 16px;
  max-width: calc(100vw - 430px);
  @media ${utils.device.tablet} {
    max-width: calc(100vw - 96px);
  }
`;

class SectionHeaderContent extends React.Component {

  constructor(props) {
    super(props);

    const { match, location } = props;
    const fullSettingsUrl = match.url;
    const locationPathname = location.pathname;

    const fullSettingsUrlLength = fullSettingsUrl.length;

    const resultPath = locationPathname.slice(fullSettingsUrlLength + 1);
    const arrayOfParams = resultPath.split('/');

    const key = getKeyByLink(arrayOfParams, settingsTree);
    const header = getTKeyByKey(key, settingsTree);
    this.state = {
      header
    };

  }

  componentDidUpdate() {
    const { match, location } = this.props;
    const fullSettingsUrl = match.url;
    const locationPathname = location.pathname;


    const fullSettingsUrlLength = fullSettingsUrl.length;
    const resultPath = locationPathname.slice(fullSettingsUrlLength + 1);
    const arrayOfParams = resultPath.split('/');

    const key = getKeyByLink(arrayOfParams, settingsTree);
    const header = getTKeyByKey(key, settingsTree);
    header !== this.state.header && this.setState({ header });

  }

  render() {
    const { t } = this.props;
    const { header } = this.state;

    return (
      <Header truncate={true}>
        {t(header)}
      </Header>
    );
  }
};

export default withRouter(withTranslation()(SectionHeaderContent));
