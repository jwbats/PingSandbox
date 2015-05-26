using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingSandbox
{
	public class PingTask2
	{

		#region ================================================== Constructor / Deconstructor ==================================================

		public PingTask2(List<AvailDomain> availDomains, int nrTasks, int msTimeout)
		{
			this.cqAvailDomainsSource      = new ConcurrentQueue<AvailDomain>(availDomains);
			this.cqAvailDomainsDestination = new ConcurrentQueue<AvailDomain>();
			this.nrTasks                   = nrTasks;
			this.msTimeout                 = msTimeout;
			this.originalDomainSourceCount = availDomains.Count;
		}

		#endregion ================================================== Constructor / Deconstructor ==================================================




		#region ================================================== Private Members ==================================================

		private ConcurrentQueue<AvailDomain> cqAvailDomainsSource;
		private ConcurrentQueue<AvailDomain> cqAvailDomainsDestination;
		private int nrTasks;
		private int msTimeout;
		private int originalDomainSourceCount;
		private int nrTasksRunning;

		#endregion ================================================== Private Members ==================================================


		
		
		#region ================================================== Public Members ==================================================

		public bool IsRunning;

		#endregion ================================================== Public Members ==================================================
		



		#region ================================================== Private Methods ==================================================

		private void DecideDomainAvailability(PingReply pingReply, AvailDomain availDomain)
		{
			bool successPing = (pingReply != null && pingReply.Status == IPStatus.Success);

			if (successPing)
			{
				availDomain.Available = false; // know for sure domain not available
			}
			else
			{
				availDomain.Available = null; // don't know for sure whether domain available
			}
		}
		
		
		
		
		private PingReply _Ping(AvailDomain availDomain)
		{
			Ping ping = new Ping();

			return ping.Send(
				availDomain.Domain,
				this.msTimeout,
				new byte[0],
				new PingOptions()
				{
					DontFragment = false,
					Ttl = int.MaxValue
				}
			);
		}




		private void PingDomain(AvailDomain availDomain)
		{
			PingReply pingReply = null;

			try
			{
				pingReply = _Ping(availDomain);
			}
			catch (Exception exception)
			{
				// so fucking what (asshole)
			}
			finally
			{
				DecideDomainAvailability(pingReply, availDomain);
			}
		}




		private void PingDomains(int index)
		{
			while (this.cqAvailDomainsDestination.Count < this.originalDomainSourceCount)
			{
				AvailDomain availDomain;
				if (this.cqAvailDomainsSource.TryDequeue(out availDomain))
				{
					PingDomain(availDomain);
					this.cqAvailDomainsDestination.Enqueue(availDomain);
				}
			}

			Debug.WriteLine("Single task run {0} completed.", index);

			Interlocked.Decrement(ref this.nrTasksRunning);
		}




		private void RunTasks()
		{
			this.nrTasksRunning = this.nrTasks;

			for (int i = 0; i < this.nrTasks; i++)
			{
				int j = i;

				Task.Factory.StartNew(() => {
					PingDomains(j);
				});
			}

			while (this.nrTasksRunning > 0);
		}




		private void __RunTask()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Restart();

			RunTasks();

			int nrTaken             = this.cqAvailDomainsDestination.Count(x => x.Available == false);
			int nrNotSure           = this.cqAvailDomainsDestination.Count(x => x.Available == null);
			long msTotalTask        = stopwatch.ElapsedMilliseconds;
			double domainsPerSecond = this.cqAvailDomainsDestination.Count / (msTotalTask / 1000.0);

			Debug.WriteLine("==================================================");
			Debug.WriteLine("Nr tasks: {0}", this.nrTasks);
			Debug.WriteLine("Ms timeout: {0}", this.msTimeout);
			Debug.WriteLine("Total domains: {0}", this.cqAvailDomainsDestination.Count);
			Debug.WriteLine("Total seconds: {0:0.00}", msTotalTask / 1000.0);
			Debug.WriteLine("Domains per second: {0:0.00}", domainsPerSecond);
			Debug.WriteLine("Domains taken   : ({0} / {1})", nrTaken, this.cqAvailDomainsDestination.Count);
			Debug.WriteLine("Domains not sure: ({0} / {1})", nrNotSure, this.cqAvailDomainsDestination.Count);
			Debug.WriteLine("==================================================");
		}




		private void _RunTask()
		{
			try
			{
				__RunTask();
			}
			catch (Exception exception)
			{
				// log it
			}
			finally
			{
				this.IsRunning = false;
			}
		}

		#endregion ================================================== Private Methods ==================================================




		#region ================================================== Public Methods ==================================================

		public void RunTask(Action<List<AvailDomain>> actionContinueWith)
		{
			Task<List<AvailDomain>> task = new Task<List<AvailDomain>>(() => {
				_RunTask();

				return new List<AvailDomain>(this.cqAvailDomainsDestination); // pass on to continuation
			});

			task.ContinueWith(x => actionContinueWith(x.Result));

			this.IsRunning = true;
			
			task.Start();
		}

		#endregion ================================================== Public Methods ==================================================

	}
}
