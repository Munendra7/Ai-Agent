import React from 'react';
import { SignInButton } from '../src/components/MSAuthentication/SignInButton';

const HomePage: React.FC = () => {
    return (
        <div className="flex items-center justify-center h-screen bg-cover bg-center text-white relative overflow-hidden" style={{ backgroundImage: "url('../src/assets/HomeBackground.jpg')" }}>
            {/* Animated Agent Images */}
            <img src="../src/assets/copilot.svg" alt="Agent 2" className="absolute bottom-10 right-10 w-32 animate-pulse opacity-80" />
            
            <div className="text-center p-8 rounded-2xl bg-white bg-opacity-20 backdrop-blur-lg shadow-2xl border border-white border-opacity-30 animate-fade-in">
                <h1 className="text-5xl font-extrabold mb-4 text-black drop-shadow-lg">Welcome to Your Custom Copilot Agent</h1>
                <p className="text-xl mb-6 text-black drop-shadow-lg">Enhance your productivity with AI-Agent</p>
                <SignInButton/>
            </div>
        </div>
    );
};

export default HomePage;