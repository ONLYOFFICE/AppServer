import React from "react";
import Chart from "./";

export default {
  title: "Components/Chart",
  component: Chart,
  parameters: {
    docs: {
      description: {
        component: `Chart component`,
      },
    },
  },
};

const getRandData = (items) => {
  return items.map(() => Math.floor(Math.random() * (Math.floor(100) - 0)) + 0);
};

const Template = (args) => {
  const labels = Array.from({ length: 7 }, (v, i) => "Jun " + i);

  const data = {
    labels: labels,
    datasets: [
      {
        label: "Visits",
        data: getRandData(labels),
      },
    ],
  };

  return (
    <div style={{ height: "50vh" }}>
      <Chart data={data} />
    </div>
  );
};

export const Default = Template.bind({});
