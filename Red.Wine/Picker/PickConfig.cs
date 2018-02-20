using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Red.Wine.Picker
{
    public class PickConfig
    {
        public PickConfig(bool includeAllNormalProperties = false, bool flatten = false, List<Pick> picks = null)
        {
            Picks = picks ?? new List<Pick>();
            IncludeAllNormalProperties = includeAllNormalProperties;
            Flatten = flatten;
        }

        public List<Pick> Picks { get; set; }
        public bool IncludeAllNormalProperties { get; set; }
        public bool Flatten { get; set; }

        public bool HasProperty(string propertyName)
        {
            foreach (var pick in Picks)
            {
                if (pick.Name == propertyName)
                {
                    return true;
                }
            }

            return false;
        }

        public PickConfig GetPickConfig(string propertyName)
        {
            foreach (var pick in Picks)
            {
                if (pick.Name == propertyName)
                {
                    return pick.PickConfig;
                }
            }

            return null;
        }
    }
}
