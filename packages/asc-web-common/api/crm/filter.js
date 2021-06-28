import { getObjectByLocation, toUrlParams } from "../../utils";

const DEFAULT_PAGE = 0;
const DEFAULT_PAGE_COUNT = 25;
const DEFAULT_TOTAL = 0;
const DEFAULT_START_INDEX = 0;
const DEFAULT_SORT_BY = "created";
const DEFAULT_SORT_ORDER = "descending";
const DEFAULT_CONTACT_STAGE = -1;
const DEFAULT_CONTACT_TYPE = -1;
const DEFAULT_SEARCH = "";
const DEFAULT_ACCESSIBILITY_TYPE = "";

const CONTACT_TYPE = "contactType";
const PAGE = "page";
const PAGE_COUNT = "count";
const SORT_BY = "sortBy";
const SORT_ORDER = "sortOrder";
const START_INDEX = "StartIndex";
const SEARCH = "search";
const CONTACT_STAGE = "contactStage";
const ACCESSIBILITY_TYPE = "isShared";

class CrmFilter {
  static getDefault() {
    return new CrmFilter({});
  }

  static getFilter(location) {
    if (!location) return this.getDefault();

    const urlFilter = getObjectByLocation(location);

    if (!urlFilter) return null;

    const defaultFilter = CrmFilter.getDefault();

    const sortBy = urlFilter[SORT_BY] || defaultFilter.sortBy;
    const sortOrder = urlFilter[SORT_ORDER] || defaultFilter.sortOrder;
    const pageCount =
      (urlFilter[PAGE_COUNT] && +urlFilter[PAGE_COUNT]) ||
      defaultFilter.pageCount;
    const startIndex = urlFilter[START_INDEX] || defaultFilter.startIndex;
    const total = defaultFilter.total;
    const isShared = urlFilter[ACCESSIBILITY_TYPE] || defaultFilter.isShared;

    const newFilter = new CrmFilter({
      sortBy,
      sortOrder,
      startIndex,
      pageCount,
      total,
      isShared,
    });

    return newFilter;
  }

  constructor({
    sortBy = DEFAULT_SORT_BY,
    sortOrder = DEFAULT_SORT_ORDER,
    startIndex = DEFAULT_START_INDEX,
    pageCount = DEFAULT_PAGE_COUNT,
    total = DEFAULT_TOTAL,
    page = DEFAULT_PAGE,
    contactType = DEFAULT_CONTACT_TYPE,
    search = DEFAULT_SEARCH,
    isShared = DEFAULT_ACCESSIBILITY_TYPE,
  }) {
    this.page = page;
    this.pageCount = pageCount;
    this.sortBy = sortBy;
    this.sortOrder = sortOrder;
    this.startIndex = startIndex;
    this.contactType = contactType;
    this.search = search;
    this.total = total;
    this.isShared = isShared;
  }

  toApiUrlParams = () => {
    const { page, pageCount, sortBy, sortOrder, startIndex, isShared } = this;

    let dtoFilter = {
      StartIndex: startIndex,
      sortBy: sortBy,
      pageCount: pageCount,
      sortOrder: sortOrder,
      Count: pageCount,
      isShared: isShared,
    };

    const str = toUrlParams(dtoFilter, false);
    return str;
  };

  toUrlParams = () => {
    const { sortBy, sortOrder, pageCount, startIndex, isShared } = this;
    const dtoFilter = {};

    if (isShared) {
      dtoFilter[ACCESSIBILITY_TYPE] = isShared;
    }

    dtoFilter[SORT_BY] = sortBy;
    dtoFilter[SORT_ORDER] = sortOrder;
    dtoFilter[START_INDEX] = startIndex;
    dtoFilter[PAGE_COUNT] = pageCount;

    const str = toUrlParams(dtoFilter, false);

    return str;
  };

  clone() {
    return new CrmFilter({
      page: this.page,
      pageCount: this.pageCount,
      total: this.total,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
      startIndex: this.startIndex,
      contactType: this.contactType,
      search: this.search,
      total: this.total,
      // isShared: this.isShared
    });
  }
}

export default CrmFilter;
