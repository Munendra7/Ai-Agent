import React from 'react';
import { SignInButton } from '../src/components/MSAuthentication/SignInButton';
import Copilot from "../src/assets/AIAgent.svg";
import Background from "../src/assets/HomeBackground.jpg";

const HomePage: React.FC = () => {
    const capabilities = [
        ['📄', 'Understand & answer queries based on shared documents.'],
        ['📊', 'Analyze Excel spreadsheets & extract insights.'],
        ['📧', 'Compose and send intelligent emails.'],
        ['🌤️', 'Fetch real-time weather updates.'],
        ['📝', 'Generate structured documents from templates.'],
        ['🧠', 'Summarize complex documents and video files.']
    ];

    return (
        <div
            className="min-h-screen overflow-y-auto text-white bg-black bg-opacity-80 relative"
            style={{
                backgroundImage: `linear-gradient(to bottom, rgba(0,0,0,0.7), rgba(0,0,0,0.8)), url(${Background})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center',
            }}
        >
            {/* Animated Agent Image */}
            <img
                src={Copilot}
                alt="AI Agent"
                className="absolute bottom-4 right-4 w-20 md:w-28 animate-bounce opacity-70 z-10"
            />

            {/* Main Content */}
            <div className="flex items-center justify-center px-4 py-16 md:py-24 relative z-20">
                <div className="text-center p-6 md:p-10 rounded-3xl bg-black/40 backdrop-blur-lg shadow-2xl border border-gray-700 max-w-6xl w-full animate-fade-in">
                    <h1 className="text-4xl md:text-5xl font-extrabold mb-3 text-white drop-shadow-lg animate-fade-up">
                        Welcome to <span className="text-indigo-400">CoThink</span>
                    </h1>
                    <p className="text-lg md:text-xl mb-8 text-gray-300 font-medium animate-fade-up delay-100">
                        Your cognitive partner in every task.
                    </p>


                    {/* Capabilities Grid */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6 mb-8 animate-fade-up delay-200">
                        {capabilities.map(([icon, desc], idx) => (
                            <div
                                key={idx}
                                className="flex flex-col items-center text-center p-6 rounded-2xl bg-gray-900/70 text-white shadow-md hover:shadow-xl transform hover:scale-[1.03] transition-all duration-300 ease-in-out border border-gray-700 backdrop-blur-md animate-fade-in-up"
                                style={{ animationDelay: `${idx * 100}ms` }}
                            >
                                <div className="w-14 h-14 rounded-full bg-indigo-500/20 text-indigo-300 flex items-center justify-center text-3xl mb-4 shadow-inner">
                                    {icon}
                                </div>
                                <p className="text-base font-semibold text-gray-200">{desc}</p>
                            </div>
                        ))}
                    </div>

                    <div className="animate-fade-up delay-300">
                        <SignInButton />
                    </div>
                </div>
            </div>
        </div>
    );
};

export default HomePage;
