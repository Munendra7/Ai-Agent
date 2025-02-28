import React from "react";
import { Outlet } from "react-router-dom";
import NavBar from "./components/NavBar";

// const ProfileContent: React.FC = () => {
//   const { instance, accounts } = useMsal();
//   const [graphData, setGraphData] = useState<any>(null);

//   const RequestProfileData = async () => {
//     try {
//       const response = await instance.acquireTokenSilent({
//         ...loginRequest,
//         account: accounts[0],
//       });
//       const data = await callMsGraph(response.accessToken);
//       setGraphData(data);
//     } catch (error) {
//       console.error("Error fetching profile data:", error);
//     }
//   };

//   return (
//     <div>
//       <h5>Welcome, {accounts[0].name}</h5>
//       {graphData ? (
//         <ProfileData graphData={graphData} />
//       ) : (
//         <Button onClick={RequestProfileData}>Request Profile Information</Button>
//       )}
//     </div>
//   );
// };

export const App: React.FC = () => {
  return (
    <div>
      <NavBar/>
      <Outlet />
    </div>
  );
}