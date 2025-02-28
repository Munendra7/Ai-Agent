import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import React from 'react';
import LoginPage from './HomePage';
import ChatPlayground from '../src/components/ChatPlayground';

const AgentPage:React.FunctionComponent<{}> = () => {
    return(
    <>
        <AuthenticatedTemplate>
            {/* <ProfileContent /> */}
            <ChatPlayground/>
        </AuthenticatedTemplate>
        <UnauthenticatedTemplate>
            <LoginPage/>
        </UnauthenticatedTemplate>
    </>
    );
};

export default AgentPage;