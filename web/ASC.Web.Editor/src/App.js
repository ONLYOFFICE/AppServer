import React from "react";
import Editor from "./Editor";
import ErrorBoundary from "@appserver/common/components/ErrorBoundary";
import "@appserver/common/custom.scss";

const App = () => {
  return (
    <ErrorBoundary>
      <Shell />
    </ErrorBoundary>
  );
};

export default App;
