import React from 'react';
import { SignInButton } from '../src/components/MSAuthentication/SignInButton';
import Copilot from "../src/assets/AIAgent.svg";
import Background from "../src/assets/HomeBackground.jpg";

const HomePage: React.FC = () => {
    return (
        <div className="flex items-center justify-center h-screen bg-cover bg-center text-white relative overflow-hidden" style={{ backgroundImage: `url(${Background})` }}>
            {/* Animated Agent Images */}
            <img src={Copilot} alt="Agent" className="absolute bottom-10 right-10 w-32 animate-pulse opacity-80" />
            
            <div className="text-center p-8 rounded-2xl bg-white bg-opacity-20 backdrop-blur-lg shadow-2xl border border-white border-opacity-30 animate-fade-in">
                <h1 className="text-5xl font-extrabold mb-4 text-black drop-shadow-lg">Meet Your AI Agent</h1>
                <p className="text-xl mb-6 text-black drop-shadow-lg">An intelligent AI Agent to boost your productivity.</p>
    
                {/* AI Capabilities Section */}
                <div className="grid grid-cols-2 gap-6 text-left max-w-2xl mx-auto bg-white bg-opacity-10 p-6 rounded-lg shadow-lg border border-white border-opacity-20">
                    <div className="flex items-center space-x-3">
                        <span className="text-3xl">📄</span>
                        <p className="text-lg text-black font-semibold">Understand & answer queries based on shared documents.</p>
                    </div>
                    <div className="flex items-center space-x-3">
                        <span className="text-3xl">📧</span>
                        <p className="text-lg text-black font-semibold">Compose & send emails.</p>
                    </div>
                    <div className="flex items-center space-x-3">
                        <span className="text-3xl">🌤️</span>
                        <p className="text-lg text-black font-semibold">Fetch real-time weather updates.</p>
                    </div>
                    <div className="flex items-center space-x-3">
                        <span className="text-3xl">📝</span>
                        <p className="text-lg text-black font-semibold">Generate structured documents from provided templates.</p>
                    </div>
                </div>
    
                <div className="mt-6">
                    <SignInButton />
                </div>
            </div>
        </div>
    );        
};

export default HomePage;