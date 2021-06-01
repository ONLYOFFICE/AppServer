import React from "react";
import Text from "@appserver/components/text";
const Users = ({ quota, t }) => {
  const { maxUsersCount, usersCount } = quota;

  return (
    <Text className="category-item-subheader">
      {t("ActiveUsers", {
        current: usersCount,
        total: maxUsersCount,
      })}
    </Text>
  );
};

export default Users;
