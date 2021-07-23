import React, { useEffect } from "react";
import { ReactSVG } from "react-svg";
import PropTypes from "prop-types";
import { withRouter } from "react-router";
import Text from "@appserver/components/text";
import Link from "@appserver/components/link";
import Badge from "@appserver/components/badge";
import Box from "@appserver/components/box";
import EmptyScreenContainer from "@appserver/components/empty-screen-container";
import ExternalLinkIcon from "../../../../../../public/images/external.link.react.svg";
import Loaders from "@appserver/common/components/Loaders";
import toastr from "studio/toastr";
import PageLayout from "@appserver/common/components/PageLayout";
import { useTranslation, withTranslation } from "react-i18next";

import { inject } from "mobx-react";
import i18n from "../../i18n";
import { I18nextProvider } from "react-i18next";
import {
  ArticleBodyContent,
  ArticleHeaderContent,
  ArticleMainButtonContent,
} from "../../components/Article";
import {
  SectionBodyContent,
  SectionFilterContent,
  SectionHeaderContent,
  SectionPagingContent,
} from "./Section";
import api from "@appserver/common/api";
import config from "../../../package.json";

const { ProjectsFilter, TasksFilter } = api;
const Home = ({
  homepage,
  setIsLoading,
  firstLoad,
  setFirstLoad,
  fetchProjects,
  history,
  selectedTreeNode,
  fetchTasks,
  setExpandedKeys,
  filter,
  getProjectFilterCommonOptions,
  setFilterCommonOptions,
  tReady,
  getTaskFilterCommonOptions,
}) => {
  const { location } = history;
  const { pathname } = location;
  useEffect(() => {
    const reg = new RegExp(`${homepage}((/?)$|/filter)`, "gm");
    const match = window.location.pathname.match(reg);
    let filterObj = null;

    if (match && match.length > 0) {
      filterObj = ProjectsFilter.getFilter(window.location);

      if (!filterObj) {
        filterObj = ProjectsFilter.getDefault();
        setIsLoading(true);
        fetchProjects(filterObj).finally(() => {
          setIsLoading(false);
          setFirstLoad(false);
        });
      }
    }

    if (pathname.indexOf("/projects/filter") > -1) {
      const newFilter = ProjectsFilter.getFilter(location);
      setIsLoading(true);
      fetchProjects(newFilter, newFilter.folder).finally(() => {
        setIsLoading(false);
        setFirstLoad(false);
      });
      // тест разворачивания дерева при f5
      setExpandedKeys(["projects"]);
    }

    if (pathname.indexOf("/task/filter") > -1) {
      const newFilter = TasksFilter.getFilter(location);
      setIsLoading(true);
      fetchTasks(newFilter).finally(() => {
        setIsLoading(false);
        setFirstLoad(false);
      });
      setExpandedKeys(["tasks"]);
    }
  }, []);

  return (
    <PageLayout>
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

const HomeWrapper = inject(
  ({
    auth,
    projectsStore,
    projectsFilterStore,
    tasksFilterStore,
    treeFoldersStore,
  }) => {
    const {
      isLoading,
      firstLoad,
      setIsLoading,
      setFirstLoad,
      setFilterCommonOptions,
      setFilter,
      filter,
    } = projectsStore;
    const { selectedTreeNode, setExpandedKeys } = treeFoldersStore;
    const { fetchTasks, getTaskFilterCommonOptions } = tasksFilterStore;
    const {
      fetchAllProjects,
      projects,
      fetchProjects,
      getProjectFilterCommonOptions,
    } = projectsFilterStore;
    return {
      modules: auth.moduleStore.modules,
      isLoaded: auth.isLoaded,
      setCurrentProductId: auth.settingsStore.setCurrentProductId,
      homepage: config.homepage,
      fetchProjects,
      isLoading,
      firstLoad,
      setIsLoading,
      setFirstLoad,
      selectedTreeNode,
      fetchAllProjects,
      filter,
      projects,
      fetchTasks,
      setExpandedKeys,
      getProjectFilterCommonOptions,
      setFilterCommonOptions,
      getTaskFilterCommonOptions,
    };
  }
)(withRouter(withTranslation(["Home", "Common", "Article"])(Home)));

export default (props) => (
  <I18nextProvider i18n={i18n}>
    <HomeWrapper {...props} />
  </I18nextProvider>
);
