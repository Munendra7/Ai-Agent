import { useNavigate } from 'react-router-dom';

function NotFoundPage() {
  const navigate = useNavigate();

  const handleRedirect = () => {
    navigate('/');
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100">
      <div className="text-center bg-white p-8 rounded-lg shadow-lg max-w-sm w-full">
        <h1 className="text-6xl font-bold text-red-600">404</h1>
        <p className="mt-4 text-lg text-gray-700">Oops! The page you're looking for doesn't exist.</p>
        <p className="mt-2 text-gray-500">It might have been moved or deleted.</p>
        <button
          onClick={handleRedirect}
          className="mt-6 px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition duration-200"
        >
          Go to Home
        </button>
      </div>
    </div>
  );
}

export default NotFoundPage;