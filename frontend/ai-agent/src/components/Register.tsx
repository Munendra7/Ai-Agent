import React, { useState, useEffect } from "react";
import { useNavigate, Link } from "react-router-dom";
import { register, clearError } from "../features/auth/authSlice";
import { Loader2, Mail, Lock, AlertCircle, Sparkles, User } from "lucide-react";
import { useAppDispatch, useAppSelector } from "../app/hooks";

const Register: React.FC = () => {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName]   = useState("");
  const [email, setEmail]         = useState("");
  const [password, setPassword]   = useState("");

  const [isFirstFocused, setIsFirstFocused]   = useState(false);
  const [isLastFocused, setIsLastFocused]     = useState(false);
  const [isEmailFocused, setIsEmailFocused]   = useState(false);
  const [isPasswordFocused, setIsPasswordFocused] = useState(false);

  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { isLoading, error } = useAppSelector((state) => state.auth);

  useEffect(() => {
    return () => {
      dispatch(clearError());
    };
  }, [dispatch]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (password.length < 6) {
      alert("Password must be at least 6 characters long");
      return;
    }
    try {
      await dispatch(register({ firstName, lastName, email, password })).unwrap();
      navigate("/chat");
    } catch (err) {
      console.error("Registration failed:", err);
    }
  };

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
        .gradient-animation { background-size: 200% 200%; animation: gradient-shift 15s ease infinite; }
        .floating-element { animation: float 6s ease-in-out infinite; }
        .glow-effect { animation: pulse-glow 3s ease-in-out infinite; }
        .glass-morphism { background: rgba(255, 255, 255, 0.05); backdrop-filter: blur(10px); -webkit-backdrop-filter: blur(10px); }
        .input-glow:focus { box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.5), 0 0 20px rgba(59, 130, 246, 0.2); }
        .hover-lift { transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1); }
        .hover-lift:hover { transform: translateY(-2px); box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3); }
      `}</style>

      <div className="min-h-screen flex items-center justify-center relative overflow-hidden bg-gradient-to-br from-gray-900 via-blue-900/20 to-gray-900 gradient-animation">
        {/* Background */}
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-40 -left-40 w-80 h-80 bg-blue-500/30 rounded-full blur-3xl glow-effect" />
          <div className="absolute -bottom-40 -right-40 w-80 h-80 bg-purple-500/30 rounded-full blur-3xl glow-effect" style={{ animationDelay: '1s' }} />
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-cyan-500/20 rounded-full blur-3xl glow-effect" style={{ animationDelay: '2s' }} />
          <div className="absolute top-20 left-20 w-2 h-2 bg-blue-400 rounded-full floating-element" />
          <div className="absolute bottom-20 right-20 w-3 h-3 bg-purple-400 rounded-full floating-element" style={{ animationDelay: '2s' }} />
          <div className="absolute top-40 right-40 w-2 h-2 bg-cyan-400 rounded-full floating-element" style={{ animationDelay: '4s' }} />
        </div>

        <div className="relative z-10 w-full max-w-md p-6">
          <div className="glass-morphism p-8 rounded-3xl border border-white/10 shadow-2xl hover-lift">
            {/* Logo */}
            <div className="flex justify-center mb-8">
              <div className="relative">
                <div className="w-20 h-20 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl flex items-center justify-center shadow-lg transform rotate-3 hover:rotate-6 transition-transform">
                  <Sparkles className="w-10 h-10 text-white" />
                </div>
                <div className="absolute -bottom-1 -right-1 w-20 h-20 bg-gradient-to-br from-blue-500/20 to-purple-600/20 rounded-2xl blur-xl" />
              </div>
            </div>

            {/* Title */}
            <div className="text-center mb-8">
              <h2 className="text-3xl font-bold text-white mb-2 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                Create Account
              </h2>
              <p className="text-gray-400">
                Join{" "}
                <span className="font-semibold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
                  AI Agent
                </span>{" "}
                today
              </p>
            </div>

            {/* Error */}
            {error && (
              <div className="mb-6 p-4 rounded-xl bg-red-500/10 border border-red-500/30 flex items-start gap-3">
                <AlertCircle className="w-5 h-5 text-red-400 mt-0.5 flex-shrink-0" />
                <p className="text-sm text-red-400">{error}</p>
              </div>
            )}

            {/* Form */}
            <form className="space-y-5" onSubmit={handleSubmit}>
              {/* First Name */}
              <div className="relative">
                <div className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${isFirstFocused ? "text-blue-400" : "text-gray-500"}`}>
                  <User className="w-5 h-5" />
                </div>
                <input
                  type="text"
                  required
                  placeholder="First Name"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  onFocus={() => setIsFirstFocused(true)}
                  onBlur={() => setIsFirstFocused(false)}
                  className="w-full pl-12 pr-4 py-4 bg-white/5 text-white placeholder-gray-500 border border-white/10 rounded-xl focus:bg-white/10 focus:border-blue-400/50 focus:outline-none input-glow transition-all"
                />
              </div>

              {/* Last Name */}
              <div className="relative">
                <div className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${isLastFocused ? "text-blue-400" : "text-gray-500"}`}>
                  <User className="w-5 h-5" />
                </div>
                <input
                  type="text"
                  required
                  placeholder="Last Name"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  onFocus={() => setIsLastFocused(true)}
                  onBlur={() => setIsLastFocused(false)}
                  className="w-full pl-12 pr-4 py-4 bg-white/5 text-white placeholder-gray-500 border border-white/10 rounded-xl focus:bg-white/10 focus:border-blue-400/50 focus:outline-none input-glow transition-all"
                />
              </div>

              {/* Email */}
              <div className="relative">
                <div className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${isEmailFocused ? "text-blue-400" : "text-gray-500"}`}>
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

              {/* Password */}
              <div className="relative">
                <div className={`absolute left-4 top-1/2 -translate-y-1/2 transition-colors ${isPasswordFocused ? "text-blue-400" : "text-gray-500"}`}>
                  <Lock className="w-5 h-5" />
                </div>
                <input
                  type="password"
                  required
                  minLength={6}
                  placeholder="Password (min 6 chars)"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  onFocus={() => setIsPasswordFocused(true)}
                  onBlur={() => setIsPasswordFocused(false)}
                  className="w-full pl-12 pr-4 py-4 bg-white/5 text-white placeholder-gray-500 border border-white/10 rounded-xl focus:bg-white/10 focus:border-blue-400/50 focus:outline-none input-glow transition-all"
                />
              </div>

              {/* Submit */}
              <button
                type="submit"
                disabled={isLoading}
                className="w-full py-4 rounded-xl bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 text-white font-semibold flex items-center justify-center transition-all transform hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed shadow-lg"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="animate-spin mr-2" size={20} />
                    Creating account...
                  </>
                ) : (
                  "Sign Up"
                )}
              </button>
            </form>

            {/* Footer */}
            <div className="mt-8 text-center">
              <span className="text-gray-400 text-sm">
                Already have an account?{" "}
                <Link
                  to="/login"
                  className="text-blue-400 hover:text-blue-300 font-semibold transition-colors underline-offset-2 hover:underline"
                >
                  Sign in
                </Link>
              </span>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default Register;