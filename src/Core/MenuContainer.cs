using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenWorldBuilder
{
    public struct MenuItem
    {
        public string name;
        public Action callback;

        public MenuItem(string name, Action callback)
        {
            this.name = name;
            this.callback = callback;
        }
    }

    public class MenuContainer
    {
        public string? name = null;
        public List<MenuContainer> subMenus = new List<MenuContainer>();
        public List<MenuItem> subItems = new List<MenuItem>();

        public MenuContainer GetOrCreateSubMenu(string subMenu)
        {
            foreach (var sub in subMenus)
            {
                if (sub.name == subMenu)
                {
                    return sub;
                }
            }

            MenuContainer c = new MenuContainer();
            c.name = subMenu;
            subMenus.Add(c);

            return c;
        }
    }
}
