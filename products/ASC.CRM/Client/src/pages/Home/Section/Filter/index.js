import React from "react";
import find from "lodash/find";
import result from "lodash/result";
import Loaders from "@appserver/common/components/Loaders";
import { inject, observer } from "mobx-react";
import { withTranslation } from "react-i18next";
import { isMobileOnly } from "react-device-detect";
import FilterInput from "@appserver/common/components/FilterInput";
import { withLayoutSize } from "@appserver/common/utils";
import { withRouter } from "react-router";
import moment from "moment";

const SectionFilterContent = ({
  sectionWidth,
  isLoaded,
  tReady,
  t,
  filter,
  getContactsList,
  customNames,
  user,
}) => {
  const getManagerType = (filterValues) => {
    const responsibleid = result(
      find(filterValues, (value) => {
        return value.group === "filter-manager";
      }),
      "key"
    );

    return responsibleid ? responsibleid : null;
  };

  const getAccessibilityType = (filterValues) => {
    const isShared = result(
      find(filterValues, (value) => {
        return value.group === "filter-access";
      }),
      "key"
    );

    return isShared
      ? isShared === "filter-access-public"
        ? "true"
        : "false"
      : null;
  };

  const getOtherType = (filterValues) => {};

  const getDateType = (filterValues) => {
    const lastMonthStart = moment()
      .subtract(1, "months")
      .date(1)
      .set({ hour: 0, minute: 0, second: 0, millisecond: 0 })
      .format();

    const lastMonthEnd = moment()
      .subtract(1, "months")
      .endOf("month")
      .set({ hour: 0, minute: 0, second: 0, millisecond: 0 })
      .format();

    const yesterday = moment()
      .subtract(1, "days")
      .set({ hour: 0, minute: 0, second: 0, millisecond: 0 })
      .format();

    const today = moment()
      .set({ hour: 0, minute: 0, second: 0, millisecond: 0 })
      .format();
    const thisMonth = moment()
      .startOf("month")
      .set({ hour: 0, minute: 0, second: 0, millisecond: 0 })
      .format();

    const date = result(
      find(filterValues, (value) => {
        return value.group === "filter-creation-date";
      }),
      "key"
    );

    return date
      ? date === "filter-last-month"
        ? {
            fromDate: lastMonthStart,
            toDate: lastMonthEnd,
          }
        : date === "filter-yesterday"
        ? {
            fromDate: yesterday,
            toDate: yesterday,
          }
        : date === "filter-today"
        ? {
            fromDate: today,
            toDate: today,
          }
        : { fromDate: thisMonth, toDate: today }
      : { fromDate: null, toDate: null };
  };

  const getContactListViewType = (filterValues) => {
    const contactListView = result(
      find(filterValues, (value) => {
        return value.group === "filter-show";
      }),
      "key"
    );

    return contactListView
      ? contactListView === "filter-show-company"
        ? "company"
        : contactListView === "filter-show-person"
        ? "person"
        : "withopportunity"
      : null;
  };

  const getSelectedFilterData = () => {
    const selectedFilterData = {
      filterValues: [],
      sortDirection: filter.sortOrder === "ascending" ? "asc" : "desc",
      sortId: filter.sortBy,
    };

    selectedFilterData.inputValue = filter.search;

    if (filter.isShared) {
      selectedFilterData.filterValues.push({
        key: `${filter.isShared}`,
        group: "filter-access",
      });
    }

    if (filter.contactListView) {
      selectedFilterData.filterValues.push({
        key: `${filter.contactListView}`,
        group: "filter-show",
      });
    }

    return selectedFilterData;
  };

  const selectedFilterData = getSelectedFilterData();

  const onFilter = (data) => {
    const responsibleid = getManagerType(data.filterValues);
    const isShared = getAccessibilityType(data.filterValues);
    const contactListView = getContactListViewType(data.filterValues);
    const { fromDate, toDate } = getDateType(data.filterValues);
    const sortBy = data.sortId;
    const sortOrder =
      data.sortDirection === "desc" ? "descending" : "ascending";
    const search = data.inputValue || "";

    const newFilter = filter.clone();
    newFilter.sortBy = sortBy;
    newFilter.sortOrder = sortOrder;
    newFilter.responsibleid = responsibleid;
    newFilter.isShared = isShared;
    newFilter.contactListView = contactListView;
    newFilter.search = search;
    newFilter.fromDate = fromDate;
    newFilter.toDate = toDate;

    getContactsList(newFilter);
  };

  const getData = () => {
    const { groupsCaption } = customNames;
    const options = [
      {
        key: "filter-manager",
        group: "filter-manager",
        label: t("Manager"),
        isHeader: true,
      },
      {
        key: "filter-my-manager",
        group: "filter-manager",
        label: t("My"),
        isSelector: true,
        defaultOptionLabel: t("Common:MeLabel"),
        defaultSelectLabel: t("Common:Select"),
        groupsCaption,
        defaultOption: user,
        selectedItem: {},
      },
      {
        key: "group",
        group: "filter-author",
        label: groupsCaption,
        defaultSelectLabel: t("Common:Select"),
        isSelector: true,
        selectedItem: {},
      },
      // {
      //   key: "filter-no-manager",
      //   group: "filter-manager",
      //   label: t("NoContactManager"),
      //   isSelector: true,
      // },
      // {
      //   key: "filter-custom-manager",
      //   group: "filter-manager",
      //   label: t("Custom"),
      //   isSelector: true,
      // },
      {
        key: "filter-access",
        group: "filter-access",
        label: t("Accessibility"),
        isRowHeader: true,
      },
      {
        key: "filter-access-public",
        group: "filter-access",
        label: t("Public"),
      },
      {
        key: "filter-access-restricted",
        group: "filter-access",
        label: t("Restricted"),
      },
      {
        key: "filter-other",
        group: "filter-Other",
        label: t("Other"),
        isRowHeader: true,
      },
      {
        key: "filter-other-temperature-level",
        group: "filter-other",
        label: t("TemperatureLevel"),
      },
      {
        key: "filter-other-contact-type",
        group: "filter-other",
        label: t("ContactType"),
      },
      {
        key: "filter-other-with-tag",
        group: "filter-other",
        label: t("WithTag"),
      },
    ];

    const filterOptions = [
      ...options,
      {
        key: "filter-creation-date",
        group: "filter-creation-date",
        label: t("CreationDate"),
        isHeader: true,
      },
      {
        key: "filter-last-month",
        group: "filter-creation-date",
        label: t("LastMonth"),
      },
      {
        key: "filter-yesterday",
        group: "filter-creation-date",
        label: t("Yesterday"),
      },
      {
        key: "filter-today",
        group: "filter-creation-date",
        label: t("Today"),
      },
      {
        key: "filter-this-month",
        group: "filter-creation-date",
        label: t("ThisMonth"),
      },
      {
        key: "filter-show",
        group: "filter-show",
        label: t("Show"),
        isRowHeader: true,
      },
      {
        key: "filter-show-company",
        group: "filter-show",
        label: t("Companies"),
      },
      {
        key: "filter-show-person",
        group: "filter-show",
        label: t("Persons"),
      },
      {
        key: "filter-show-with-opportunity",
        group: "filter-show",
        label: t("WithOpportunities"),
      },
    ];

    return filterOptions;
  };

  const getSortData = () => {
    const commonOptions = [
      { key: "created", label: t("ByCreationDate"), default: true },
      { key: "displayname", label: t("ByTitle"), default: true },
      {
        key: "contacttype",
        label: t("ByTemperatureLevel"),
        default: true,
      },
      { key: "history", label: t("ByHistory"), default: true },
    ];

    const viewSettings = [
      { key: "row", label: t("ViewList"), isSetting: true, default: true },
      { key: "tile", label: t("ViewTiles"), isSetting: true, default: true },
    ];

    return window.innerWidth < 460
      ? [...commonOptions, ...viewSettings]
      : commonOptions;
  };

  const filterColumnCount =
    window.innerWidth < 500 ? {} : { filterColumnCount: 2 };

  return isLoaded && tReady ? (
    <FilterInput
      sectionWidth={sectionWidth}
      getFilterData={getData}
      getSortData={getSortData}
      selectedFilterData={selectedFilterData}
      onFilter={onFilter}
      directionAscLabel={t("Common:DirectionAscLabel")}
      directionDescLabel={t("Common:DirectionDescLabel")}
      placeholder={t("Common:Search")}
      {...filterColumnCount}
      contextMenuHeader={t("Common:AddFilter")}
      isMobile={isMobileOnly}
    />
  ) : (
    <Loaders.Filter />
  );
};

export default withRouter(
  inject(({ auth, crmStore, filterStore, contactsStore }) => {
    const { isLoaded } = crmStore;
    const { filter } = filterStore;
    const { getContactsList } = contactsStore;
    const { customNames } = auth.settingsStore;
    const { user } = auth.userStore;

    return {
      isLoaded,
      filter,
      getContactsList,
      customNames,
      user,
    };
  })(
    observer(
      withLayoutSize(withTranslation(["Home", "Common"])(SectionFilterContent))
    )
  )
);
