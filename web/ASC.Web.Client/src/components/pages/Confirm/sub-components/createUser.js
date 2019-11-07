import React from 'react';
import { withRouter } from "react-router";
import { withTranslation } from 'react-i18next';
import { Button, TextInput, PageLayout, Text, PasswordInput, toastr, Loader } from 'asc-web-components';
import styled from 'styled-components';
import { Collapse } from 'reactstrap';
import { connect } from 'react-redux';
import { getConfirmationInfo, createConfirmUser, logout, login } from '../../../../store/auth/actions';
import PropTypes from 'prop-types';

const inputWidth = '400px';

const ConfirmContainer = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    margin-left: 200px;

    @media (max-width: 830px) {
            margin-left: 40px;
        }

    .start-basis {
        align-items: flex-start;
    }
    
    .margin-left {
        margin-left: 20px;
    }

    .full-width {
        width: ${inputWidth}
    }

    .confirm-row {
        margin: 23px 0 0;
    }

    .break-word {
        word-break: break-word;
    }

`;

const emailInputName = 'email';
const passwordInputName = 'password';

const emailRegex = '[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+.[a-zA-Z]{2,}$';
const validationEmail = new RegExp(emailRegex);

class Confirm extends React.PureComponent {

    constructor(props) {
        super(props);

        this.state = {
            email: '',
            emailValid: true,
            firstName: '',
            firstNameValid: true,
            lastName: '',
            lastNameValid: true,
            password: '',
            passwordValid: true,
            errorText: '',
            isLoading: false,
            passwordEmpty: false,
            key: props.linkData.confirmHeader,
            linkType: props.linkData.type
        };
    }

    /*componentWillMount() {
        const { isAuthenticated, logout } = this.props;

        if(isAuthenticated)
            logout();
    }*/

    onSubmit = () => {
        this.setState({ isLoading: true }, () => {
            const { history, createConfirmUser, linkData } = this.props;
            const isVisitor = parseInt(linkData.emplType) === 2;

            this.setState({ errorText: "" });

            let hasError = false;

            if (!this.state.firstName.trim()) {
                hasError = true;
                this.setState({ firstNameValid: !hasError });
            }

            if (!this.state.lastName.trim()) {
                hasError = true;
                this.setState({ lastNameValid: !hasError });
            }

            if (!validationEmail.test(this.state.email.trim())) {
                hasError = true;
                this.setState({ emailValid: !hasError });
            }

            if (!this.state.passwordValid) {
                hasError = true;
                this.setState({ passwordValid: !hasError });
            }

            !this.state.password.trim() && this.setState({ passwordEmpty: true });

            if (hasError) {
                this.setState({ isLoading: false });
                return false;
            }

            const loginData = {
                userName: this.state.email,
                password: this.state.password
            };

            const personalData = {
                firstname: this.state.firstName,
                lastname: this.state.lastName,
                email: this.state.email
            };
            const registerData = Object.assign(personalData, { isVisitor: isVisitor })

            createConfirmUser(registerData, loginData, this.state.key)
                .then(() => {
                    toastr.success("User has been created successfully");
                    return history.push('/');
                })
                .catch((error) => {
                    console.error("confirm error", error);
                    this.setState({
                        errorText: error,
                        isLoading: false
                    });
                });
        });
    };

    onKeyPress = (event) => {
        if (event.key === "Enter") {
            this.onSubmit();
        }
    };

    onCopyToClipboard = () => toastr.success(this.props.t('EmailAndPasswordCopiedToClipboard'));
    validatePassword = (value) => this.setState({ passwordValid: value });

    componentDidMount() {
        const { getConfirmationInfo, history } = this.props;

        getConfirmationInfo(this.state.key, this.state.linkType)
            .catch(e => {
                console.error("get settings error", e);
                history.push(`/login/error=${e}`);
            });

        window.addEventListener('keydown', this.onKeyPress);
        window.addEventListener('keyup', this.onKeyPress);
    }

    componentWillUnmount() {
        window.removeEventListener('keydown', this.onKeyPress);
        window.removeEventListener('keyup', this.onKeyPress);
    }

    onChangeName = event => {
        this.setState({ firstName: event.target.value });
        !this.state.firstNameValid && this.setState({ firstNameValid: event.target.value });
        this.state.errorText && this.setState({ errorText: "" });
    }

    onChangeSurname = event => {
        this.setState({ lastName: event.target.value });
        !this.state.lastNameValid && this.setState({ lastNameValid: true });
        this.state.errorText && this.setState({ errorText: "" });;
    }

    onChangeEmail = event => {
        this.setState({ email: event.target.value });
        !this.state.emailValid && this.setState({ emailValid: true });
        this.state.errorText && this.setState({ errorText: "" });;
    }

    onChangePassword = event => {
        this.setState({ password: event.target.value });
        !this.state.passwordValid && this.setState({ passwordValid: true });
        (event.target.value.trim()) && this.setState({ passwordEmpty: false });
        this.state.errorText && this.setState({ errorText: "" });
        this.onKeyPress(event);
    }

    render() {
        console.log('createUser render');
        const { settings, isConfirmLoaded, t, greetingTitle } = this.props;
        return (
            !isConfirmLoaded
                ? (
                    <Loader className="pageLoader" type="rombs" size={40} />
                )
                : (
                    <ConfirmContainer>
                        <div className='start-basis'>
                            <div className='margin-left'>
                                <Text.Body className='confirm-row' as='p' fontSize={18}>{t('InviteTitle')}</Text.Body>

                                <div className='confirm-row full-width break-word'>
                                    <a href='/login'>
                                        <img src="images/dark_general.png" alt="Logo" />
                                    </a>
                                    <Text.Body as='p' fontSize={24} color='#116d9d'>{greetingTitle}</Text.Body>
                                </div>
                            </div>

                            <div>
                                <div className='full-width'>

                                    <TextInput
                                        className='confirm-row'
                                        id='name'
                                        name='name'
                                        value={this.state.firstName}
                                        placeholder={t('FirstName')}
                                        size='huge'
                                        scale={true}
                                        tabIndex={1}
                                        isAutoFocussed={true}
                                        autoComplete='given-name'
                                        isDisabled={this.state.isLoading}
                                        hasError={!this.state.firstNameValid}
                                        onChange={this.onChangeName}
                                        onKeyDown={this.onKeyPress}
                                    />

                                    <TextInput
                                        className='confirm-row'
                                        id='surname'
                                        name='surname'
                                        value={this.state.lastName}
                                        placeholder={t('LastName')}
                                        size='huge'
                                        scale={true}
                                        tabIndex={2}
                                        autoComplete='family-name'
                                        isDisabled={this.state.isLoading}
                                        hasError={!this.state.lastNameValid}
                                        onChange={this.onChangeSurname}
                                        onKeyDown={this.onKeyPress}
                                    />

                                    <TextInput
                                        className='confirm-row'
                                        id='email'
                                        name={emailInputName}
                                        value={this.state.email}
                                        placeholder={t('Email')}
                                        size='huge'
                                        scale={true}
                                        tabIndex={3}
                                        autoComplete='email'
                                        isDisabled={this.state.isLoading}
                                        hasError={!this.state.emailValid}
                                        onChange={this.onChangeEmail}
                                        onKeyDown={this.onKeyPress}
                                    />

                                </div>

                                <PasswordInput
                                    className='confirm-row'
                                    id='password'
                                    inputName={passwordInputName}
                                    emailInputName={emailInputName}
                                    inputValue={this.state.password}
                                    placeholder={t('InvitePassword')}
                                    size='huge'
                                    scale={true}
                                    tabIndex={4}
                                    maxLength={30}
                                    inputWidth={inputWidth}
                                    hasError={this.state.passwordEmpty}
                                    onChange={this.onChangePassword}
                                    onCopyToClipboard={this.onCopyToClipboard}
                                    onValidateInput={this.validatePassword}
                                    clipActionResource={t('CopyEmailAndPassword')}
                                    clipEmailResource={`${t('Email')}: `}
                                    clipPasswordResource={`${t('InvitePassword')}: `}
                                    tooltipPasswordTitle={`${t('ErrorPasswordMessage')}:`}
                                    tooltipPasswordLength={`${t('ErrorPasswordLength', { fromNumber: 6, toNumber: 30 })}:`}
                                    tooltipPasswordDigits={t('ErrorPasswordNoDigits')}
                                    tooltipPasswordCapital={t('ErrorPasswordNoUpperCase')}
                                    tooltipPasswordSpecial={`${t('ErrorPasswordNoSpecialSymbols')} (!@#$%^&*)`}
                                    generatorSpecial="!@#$%^&*"
                                    passwordSettings={settings}
                                    isDisabled={this.state.isLoading}
                                    onKeyDown={this.onKeyPress}
                                />

                                <Button
                                    className='confirm-row'
                                    primary
                                    size='big'
                                    label={t('LoginRegistryButton')}
                                    tabIndex={5}
                                    isLoading={this.state.isLoading}
                                    onClick={this.onSubmit}
                                />

                            </div>

                            {/*             <Row className='confirm-row'>

                    <Text.Body as='p' fontSize={14}>{t('LoginWithAccount')}</Text.Body>

            </Row>
 */}
                            <Collapse className='confirm-row'
                                isOpen={!!this.state.errorText}>
                                <div className="alert alert-danger">{this.state.errorText}</div>
                            </Collapse>
                        </div>
                    </ConfirmContainer>
                )
        );
    }
}


Confirm.propTypes = {
    getConfirmationInfo: PropTypes.func.isRequired,
    createConfirmUser: PropTypes.func.isRequired,
    location: PropTypes.object.isRequired,
    history: PropTypes.object.isRequired
};
const CreateUserForm = (props) => (<PageLayout sectionBodyContent={<Confirm {...props} />} />);


function mapStateToProps(state) {
    return {
        isConfirmLoaded: state.auth.isConfirmLoaded,
        isAuthenticated: state.auth.isAuthenticated,
        settings: state.auth.settings.passwordSettings,
        greetingTitle: state.auth.settings.greetingSettings
    };
}

export default connect(mapStateToProps, { getConfirmationInfo, createConfirmUser, login, logout })(withRouter(withTranslation()(CreateUserForm)));