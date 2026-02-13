import { Link, useLocation } from 'react-router-dom';
import { MagnifyingGlassIcon, DocumentPlusIcon, Cog6ToothIcon, ChartBarIcon, DocumentTextIcon, ClockIcon, BeakerIcon } from '@heroicons/react/24/outline';

function Sidebar() {
  const location = useLocation();

  const navItems = [
    { path: '/', name: 'Search', icon: MagnifyingGlassIcon },
    { path: '/experiments', name: 'Experiments', icon: BeakerIcon },
    { path: '/uploadDocuments', name: 'Upload Documents', icon: DocumentPlusIcon },
    { path: '/documents', name: 'Documents', icon: DocumentTextIcon },
    { path: '/queryHistory', name: 'Query History', icon: ClockIcon },
    { path: '/statistics', name: 'Statistics', icon: ChartBarIcon },
    { path: '/settings', name: 'Settings', icon: Cog6ToothIcon },
  ];

  const isActive = (path) => location.pathname === path;

  return (
    <aside className="w-16 md:w-64 bg-[#181818] border-r border-gray-800 p-2 md:p-4">
      <nav className="space-y-2">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-2 md:px-4 py-3 rounded-lg transition-colors select-none ${
                isActive(item.path)
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-400 hover:bg-[#1F1F1F] hover:text-white'
              }`}
              title={item.name}
            >
              <Icon className="w-5 h-5 flex-shrink-0" />
              <span className="font-medium hidden md:inline">{item.name}</span>
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}

export default Sidebar;
