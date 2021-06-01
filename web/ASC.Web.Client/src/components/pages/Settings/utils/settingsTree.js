/**
 * Array for generation current settings tree.
 */

export const settingsTree = [
  {
    key: "0",
    icon: "/static/images/settings.react.svg",
    link: "common",
    tKey: "ManagementCategoryCommon",
    isHeader: true,
    children: [
      {
        key: "0-0",
        icon: "",
        link: "customization",
        tKey: "Customization",
        isCategory: true,
        children: [
          {
            key: "0-0-0",
            icon: "",
            link: "language-and-time-zone",
            tKey: "StudioTimeLanguageSettings",
          },
          {
            key: "0-0-1",
            icon: "",
            link: "custom-titles",
            tKey: "CustomTitles",
          },
          {
            key: "0-0-2",
            icon: "",
            link: "team-template",
            tKey: "TeamTemplate",
          },
        ],
      },
      // {
      //     key: '0-2',
      //     icon: '',
      //     link: 'whitelabel',
      //     tKey: 'WhiteLabel',
      // },
    ],
  },
  {
    key: "1",
    icon: "/images/security.react.svg",
    link: "security",
    tKey: "ManagementCategorySecurity",
    isHeader: true,
    children: [
      {
        key: "1-0",
        icon: "",
        link: "accessrights",
        tKey: "AccessRights",
        isCategory: true,
      },
    ],
  },
  {
    key: "3",
    icon: "/images/integration.react.svg",
    link: "integration",
    tKey: "ManagementCategoryIntegration",
    isHeader: true,
    children: [
      {
        key: "3-0",
        icon: "",
        link: "third-party-services",
        tKey: "ThirdPartyAuthorization",
        isCategory: true,
      },
    ],
  },
  {
    key: "4",
    icon: "/static/images/stats.react.svg",
    link: "statistics",
    tKey: "ManagementCategoryStatistic",
    isHeader: false,
    children: [
      {
        key: "4-0",
        icon: "",
        link: "",
        tKey: "ManagementCategoryStatistic",
        isCategory: true,
      },
    ],
  },
];

/**
* Array for generation full settings tree, old structure.
param title is unused
param link also used as translation key
*/
export const settingsTreeFull = [
  {
    title: "Common",
    key: "0",
    icon: "/static/images/settings.react.svg",
    link: "common",
    children: [
      {
        title: "Customization",
        key: "0-0",
        icon: "",
        link: "customization",
      },
      {
        title: "Modules & tools",
        key: "0-1",
        icon: "",
        link: "modules-and-tools",
      },
      {
        title: "White label",
        key: "0-2",
        icon: "",
        link: "white-label",
      },
    ],
  },
  {
    title: "Security",
    key: "1",
    icon: "/static/images/settings.react.svg",
    link: "security",
    children: [
      {
        title: "Portal Access",
        key: "1-0",
        icon: "",
        link: "portal-access",
      },
      {
        title: "Access Rights",
        key: "1-1",
        icon: "",
        link: "access-rights",
      },
      {
        title: "Login History",
        key: "1-2",
        icon: "",
        link: "login-history",
      },
      {
        title: "Audit Trail",
        key: "1-3",
        icon: "",
        link: "audit-trail",
      },
    ],
  },
  {
    title: "Data Management",
    key: "2",
    icon: "/static/images/settings.react.svg",
    link: "data-management",
    children: [
      {
        title: "Migration",
        key: "2-0",
        icon: "",
        link: "migration",
      },
      {
        title: "Backup",
        key: "2-1",
        icon: "",
        link: "backup",
      },
      {
        title: "Portal Deactivation/Deletion",
        key: "2-2",
        icon: "",
        link: "portal-deactivation-deletion",
      },
    ],
  },
  {
    title: "Integration",
    key: "3",
    icon: "/static/images/settings.react.svg",
    link: "integration",
    children: [
      {
        title: "Third-Party Services",
        key: "3-0",
        icon: "",
        link: "third-party-services",
      },
      {
        title: "SMTP Settings",
        key: "3-1",
        icon: "",
        link: "smtp-settings",
      },
    ],
  },
  {
    title: "Statistics",
    key: "4",
    icon: "/static/images/settings.react.svg",
    link: "statistics",
  },
];
