using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace OpenWorldBuilder
{
    public struct Hotkey
    {
        public bool ctrl;
        public bool shift;
        public bool alt;
        public Keys key;
    }

    public class MenuItem
    {
        public bool enabled;
        public string name;
        public Action callback;
        public Hotkey? hotkey;

        public MenuItem(string name, Action callback, Hotkey? hotkey = null)
        {
            this.enabled = true;
            this.name = name;
            this.callback = callback;
            this.hotkey = hotkey;
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
