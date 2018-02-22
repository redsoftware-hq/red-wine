using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Red.Wine.Picker
{
    internal static class PickUtilities
    {
        internal static PickConfig PickThese(string config)
        {
            string[] configArray = config.Split(',');
            return PickThese(configArray);
        }

        internal static PickConfig PickThese(string[] config)
        {
            PickConfig pickConfig = new PickConfig(true, true)
            {
                Picks = new List<Pick>()
            };

            foreach (var item in config)
            {
                string[] subItems = item.Split('.');

                switch(subItems.Length)
                {
                    case 1:
                        pickConfig.Picks.Add(new Pick(subItems[0], new PickConfig(true, true)));
                        break;
                    case 2:
                        pickConfig.Picks.Add(new Pick(subItems[0], new PickConfig(false, true, new List<Pick>
                        {
                            new Pick(subItems[1])
                        })));
                        break;
                    case 3:
                        pickConfig.Picks.Add(new Pick(subItems[0], new PickConfig(true, true, new List<Pick>
                        {
                            new Pick(subItems[1], new PickConfig(false, true, new List<Pick>
                            {
                                new Pick(subItems[2])
                            }))
                        })));
                        break;
                    case 4:
                        pickConfig.Picks.Add(new Pick(subItems[0], new PickConfig(true, true, new List<Pick>
                        {
                            new Pick(subItems[1], new PickConfig(true, true, new List<Pick>
                            {
                                new Pick(subItems[2], new PickConfig(false, true, new List<Pick>{
                                    new Pick(subItems[3])
                                }))
                            }))
                        })));
                        break;
                }
                        
            }

            return pickConfig;
        }
    }
}
