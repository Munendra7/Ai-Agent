import { useEffect, useState } from "react";
import { useRouteError } from "react-router-dom";
import { toast } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

const ErrorPage = () => {
  const error = useRouteError();
  const [hasShownToast, setHasShownToast] = useState(false);

  useEffect(() => {
    if (!hasShownToast) {
      toast.error("Something went wrong! Please try again.");
      setHasShownToast(true); // Ensure it only shows once per error
    }
  }, [hasShownToast]);

  console.error("Error caught in ErrorPage:", error);

  return (
    <div style={{ textAlign: "center", marginTop: "50px" }}>
      <h1>Oops! Something went wrong.</h1>
      <p>{(error as Error)?.message || "An unexpected error occurred."}</p>
      <a href="/" style={{ color: "blue", textDecoration: "underline" }}>
        Go back to Home
      </a>
    </div>
  );
};

export default ErrorPage;