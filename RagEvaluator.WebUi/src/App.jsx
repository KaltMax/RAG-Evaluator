import { BrowserRouter, Routes, Route } from "react-router-dom";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import Header from "./components/Header";
import SearchView from "./components/SearchView";
import Sidebar from "./components/Sidebar";
import UploadDocuments from "./components/UploadDocuments";
import Settings from "./components/Settings";
import Statistics from "./components/Statistics";

function App() {

  return (
    <BrowserRouter>
      <div className="min-h-screen flex flex-col bg-[#1F1F1F]">
        <Header />
        <div className="flex flex-1 w-full overflow-x-hidden">
          <Sidebar />
          <main className="flex-1 min-w-0 p-4 md:p-8">
            <Routes>
              <Route path="/" element={<SearchView />} />
              <Route path="/upload" element={<UploadDocuments />} />
              <Route path="/statistics" element={< Statistics />} />
              <Route path="/settings" element={<Settings />} />
            </Routes>
          </main>
        </div>
        <ToastContainer position="bottom-left" theme="dark" />
      </div>
    </BrowserRouter>
  )
}

export default App
