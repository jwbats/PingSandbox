using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace PingSandbox
{
	class Program
	{

		private static List<AvailDomain> CreateListAvailDomains(int skip, int max)
		{
			string[] lines = File.ReadAllLines("urls.txt")
				.Where(x => !String.IsNullOrWhiteSpace(x))
				.Select(x => x.Trim())
				.Skip(skip)
				.Take(max)
				.ToArray();

			return lines
				.Select((x, i) => new AvailDomain(i, x))
				.ToList();
		}




		private static void ProcessAvailDomains(List<AvailDomain> availDomains)
		{
			int nrTaken   = availDomains.Count(x => x.Available == false);
			int nrNotSure = availDomains.Count(x => x.Available == null);

			//Debug.WriteLine("Domains taken   : ({0} / {1})", nrTaken, dicAvailDomains.Keys.Count);
			//Debug.WriteLine("Domains not sure: ({0} / {1})", nrNotSure, dicAvailDomains.Keys.Count);
		}




		private static void TestRun(int nrTasks, int msTimeout)
		{
			List<AvailDomain> availDomains = CreateListAvailDomains(2000, 1000);

			PingTask2 pingTask = new PingTask2(availDomains, nrTasks, msTimeout);
			pingTask.RunTask(ProcessAvailDomains);
			while(pingTask.IsRunning);
		}




		static void Main(string[] args)
		{
			TestRun(10, 200);
			
			Console.WriteLine("End of program... waiting for ReadKey().");

			Console.ReadKey();
		}

	}
}
