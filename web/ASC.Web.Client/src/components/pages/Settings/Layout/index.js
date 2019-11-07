import React, { Suspense, useEffect } from "react";
import { connect } from 'react-redux';
import { Loader, PageLayout } from "asc-web-components";
import i18n from "../i18n";
import { I18nextProvider } from "react-i18next";
import {
  ArticleHeaderContent,
  ArticleBodyContent
} from "./Article";
import { SectionHeaderContent } from './Section';
import { setCurrentProductId } from '../../../../store/auth/actions';

const Layout = ({ currentProductId, setCurrentProductId, language, children }) => {

  useEffect(() => {
    currentProductId !== 'settings' && setCurrentProductId('settings');
    i18n.changeLanguage(language);
  }, [language]);

  return (
    <I18nextProvider i18n={i18n}>
      <Suspense
        fallback={<Loader className="pageLoader" type="rombs" size={40} />}
      >
        <PageLayout
          withBodyScroll={true}
          articleHeaderContent={<ArticleHeaderContent />}
          articleBodyContent={<ArticleBodyContent />}
          sectionHeaderContent={<SectionHeaderContent />}
          sectionBodyContent={children}
        />

      </Suspense>
    </I18nextProvider >
  );
};

function mapStateToProps(state) {
  return {
    language: state.auth.user.cultureName
  };
}

export default connect(mapStateToProps, { setCurrentProductId })(Layout);
