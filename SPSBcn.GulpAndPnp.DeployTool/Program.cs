using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPSBcn.GulpAndPnp.DeployTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() == 4)
            {
                string url = args[0];
                string user = args[1];
                string password = args[2];
                string template = args[3];

                Deployment.ApplyProvisioningTemplate(url, user, password, template);
            }
            else
            {
                throw new ArgumentException("Provide the right arguments: url, user, password, templatefile");
            }       
        }
    }
}
