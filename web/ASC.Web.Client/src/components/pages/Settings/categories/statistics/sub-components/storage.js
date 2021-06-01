import React from "react";
import ProgressBar from "@appserver/components/progress-bar";
import Text from "@appserver/components/text";

const Storage = ({ quota, t }) => {
  const { storageSize, usedSize } = quota;
  const toUserValue = (value) => {
    const sizes = ["B", "Kb", "Mb", "Gb", "Tb"];
    if (value == 0) return "0 B";
    const i = parseInt(Math.floor(Math.log(value) / Math.log(1024)));
    return Math.round(value / Math.pow(1024, i), 2) + " " + sizes[i];
  };

  const storageSizeConverted = toUserValue(storageSize);
  const usedSizeConverted = toUserValue(usedSize);
  const storageUsedPercent = parseFloat(
    ((usedSize * 100) / storageSize).toFixed(2)
  );

  return (
    <div className="category-item-wrapper">
      <div className="category-item-heading">
        <Text className="inherit-title-link header" truncate={true}>
          {t("StorageTitle")}
        </Text>
      </div>
      <div className="storage-value-title">
        <div className="storage-value-current">{usedSizeConverted}</div>
        <div className="storage-value-max">{storageSizeConverted}</div>
        <ProgressBar percent={storageUsedPercent} />
      </div>
      <Text className="category-item-description">{t("StorageInfo")}</Text>
    </div>
  );
};

export default Storage;
