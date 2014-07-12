using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class GraphFactory : IComponentFactory
    {
        public string ComponentName
        {
            get { return "Graph"; }
        }

        public string Description
        {
            get { return ""; }
        }

        public ComponentCategory Category
        {
            get { return ComponentCategory.Media; }
        }

        public IComponent Create(LiveSplitState state)
        {
            return new GraphCompositeComponent(state);
        }

        public string UpdateName
        {
            get { return ComponentName; }
        }

        public string XMLURL
        {
#if RELEASE_CANDIDATE
            get { return "http://livesplit.org/update_rc_sdhjdop/Components/update.LiveSplit.Graph.xml"; }
#else
            get { return "http://livesplit.org/update/Components/update.LiveSplit.Graph.xml"; }
#endif
        }

        public string UpdateURL
        {
#if RELEASE_CANDIDATE
            get { return "http://livesplit.org/update_rc_sdhjdop/"; }
#else
            get { return "http://livesplit.org/update/"; }
#endif
        }

        public Version Version
        {
            get { return Version.Parse("1.3.10"); }
        }
    }
}
