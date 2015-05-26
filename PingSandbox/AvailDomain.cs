using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingSandbox
{
	public class AvailDomain
	{
		public AvailDomain(long id, string domain)
		{
			this.ID        = id;
			this.Domain    = domain;
			this.Available = null;
		}

		public long   ID        { get; set; }
		public string Domain    { get; set; }
		public bool?  Available { get; set; }
	}
}
