using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingSandbox
{
	public class PingTask
	{

		#region ================================================== Constructor / Deconstructor ==================================================

		public PingTask(int chunkSize, int msTimeout)
		{
			this.chunkSize = chunkSize;
			this.msTimeout = msTimeout;
		}

		#endregion ================================================== Constructor / Deconstructor ==================================================




		#region ================================================== Private Members ==================================================

		private int chunkSize;
		private int msTimeout;

		private long nrDomainsToPing;
		private long nrEventsRunning;

		#endregion ================================================== Private Members ==================================================


		
		
		#region ================================================== Public Members ==================================================

		public bool IsRunning;

		#endregion ================================================== Public Members ==================================================
		



		#region ================================================== Private Methods ==================================================

		private void DecideDomainAvailability(object sender, PingCompletedEventArgs e)
		{
			AvailDomain availDomain = e.UserState as AvailDomain;

			bool successPing = (e.Reply != null && e.Reply.Status == IPStatus.Success);

			if (successPing)
			{
				availDomain.Available = false; // know for sure domain not available
			}
			else
			{
				availDomain.Available = null; // don't know for sure whether domain available
			}

			// Debug.WriteLine("Domain: {0}. Available: {1}", availDomain.Domain, (availDomain.Available == null) ? "NULL" : availDomain.Available.ToString());

			Interlocked.Decrement(ref this.nrEventsRunning);
			Interlocked.Decrement(ref this.nrDomainsToPing);
		}
		
		
		
		
		private void PingAsync(AvailDomain availDomain)
		{
			Interlocked.Increment(ref this.nrEventsRunning);

			Ping ping = new Ping();

			ping.PingCompleted += new PingCompletedEventHandler(DecideDomainAvailability);

			ping.SendAsync(
				availDomain.Domain,
				this.msTimeout,
				new byte[0],
				new PingOptions()
				{
					DontFragment = false,
					Ttl = int.MaxValue
				},
				availDomain
			);
		}




		private void ___RunTask(Dictionary<long, AvailDomain> dicAvailDomains)
		{
			this.nrDomainsToPing = dicAvailDomains.Keys.Count;

			List<List<long>> keyLists = dicAvailDomains.Keys.Chunk(this.chunkSize); // chunk it to keep parallel task count modest

			foreach (List<long> keyList in keyLists) // iterate chunks
			{
				foreach (long id in keyList) // iterate chunk
				{
					PingAsync(dicAvailDomains[id]);
				}

				while (this.nrEventsRunning > 0); // wait until all async events done for this chunk
			}

			while (this.nrDomainsToPing > 0); // wait until all async for all domains have been completed
		}




		private void __RunTask(Dictionary<long, AvailDomain> dicAvailDomains)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Restart();

			___RunTask(dicAvailDomains);

			int nrTaken             = dicAvailDomains.Count(x => x.Value.Available == false);
			int nrNotSure           = dicAvailDomains.Count(x => x.Value.Available == null);
			long msTotalTask        = stopwatch.ElapsedMilliseconds;
			double domainsPerSecond = dicAvailDomains.Keys.Count / (msTotalTask / 1000.0);

			Debug.WriteLine("==================================================");
			Debug.WriteLine("Chunk size: {0}", this.chunkSize);
			Debug.WriteLine("Ms timeout: {0}", this.msTimeout);
			Debug.WriteLine("Total domains: {0}", dicAvailDomains.Keys.Count);
			Debug.WriteLine("Total seconds: {0:0.00}", msTotalTask / 1000.0);
			Debug.WriteLine("Domains per second: {0:0.00}", domainsPerSecond);
			Debug.WriteLine("Domains taken   : ({0} / {1})", nrTaken, dicAvailDomains.Keys.Count);
			Debug.WriteLine("Domains not sure: ({0} / {1})", nrNotSure, dicAvailDomains.Keys.Count);
			Debug.WriteLine("==================================================");
		}




		private void _RunTask(Dictionary<long, AvailDomain> dicAvailDomains)
		{
			try
			{
				__RunTask(dicAvailDomains);
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

		public void RunTask(Dictionary<long, AvailDomain> dicAvailDomains, Action<Dictionary<long, AvailDomain>> actionContinueWith)
		{
			Task<Dictionary<long, AvailDomain>> task = new Task<Dictionary<long, AvailDomain>>(() => {
				_RunTask(dicAvailDomains);

				return dicAvailDomains; // pass on to continuation
			});

			task.ContinueWith(x => actionContinueWith(x.Result));

			this.IsRunning = true;
			
			task.Start();
		}

		#endregion ================================================== Public Methods ==================================================

	}
}
