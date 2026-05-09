using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Razor;

namespace ZSN.AI.Service.Expander
{
    public class ViewLocationExpander : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            throw new NotImplementedException();
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            throw new NotImplementedException();
        }
    }
}
