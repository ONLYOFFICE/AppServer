import React, { useEffect } from "react";
import PropTypes from "prop-types";
import { withRouter } from "react-router";
//import RequestLoader from "@appserver/components/request-loader";
import PageLayout from "@appserver/common/components/PageLayout";
import { showLoader, hideLoader } from "@appserver/common/utils";
import {
  ArticleHeaderContent,
  ArticleBodyContent,
  ArticleMainButtonContent,
} from "../../components/Article";
import {
  SectionHeaderContent,
  SectionBodyContent,
  SectionFilterContent,
  SectionPagingContent,
} from "./Section";
import { inject, observer } from "mobx-react";
import { isMobile } from "react-device-detect";

const Home = ({ isLoading }) => {
  //console.log("People Home render");

  useEffect(() => {
    isLoading ? showLoader() : hideLoader();
  }, [isLoading]);

  return (
    <PageLayout withBodyScroll withBodyAutoFocus={!isMobile}>
      <PageLayout.ArticleHeader>
        <ArticleHeaderContent />
      </PageLayout.ArticleHeader>

      <PageLayout.ArticleMainButton>
        <ArticleMainButtonContent />
      </PageLayout.ArticleMainButton>

      <PageLayout.ArticleBody>
        <ArticleBodyContent />
      </PageLayout.ArticleBody>

      <PageLayout.SectionHeader>
        <SectionHeaderContent />
      </PageLayout.SectionHeader>

      <PageLayout.SectionFilter>
        <SectionFilterContent />
      </PageLayout.SectionFilter>

      <PageLayout.SectionBody>
        <SectionBodyContent />
      </PageLayout.SectionBody>

      <PageLayout.SectionPaging>
        <SectionPagingContent />
      </PageLayout.SectionPaging>
    </PageLayout>
  );
};

Home.propTypes = {
  isLoading: PropTypes.bool,
};

export default withRouter(
  inject(({ peopleStore }) => ({
    isLoading: peopleStore.isLoading,
  }))(observer(Home))
);