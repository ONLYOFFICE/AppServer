import React, { Component, useEffect } from "react";
import PropTypes from "prop-types";
import { withRouter } from "react-router";
import { store } from "asc-web-common";
import {
  Box,
  Button,
  TextInput,
  Text,
  Link,
  toastr,
  Checkbox,
  HelpButton,
  PasswordInput,
  FieldContainer,
} from "asc-web-components";
import PageLayout from "../../components/PageLayout";
import { connect } from "react-redux";
import styled from "styled-components";
import { withTranslation } from "react-i18next";
import i18n from "./i18n";
import ForgotPasswordModalDialog from "./sub-components/forgot-password-modal-dialog";
import {
  login,
  setIsLoaded,
  reloadPortalSettings,
} from "../../store/auth/actions";
import { sendInstructionsToChangePassword } from "../../api/people";
import Register from "./sub-components/register-container";
import { createPasswordHash } from "../../utils";
//import history from "../../history";
import { redirectToDefaultPage } from "../../utils";
const { getLanguage } = store.auth.selectors;
const LoginContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  margin: 120px auto 0 auto;
  max-width: 960px;

  @media (max-width: 768px) {
    padding: 0 16px;
    max-width: 475px;
  }
  @media (max-width: 375px) {
    margin: 72px auto 0 auto;
    max-width: 311px;
  }

  .greeting-title {
    width: 100%;

    @media (max-width: 768px) {
      text-align: left;
    }
    @media (max-width: 375px) {
      font-size: 23px;
    }
  }

  .auth-form-container {
    margin: 32px 213px 0 213px;
    width: 311px;

    @media (max-width: 768px) {
      margin: 32px 0 0 0;
      width: 100%;
    }
    @media (max-width: 375px) {
      margin: 32px 0 0 0;
      width: 100%;
    }

    .login-forgot-wrapper {
      height: 36px;
      padding: 14px 0;

      .login-checkbox-wrapper {
        position: absolute;
        display: inline-flex;

        .login-checkbox {
          float: left;
          span {
            font-size: 12px;
          }
        }

        .login-tooltip {
          display: inline-flex;

          @media (min-width: 1025px) {
            margin-left: 8px;
            margin-top: 4px;
          }
          @media (max-width: 1024px) {
            padding: 4px 8px 8px 8px;
          }
        }
      }

      .login-link {
        float: right;
        line-height: 16px;
      }
    }

    .login-button {
      margin-bottom: 16px;
    }

    .login-button-dialog {
      margin-right: 8px;
    }

    .login-bottom-border {
      width: 100%;
      height: 1px;
      background: #eceef1;
    }

    .login-bottom-text {
      margin: 0 8px;
    }
  }
