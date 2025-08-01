import React from 'react';
import { SignInButton } from '../src/components/MSAuthentication/SignInButton';
import Copilot from "../src/assets/AIAgent.svg";
import Background from "../src/assets/HomeBackground.jpg";

const HomePage: React.FC = () => {
    return (
        <div
            className="flex items-center justify-center min-h-screen bg-cover bg-center text-white relative overflow-y-auto px-4 py-10"
            style={{ backgroundImage: `url(${Background})` }}
        >
            {/* Animated Agent Images */}
            <img
                src={Copilot}
                alt="Agent"
                className="absolute bottom-4 right-4 w-20 md:w-32 animate-pulse opacity-80"
            />

            <div className="text-center p-4 md:p-8 rounded-2xl bg-white bg-opacity-20 backdrop-blur-lg shadow-2xl border border-white border-opacity-30 animate-fade-in max-w-4xl w-full">
                <h1 className="text-3xl md:text-5xl font-extrabold mb-4 text-black drop-shadow-lg">
                    Meet Your AI Agent
                </h1>
                <p className="text-lg md:text-xl mb-6 text-black drop-shadow-lg">
                    An intelligent AI Agent to boost your productivity.
                </p>

                {/* AI Capabilities Section */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 text-left bg-white bg-opacity-10 p-4 md:p-6 rounded-lg shadow-lg border border-white border-opacity-20">
                    {[
                        ['📄', 'Understand & answer queries based on shared documents.'],
                        ['📊', 'Perform intelligent analysis on Excel spreadsheets and extract key insights.'],
                        ['📧', 'Compose & send emails.'],
                        ['🌤️', 'Fetch real-time weather updates.'],
                        ['📝', 'Generate structured documents from custom templates you provide.'],
                        ['🧠', 'Summarize complex documents and meetings from video recordings.']
                    ].map(([icon, text], index) => (
                        <div key={index} className="flex items-start space-x-3">
                            <span className="text-2xl md:text-3xl">{icon}</span>
                            <p className="text-base md:text-lg text-black font-semibold">{text}</p>
                        </div>
                    ))}
                </div>

                <div className="mt-6">
                    <SignInButton />
                </div>
            </div>
        </div>
    );
};

export default HomePage;