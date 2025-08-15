import React from "react";
import { SignInButton } from "../src/components/MSAuthentication/SignInButton";
import Typewriter from "typewriter-effect";
import Background from "../src/assets/HomeBackground.jpg";

const HomePage: React.FC = () => {
  const capabilities = [
    ["📄", "Document Q&A", "Understand & answer queries based on shared documents."],
    ["🧠", "Smart Summaries", "Summarize complex documents and video files into key points."],
    ["🌐", "Web Search", "Search the web in real-time to answer your queries."],
    ["📝", "Template Docs", "Generate structured documents from templates effortlessly."],
    ["📊", "Excel Insights", "Analyze spreadsheets and extract actionable insights."],
    ["📧", "Send Emails", "Compose and send intelligent, context-aware emails."],
    ["🌤️", "Weather Updates", "Fetch up-to-the-minute weather forecasts."]
  ];

  return (
    <div className="overflow-x-hidden text-white">
      {/* HERO SECTION */}
      <section
        className="relative min-h-screen flex items-center justify-center text-center px-6 bg-cover bg-center"
        style={{
          backgroundImage: `linear-gradient(to bottom right, rgba(0,0,0,0.7), rgba(10,10,20,0.9)), url(${Background})`,
        }}
      >
        <div className="max-w-4xl mx-auto z-10">
          <h1 className="text-5xl md:text-6xl font-extrabold leading-tight animate-fade-up">
            Your <span className="text-indigo-400">Intelligent CoPilot</span> for Every Task
          </h1>

          {/* Colorful Animated Typing Effect */}
          <div className="mt-4 text-2xl md:text-3xl font-bold animate-fade-up delay-150">
            <Typewriter
              options={{
                strings: [
                  'Document Q&A',
                  'Smart Summaries',
                  'Web Search',
                  'Template Docs',
                  'Send Emails',
                  'Weather Updates',
                  'Excel Insights',
                ],
                autoStart: true,
                loop: true,
                delay: 60,
                deleteSpeed: 40,
                cursor: '<span class="gradient-text">|</span>',
                wrapperClassName: "gradient-text"
              }}
            />
          </div>

          <p className="mt-6 text-lg md:text-xl text-gray-400 animate-fade-up delay-300">
            AI Agent that works alongside you — smart, fast, and always ready.
          </p>

          <div className="mt-8 animate-fade-up delay-500">
            <SignInButton />
          </div>
        </div>

        {/* Decorative wave divider */}
        <div className="absolute bottom-0 left-0 w-full overflow-hidden leading-[0] rotate-180">
          <svg viewBox="0 0 1200 120" preserveAspectRatio="none" className="relative block w-full h-20">
            <path
              d="M321.39 56.44c58.43 0 117.16-15.36 175.6-15.36s117.16 15.36 175.6 15.36 117.16-15.36 175.6-15.36 117.16 15.36 175.6 15.36 117.16-15.36 175.6-15.36v79.11H0V41.08c58.43 0 117.16 15.36 175.6 15.36s117.16-15.36 175.6-15.36z"
              className="fill-gray-900"
            ></path>
          </svg>
        </div>
      </section>

      {/* FEATURES SECTION */}
      <section className="bg-gray-900 py-20 px-6">
        <div className="max-w-6xl mx-auto text-center">
          <h2 className="text-4xl font-bold mb-12 animate-fade-up">What Can My AI Agent Do?</h2>
          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
            {capabilities.map(([icon, title, desc], idx) => (
              <div
                key={idx}
                className="bg-gradient-to-br from-gray-800 to-gray-700 p-8 rounded-3xl shadow-lg transform hover:-translate-y-3 hover:shadow-indigo-500/30 transition-all duration-500 ease-out opacity-0 animate-fade-up"
                style={{ animationDelay: `${idx * 150 + 200}ms` }}
              >
                <div className="w-16 h-16 mx-auto rounded-full bg-indigo-500/20 flex items-center justify-center text-3xl mb-4">
                  {icon}
                </div>
                <h3 className="text-xl font-semibold text-indigo-300 mb-2">{title}</h3>
                <p className="text-gray-400">{desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* FOOTER */}
      <footer className="bg-black py-6 text-center text-gray-500 text-sm">
        © {new Date().getFullYear()} AI Agent — All Rights Reserved
      </footer>

      {/* Animations & Gradient Text */}
      <style>{`
        .gradient-text {
          background: linear-gradient(90deg, #ff7eb3, #ff758c, #42a5f5, #7e57c2, #ff7eb3);
          background-size: 300% 300%;
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
          animation: gradientShift 5s ease infinite;
          text-shadow: 0 0 8px rgba(255, 255, 255, 0.3);
        }
        @keyframes gradientShift {
          0% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
          100% { background-position: 0% 50%; }
        }
        @keyframes fade-up {
          0% { opacity: 0; transform: translateY(30px); }
          100% { opacity: 1; transform: translateY(0); }
        }
        .animate-fade-up {
          animation: fade-up 0.8s ease forwards;
        }
        .delay-150 { animation-delay: 0.15s; }
        .delay-300 { animation-delay: 0.3s; }
        .delay-500 { animation-delay: 0.5s; }
      `}</style>
    </div>
  );
};

export default HomePage;