`;

class Form extends Component {
  constructor(props) {
    super(props);

    this.state = {
      identifierValid: true,
      identifier: "",
      isLoading: false,
      isDisabled: false,
      passwordValid: true,
      password: "",
      isChecked: false,
      openDialog: false,
      email: "",
      emailError: false,
      errorText: "",
      socialButtons: [],
    };
  }

  onChangeLogin = (event) => {
    this.setState({ identifier: event.target.value });
    !this.state.identifierValid && this.setState({ identifierValid: true });
    this.state.errorText && this.setState({ errorText: "" });
  };

  onChangePassword = (event) => {
    this.setState({ password: event.target.value });
    !this.state.passwordValid && this.setState({ passwordValid: true });
    this.state.errorText && this.setState({ errorText: "" });
  };

  onChangeEmail = (event) => {
    this.setState({ email: event.target.value, emailError: false });
  };

  onChangeCheckbox = () => this.setState({ isChecked: !this.state.isChecked });

  onClick = () => {
    this.setState({
      openDialog: true,
      isDisabled: true,
      email: this.state.identifier,
    });
  };

  onKeyPress = (event) => {
    if (event.key === "Enter") {
      !this.state.isDisabled
        ? this.onSubmit()
        : this.onSendPasswordInstructions();
    }
  };

  onSendPasswordInstructions = () => {
    if (!this.state.email.trim()) {
      this.setState({ emailError: true });
    } else {
      this.setState({ isLoading: true });
      sendInstructionsToChangePassword(this.state.email)
        .then(
          (res) => toastr.success(res),
          (message) => toastr.error(message)
        )
        .finally(this.onDialogClose());
    }
  };

  onDialogClose = () => {
    this.setState({
      openDialog: false,
      isDisabled: false,
      isLoading: false,
      email: "",
      emailError: false,
    });
  };

  onSubmit = () => {
    const { errorText, identifier, password } = this.state;
    const { login, setIsLoaded, history, hashSettings, homepage } = this.props;

    errorText && this.setState({ errorText: "" });
    let hasError = false;

    const userName = identifier.trim();

    if (!userName) {
      hasError = true;
      this.setState({ identifierValid: !hasError });
    }

    const pass = password.trim();

    if (!pass) {
      hasError = true;
      this.setState({ passwordValid: !hasError });
    }

    if (hasError) return false;

    this.setState({ isLoading: true });
    const hash = createPasswordHash(pass, hashSettings);

    login(userName, hash)
      .then(() => {
        if (!redirectToDefaultPage()) {
          setIsLoaded(true);
        }
      })
      .catch((error) => {
        let err = error.data.error.message;
        this.setState({ errorText: err, isLoading: false });
      });
  };

  componentDidMount() {
    const {
      match,
      t,
      hashSettings,
      reloadPortalSettings,
      organizationName,
    } = this.props;
    const { error, confirmedEmail } = match.params;

    document.title = `${t("Authorization")} – ${organizationName}`; //TODO: implement the setDocumentTitle() utility in ASC.Web.Common

    error && this.setState({ errorText: error });
    confirmedEmail && this.setState({ identifier: confirmedEmail });
    window.addEventListener("keyup", this.onKeyPress);

    if (!hashSettings) {
      reloadPortalSettings();
    }
  }

  componentWillUnmount() {
    window.removeEventListener("keyup", this.onKeyPress);
  }

  settings = {
    minLength: 6,
    upperCase: false,
    digits: false,
    specSymbols: false,
  };

  render() {
    const { greetingTitle, match, t } = this.props;

    const {
      identifierValid,
      identifier,
      isLoading,
      passwordValid,
      password,
      isChecked,
      openDialog,
      email,
      emailError,
      errorText,
      socialButtons,
    } = this.state;
    const { params } = match;

    //console.log("Login render");

    return (
      <>
        <LoginContainer>
          <Text
            fontSize="32px"
            fontWeight={600}
            textAlign="center"
            className="greeting-title"
          >
            {greetingTitle}
          </Text>

          <form className="auth-form-container">
            <FieldContainer
              isVertical={true}
              labelVisible={false}
              hasError={!identifierValid}
              errorMessage={t("RequiredFieldMessage")}
            >
              <TextInput
                id="login"
                name="login"
                hasError={!identifierValid}
                value={identifier}
                placeholder={t("RegistrationEmailWatermark")}
                size="large"
                scale={true}
                isAutoFocussed={true}
                tabIndex={1}
                isDisabled={isLoading}
                autoComplete="username"
                onChange={this.onChangeLogin}
                onKeyDown={this.onKeyPress}
              />
            </FieldContainer>
            <FieldContainer
              isVertical={true}
              labelVisible={false}
              hasError={!passwordValid}
              errorMessage={t("RequiredFieldMessage")}
            >
              <PasswordInput
                simpleView={true}
                passwordSettings={this.settings}
                id="password"
                inputName="password"
                placeholder={t("Password")}
                type="password"
                hasError={!passwordValid}
                inputValue={password}
                size="large"
                scale={true}
                tabIndex={1}
                isDisabled={isLoading}
                autoComplete="current-password"
                onChange={this.onChangePassword}
                onKeyDown={this.onKeyPress}
              />
            </FieldContainer>
            <div className="login-forgot-wrapper">
              <div className="login-checkbox-wrapper">
                <Checkbox
                  className="login-checkbox"
                  isChecked={isChecked}
                  onChange={this.onChangeCheckbox}
                  label={<Text fontSize="13px">{t("Remember")}</Text>}
                />
                <HelpButton
                  className="login-tooltip"
                  helpButtonHeaderContent={t("CookieSettingsTitle")}
                  tooltipContent={
                    <Text fontSize="12px">{t("RememberHelper")}</Text>
                  }
                />
              </div>

              <Link
                fontSize="13px"
                color="#316DAA"
                className="login-link"
                type="page"
                isHovered={false}
                onClick={this.onClick}
              >
                {t("ForgotPassword")}
              </Link>
            </div>

            {openDialog && (
              <ForgotPasswordModalDialog
                openDialog={openDialog}
                isLoading={isLoading}
                email={email}
                emailError={emailError}
                onChangeEmail={this.onChangeEmail}
                onSendPasswordInstructions={this.onSendPasswordInstructions}
                onDialogClose={this.onDialogClose}
                t={t}
              />
            )}

            <Button
              id="button"
              className="login-button"
              primary
              size="large"
              scale={true}
              label={isLoading ? t("LoadingProcessing") : t("LoginButton")}
              tabIndex={1}
              isDisabled={isLoading}
              isLoading={isLoading}
              onClick={this.onSubmit}
            />

            {params.confirmedEmail && (
              <Text isBold={true} fontSize="16px">
                {t("MessageEmailConfirmed")} {t("MessageAuthorize")}
              </Text>
            )}
            <Text fontSize="14px" color="#c30">
              {errorText}
            </Text>

            {socialButtons.length ? (
              <Box displayProp="flex" alignItems="center">
                <div className="login-bottom-border"></div>
                <Text className="login-bottom-text" color="#A3A9AE">
                  {t("Or")}
                </Text>
                <div className="login-bottom-border"></div>
              </Box>
            ) : null}
          </form>
        </LoginContainer>
      </>
    );
  }
}

Form.propTypes = {
  login: PropTypes.func.isRequired,
  match: PropTypes.object.isRequired,
  //history: PropTypes.object.isRequired,
  setIsLoaded: PropTypes.func.isRequired,
  greetingTitle: PropTypes.string.isRequired,
  t: PropTypes.func.isRequired,
  i18n: PropTypes.object.isRequired,
  language: PropTypes.string.isRequired,
  socialButtons: PropTypes.array,
  organizationName: PropTypes.string,
  homepage: PropTypes.string,
};

Form.defaultProps = {
  identifier: "",
  password: "",
  email: "",
};

const FormWrapper = withTranslation()(Form);
const RegisterWrapper = withTranslation()(Register);

const LoginForm = (props) => {
  const { language, isLoaded, enabledJoin } = props;

  useEffect(() => {
    i18n.changeLanguage(language);
  }, [language]);

  return (
    <>
      {isLoaded && (
        <>
          <PageLayout>
            <PageLayout.SectionBody>
              <>
                <FormWrapper i18n={i18n} {...props} />
                {enabledJoin && <RegisterWrapper i18n={i18n} {...props} />}
              </>
            </PageLayout.SectionBody>
          </PageLayout>
        </>
      )}
    </>
  );
};

LoginForm.propTypes = {
  language: PropTypes.string.isRequired,
  isLoaded: PropTypes.bool,
  enabledJoin: PropTypes.bool,
};

function mapStateToProps(state) {
  const { isLoaded, settings } = state.auth;
  const {
    greetingSettings,
    enabledJoin,
    organizationName,
    hashSettings,
  } = settings;

  return {
    isLoaded,
    enabledJoin,
    organizationName,
    language: getLanguage(state),
    greetingTitle: greetingSettings,
    hashSettings,
  };
}

export default connect(mapStateToProps, {
  login,
  setIsLoaded,
  reloadPortalSettings,
})(withRouter(LoginForm));
