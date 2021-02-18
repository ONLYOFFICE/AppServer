import React from "react";
import { connect } from "react-redux";
import { Router, Switch, Redirect } from "react-router-dom";
import history from "@appserver/common/src/history";
import PrivateRoute from "@appserver/common/src/components/PrivateRoute";
import Error404 from "@appserver/common/src/pages/errors/404";

import Home from "./components/pages/Home";
//import Profile from "./components/pages/Profile";
//import ProfileAction from "./components/pages/ProfileAction";
//import GroupAction from "./components/pages/GroupAction";
//import Reassign from "./components/pages/Reassign";

import { getFilterByLocation } from "./helpers/converters";
import { fetchGroups, fetchPeople } from "./store/people/actions";

import config from "../package.json";
const { homepage } = config;

const Routes = () => {
  return (
    <Router history={history}>
      <Switch>
        <Redirect exact from="/" to={`${homepage}`} />
        {/* <PrivateRoute
          exact
          path={`${homepage}/view/:userId`}
          component={Profile}
        />
        <PrivateRoute
          path={`${homepage}/edit/:userId`}
          restricted
          allowForMe
          component={ProfileAction}
        />
        <PrivateRoute
          path={`${homepage}/create/:type`}
          restricted
          component={ProfileAction}
        />
        <PrivateRoute
          path={[`${homepage}/group/edit/:groupId`, `${homepage}/group/create`]}
          restricted
          component={GroupAction}
        />
        <PrivateRoute
          path={`${homepage}/reassign/:userId`}
          restricted
          component={Reassign}
        /> */}
        <PrivateRoute exact path={homepage} component={Home} />
        <PrivateRoute path={`${homepage}/filter`} component={Home} />
        <PrivateRoute component={Error404} />
      </Switch>
    </Router>
  );
};

export default Routes;

// const mapStateToProps = (state) => {
//   const { settings } = state.auth;
//   const { homepage } = settings;
//   return {
//     homepage: homepage || config.homepage,
//   };
// };

// export default connect(mapStateToProps)(Routes);