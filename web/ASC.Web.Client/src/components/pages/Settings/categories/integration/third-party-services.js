import React from "react";
import { consumers } from "./sub-components/consumers";
import { Box, Text, Link } from "asc-web-components";
import ConsumerItem from "./sub-components/consumerItem";
import { withTranslation } from 'react-i18next';
import { connect } from "react-redux";

class ThirdPartyServices extends React.Component {

    constructor(props) {
        super(props);

        const { t } = props;
        document.title = `${t("ThirdPartyAuthorization")} – ${t("OrganizationName")}`;

        this.state = {
            consumers: consumers,
            selectedConsumer: "",
            dialogVisible: false
        }
    }

    onModalOpen = () => {
        this.setState({
            dialogVisible: true,
        })
    }

    onModalClose = () => {
        this.setState({
            dialogVisible: false,
            selectedConsumer: ""
        })
    }

    onToggleClick = () => {
        this.onModalOpen();
    }

    titleDescription = "Ключи авторизации позволяют подключить портал ONLYOFFICE к сторонним сервисам, таким как Twitter, Facebook, Dropbox и т.д. Подключите портал к Facebook, Twitter или Linkedin, если Вы не хотите каждый раз при входе вводить свои учетные данные на портале. Привяжите портал к таким сервисам, как Dropbox, OneDrive и т.д. чтобы перенести документы из всех этих хранилищ в модуль Документы ONLYOFFICE."

    render() {

        const { t } = this.props;
        const { consumers, selectedConsumer, dialogVisible } = this.state;
        const { titleDescription, onModalClose, onToggleClick } = this;

        const refs = consumers.reduce((acc, consumer) => {
            acc[consumer.name] = React.createRef();
            return acc;
        }, []);

        return (
            <>
                <Box displayProp="flex" flexDirection="column">
                    <Box marginProp="10px">
                        <Text>
                            {titleDescription}
                            {" "}
                            <Link
                                isHovered={true}
                                target="_blank"
                                href="https://helpcenter.onlyoffice.com/ru/server/windows/community/authorization-keys.aspx">
                                {t("LearnMore")}
                            </Link>
                        </Text>
                    </Box>
                    <Box displayProp="flex">
                        <Box displayProp="flex" widthProp="100%">
                            {consumers
                                .map((consumer, i) =>
                                    <ConsumerItem
                                        key={i}

                                        ref={el => (refs[i] = el)}

                                        name={consumer.name}
                                        description={consumer.description}

                                        consumers={consumers}

                                        dialogVisible={dialogVisible}
                                        selectedConsumer={selectedConsumer}

                                        onModalClose={onModalClose}
                                        onToggleClick={() => {
                                            this.setState({ selectedConsumer: consumer.name })
                                            onToggleClick()
                                        }}
                                    />
                                )}
                        </Box>
                    </Box>
                </Box>
            </>
        )
    }
}

export default connect(null, null)(withTranslation()(ThirdPartyServices));