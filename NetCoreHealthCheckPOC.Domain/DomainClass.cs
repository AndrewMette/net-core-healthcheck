using System;
using NetCoreHealthCheckPOC.DataAccess;

namespace NetCoreHealthCheckPOC.Domain
{
    public class DomainClass
    {
        public DomainClass()
        {
            var stuff = new ApiDao();
        }
    }
}
