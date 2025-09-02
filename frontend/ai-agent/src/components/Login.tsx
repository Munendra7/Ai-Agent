import React, { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router-dom";
import { login, clearError, microsoftLoginMSAL } from "../features/auth/authSlice";
import authService from "../services/authService";
import { Loader2, Mail, Lock, AlertCircle } from "lucide-react";
import Copilot from "../assets/AIAgent.svg";
import { useAppDispatch, useAppSelector } from "../app/hooks";

const Login: React.FC = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isEmailFocused, setIsEmailFocused] = useState(false);
  const [isPasswordFocused, setIsPasswordFocused] = useState(false);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { isLoading, error } = useAppSelector(
    (state) => state.auth
  );

  useEffect(() => {
    return () => {
      dispatch(clearError());
    };
  }, [dispatch]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    dispatch(login({ email, password }));
  };

  const handleGoogleLogin = () => {
    window.location.href = authService.getGoogleAuthUrl();
  };

  const handleMicrosoftLogin = async () => {
    try {
      await dispatch(microsoftLoginMSAL()).unwrap();
      navigate("/chat/"+crypto.randomUUID());
    } catch (error) {
      console.error("Microsoft login failed:", error);
    }
  };

  // Google Logo SVG Component
  const GoogleLogo = () => (
    <svg className="w-5 h-5" viewBox="0 0 24 24">
      <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
      <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
      <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
      <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
    </svg>
  );

  // Microsoft Logo SVG Component
  const MicrosoftLogo = () => (
    <svg className="w-5 h-5" viewBox="0 0 24 24">
      <path fill="#f25022" d="M11.4 11.4H2.6V2.6h8.8v8.8z"/>
      <path fill="#00a4ef" d="M21.4 11.4h-8.8V2.6h8.8v8.8z"/>
      <path fill="#7fba00" d="M11.4 21.4H2.6v-8.8h8.8v8.8z"/>
      <path fill="#ffb900" d="M21.4 21.4h-8.8v-8.8h8.8v8.8z"/>
    </svg>
  );

  return (
    <>
      <style>{`
        @keyframes gradient-shift {
          0%, 100% { background-position: 0% 50%; }
          50% { background-position: 100% 50%; }
        }
        
        @keyframes float {
          0%, 100% { transform: translateY(0px) rotate(0deg); }
          50% { transform: translateY(-20px) rotate(10deg); }
        }
        
        @keyframes pulse-glow {
          0%, 100% { opacity: 0.6; }
          50% { opacity: 1; }
        }

        .gradient-animation {
          background-size: 200% 200%;
          animation: gradient-shift 15s ease infinite;
        }
        
        .floating-element {
          animation: float 6s ease-in-out infinite;
        }
        
        .glow-effect {
          animation: pulse-glow 3s ease-in-out infinite;
        }

        .glass-morphism {
          background: rgba(255, 255, 255, 0.05);
          backdrop-filter: blur(10px);
          -webkit-backdrop-filter: blur(10px);
        }

        .input-glow:focus {
          box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.5), 0 0 20px rgba(59, 130, 246, 0.2);
        }

        .hover-lift {
          transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        }

        .hover-lift:hover {
          transform: translateY(-2px);
          box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3);
        }
      `}</style>

      <div className="mt-2 min-h-screen flex items-center justify-center relative overflow-hidden bg-gradient-to-br from-gray-900 via-blue-900/20 to-gray-900 gradient-animation">
        {/* Animated Background Elements */}
        <div className="absolute inset-0 overflow-hidden">
          {/* Gradient Orbs */}
          <div className="absolute -top-40 -left-40 w-80 h-80 bg-blue-500/30 rounded-full blur-3xl glow-effect" />
          <div className="absolute -bottom-40 -right-40 w-80 h-80 bg-purple-500/30 rounded-full blur-3xl glow-effect" style={{ animationDelay: '1s' }} />
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-cyan-500/20 rounded-full blur-3xl glow-effect" style={{ animationDelay: '2s' }} />
          
          {/* Floating Particles */}
          <div className="absolute top-20 left-20 w-2 h-2 bg-blue-400 rounded-full floating-element" />
          <div className="absolute bottom-20 right-20 w-3 h-3 bg-purple-400 rounded-full floating-element" style={{ animationDelay: '2s' }} />
          <div className="absolute top-40 right-40 w-2 h-2 bg-cyan-400 rounded-full floating-element" style={{ animationDelay: '4s' }} />
        </div>

        <div className="relative z-10 w-full max-w-md p-6">
          <div className="glass-morphism p-8 rounded-3xl border border-white/10 shadow-2xl hover-lift">
            {/* Logo/Icon */}
            <div className="flex justify-center mb-2">
              <img src={Copilot} alt="AI Agent Logo" className="h-20 w-20" />
            </div>

            {/* Title */}
            <div className="text-center mb-8">
              <h2 className="text-3xl font-bold text-white mb-2 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                Welcome Back
              </h2>
              <p className="text-gray-400">
                Sign in to continue to{" "}
                <span className="font-semibold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
                  AI Agent
                </span>
              </p>
            </div>

            {/* Error Alert */}
            {error && (
              <div className="mb-6 p-4 rounded-xl bg-red-500/10 border border-red-500/30 flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-red-400 mt-0.5 flex-shrink-0" />
                <p className="text-sm text-red-400">{error}</p>
              </div>
            )}

            {/* Form */}
            <form className="space-y-5" onSubmit={handleSubmit}>
              {/* Email Input */}
              <div className="relative">
                <div className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${isEmailFocused ? 'text-blue-400' : 'text-gray-500'}`}>
                  <Mail className="w-5 h-5" />
                </div>
                <input
                  type="email"
                  required
                  placeholder="Email address"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  onFocus={() => setIsEmailFocused(true)}
                  onBlur={() => setIsEmailFocused(false)}
                  className="w-full pl-12 pr-4 py-4 bg-white/5 text-white placeholder-gray-500 border border-white/10 rounded-xl focus:bg-white/10 focus:border-blue-400/50 focus:outline-none input-glow transition-all"
                />
              </div>

              {/* Password Input */}
              <div className="relative">
                <div className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${isPasswordFocused ? 'text-blue-400' : 'text-gray-500'}`}>
                  <Lock className="w-5 h-5" />
                </div>
                <input
                  type="password"
                  required
                  placeholder="Password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  onFocus={() => setIsPasswordFocused(true)}
                  onBlur={() => setIsPasswordFocused(false)}
                  className="w-full pl-12 pr-4 py-4 bg-white/5 text-white placeholder-gray-500 border border-white/10 rounded-xl focus:bg-white/10 focus:border-blue-400/50 focus:outline-none input-glow transition-all"
                />
              </div>

              {/* Forgot Password Link */}
              {/* <div className="text-right">
                <Link to="/forgot-password" className="text-sm text-blue-400 hover:text-blue-300 transition-colors">
                  Forgot password?
                </Link>
              </div> */}

              {/* Submit Button */}
              <button
                type="submit"
                disabled={isLoading}
                className="cursor-pointer w-full py-4 rounded-xl bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 text-white font-semibold flex items-center justify-center transition-all transform hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none shadow-lg"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="animate-spin mr-2" size={20} />
                    Signing in...
                  </>
                ) : (
                  'Sign in'
                )}
              </button>
            </form>

            {/* Divider */}
            <div className="flex items-center gap-4 my-6">
              <div className="flex-1 h-px bg-gradient-to-r from-transparent via-white/20 to-transparent" />
              <span className="text-gray-500 text-sm font-medium">or continue with</span>
              <div className="flex-1 h-px bg-gradient-to-r from-transparent via-white/20 to-transparent" />
            </div>

            {/* Social Login Buttons */}
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={handleGoogleLogin}
                className="group relative flex items-center justify-center gap-2 py-3 px-4 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 hover:border-white/20 text-white font-medium transition-all hover-lift"
              >
                <GoogleLogo />
                <span>Google</span>
                <div className="cursor-pointer absolute inset-0 rounded-xl bg-gradient-to-r from-blue-400/0 via-red-400/0 to-yellow-400/0 group-hover:from-blue-400/10 group-hover:via-red-400/10 group-hover:to-yellow-400/10 transition-all" />
              </button>

              <button
                type="button"
                onClick={handleMicrosoftLogin}
                disabled={isLoading}
                className="cursor-pointer group relative flex items-center justify-center gap-2 py-3 px-4 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 hover:border-white/20 text-white font-medium transition-all disabled:opacity-50 disabled:cursor-not-allowed hover-lift"
              >
                <MicrosoftLogo />
                <span>Microsoft</span>
                <div className="absolute inset-0 rounded-xl bg-gradient-to-r from-blue-500/0 to-green-500/0 group-hover:from-blue-500/10 group-hover:to-green-500/10 transition-all" />
              </button>
            </div>

            {/* Footer */}
            <div className="mt-8 text-center">
              <span className="text-gray-400 text-sm">
                Don't have an account?{" "}
                <Link
                  to="/register"
                  className="text-blue-400 hover:text-blue-300 font-semibold transition-colors underline-offset-2 hover:underline"
                >
                  Sign up
                </Link>
              </span>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default Login;