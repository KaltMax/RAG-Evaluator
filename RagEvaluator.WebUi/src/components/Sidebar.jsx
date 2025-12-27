import { Link, useLocation } from 'react-router-dom';
import { MagnifyingGlassIcon, DocumentPlusIcon, Cog6ToothIcon, ChartBarIcon } from '@heroicons/react/24/outline';

function Sidebar() {
  const location = useLocation();

  const navItems = [
    { path: '/', name: 'Search', icon: MagnifyingGlassIcon },
    { path: '/upload', name: 'Upload', icon: DocumentPlusIcon },
    { path: '/statistics', name: 'Statistics', icon: ChartBarIcon },
    { path: '/settings', name: 'Settings', icon: Cog6ToothIcon },
  ];

  const isActive = (path) => location.pathname === path;

  return (
    <aside className="w-64 bg-[#181818] border-r border-gray-800 p-4 hidden md:block">
      <nav className="space-y-2">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                isActive(item.path)
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-400 hover:bg-[#1F1F1F] hover:text-white'
              }`}
            >
              <Icon className="w-5 h-5" />
              <span className="font-medium">{item.name}</span>
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}

export default Sidebar;
