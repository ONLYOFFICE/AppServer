import React, { useEffect, useState } from "react";
import { observer, inject } from "mobx-react";
import { isMobile } from "react-device-detect";

import Loaders from "@appserver/common/components/Loaders";

let loadTimeout = null;
export default function withLoader(WrappedComponent, type) {
  const withLoader = (props) => {
    const { tReady, firstLoad, isLoaded, isLoading } = props;
    const [inLoad, setInLoad] = useState(false);

    const cleanTimer = () => {
      loadTimeout && clearTimeout(loadTimeout);
      loadTimeout = null;
    };

    useEffect(() => {
      if (isLoading) {
        cleanTimer();
        loadTimeout = setTimeout(() => {
          console.log("inLoad", true);
          setInLoad(true);
        }, 500);
      } else {
        cleanTimer();
        console.log("inLoad", false);
        setInLoad(false);
      }

      return () => {
        cleanTimer();
      };
    }, [isLoading]);

    return firstLoad || !isLoaded || (isMobile && inLoad) || !tReady ? (
      <Loaders.Rows />
    ) : (
      <WrappedComponent {...props} />
    );
  };

  return inject(({ auth, filesStore }) => {
    const { firstLoad, isLoading } = filesStore;
    return {
      firstLoad,
      isLoaded: auth.isLoaded,
      isLoading,
    };
  })(observer(withLoader));
}
