using System;
using MultiSpeakBrokerLoadTest.proxy;
using NLog;


/*
	MultiSpeakBrokerLoadTest is used to sending multiple commands to the broker
	to test high availability over a day.  
	
	HAProxy infront of two or more web servers. With Mongodb running a replica set.
	Start sending commands, and then ... stop one web site and see what happens.
    We need to setup meters and a fake CIS or provider who owns all these meters
    so the data can be routed properly and not fail do to an invalid look up.

	We will add the incrementer as the transactionID and send the same request 
	for the same meter.  This way we can check if 0 through X loaded.
*/
namespace MultiSpeakBrokerLoadTest
{
	class MainClass
	{
		private static Logger _logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		public static void Main(string[] args)
		{
			int requests = 250;  //86400, one command per second over 24 hours
			try {
				if (args.Length > 0)
					requests = Convert.ToInt32(args[0]);
			} catch (Exception ex) {
				StdOut(ex.Message);
			}

			var wallTime = new System.Diagnostics.Stopwatch();
			wallTime.Start();
			StdOut("Hello MultiSpeakBrokerLoadTest!");
			StdOut("Sending " + requests + " requests");
			InitiateMeterReadingsByMeterID(requests);
			StdOut("Goodbye MultiSpeakBrokerLoadTest!");
			wallTime.Stop();
			StdOut("Wall Time | " + wallTime.Elapsed);
			StdOut("TPS | " + (requests / wallTime.Elapsed.Seconds));
			Console.ReadLine();
		}

		/// <summary>
		/// Initiates the meter readings by meter identifier.
		/// </summary>
		/// <param name="messages">Messages.</param>
		public static void InitiateMeterReadingsByMeterID(int messages)
		{
			var wallTime = new System.Diagnostics.Stopwatch();
			var client = new MDM_Server();
			client.Url = "http://10.87.1.95/MultiSpeak/416/1/MDM_Server.asmx";
			StdOut(client.Url);

			//Add the security - once for the entire batch
			var header = new MultiSpeakMsgHeader {
				Company = "Spacely's Sprockets",
				UserID = "niscmr",
				Pwd = "niscmr",
				AppName = "TestApp for HAProxy"
			};
			client.MultiSpeakMsgHeaderValue = header;

			for (int i = 0; i < messages; i++) {
				try {
					StdOut("********** NEW MESSAGE **********");
					wallTime.Restart();
					// Add fake meters
					meterID[] meters = {
						new meterID{
							meterNo = "0",
							objectID = i.ToString(),
							serviceType = serviceType.Electric,
							serviceTypeSpecified = true,
							utility = i + "_HA_Test",
							//Value = "What is this for?"
						}
					};
					LogRequest(meters, header);

					string responseURL = "http://10.86.1.31/Simulators/MultiSpeak/416/MDM_Server.asmx";
					//Where we send the results, need a test url for this...
					//responseURL = "";  //Test this for empty url and error to be returned.
					string transactionID = i.ToString(); //Guid.NewGuid().ToString();
					var lifespan = new expirationTime {
						units = timeUnits.Hours,
						Value = 1
					};

					var results = client.InitiateMeterReadingsByMeterID(meters, responseURL, transactionID, lifespan);

					if (results != null) {
						foreach (var result in results) {
							StdOut("errorString\t: " + result.errorString);
							StdOut("eventTime \t: " + result.eventTime);
							StdOut("eventTimeSpecified \t: " + result.eventTimeSpecified);
							StdOut("nounType \t: " + result.nounType);
							StdOut("objectID \t: " + result.objectID);
							StdOut("Value \t: " + result.Value);
						}
					}
					//System.Threading.Thread.Sleep(1000);
					wallTime.Stop();
					StdOut("Wall Time | " + wallTime.Elapsed);
					StdOut("********** END MESSAGE **********");
				} catch (Exception ex) {
					StdOut("Exception TransactionID | (" + i + ") | " + ex.Message);
					if (ex.InnerException != null) {
						StdOut("InnerException TransactionID | (" + i + ") | " + ex.Message);
					}
					wallTime.Stop();
					StdOut("Wall Time | " + wallTime.Elapsed);
					StdOut("********** END MESSAGE **********");
				}
			} //end of loop
		}

		/// <summary>
		/// Logs the request - quick hack to keep up with data for referencing the requests sent.
		/// </summary>
		/// <param name="meters">Meters.</param>
		/// <param name="header">Header.</param>
		private static void LogRequest(meterID[] meters, MultiSpeakMsgHeader header)
		{
			StdOut("Company : " + header.Company);
			StdOut("UserID : " + header.UserID);
			StdOut("Pwd : " + header.Pwd);
			StdOut("AppName : " + header.AppName);
			foreach (var meter in meters) {
				StdOut("********** Meter List **********");
				StdOut("meterNo : " + meter.meterNo);
				StdOut("objectID : " + meter.objectID);
				StdOut("serviceType : " + meter.serviceType.ToString());
				StdOut("serviceTypeSpecified : " + meter.serviceTypeSpecified.ToString());
				StdOut("utility : " + meter.utility);
				StdOut("Value : " + meter.Value);
			}
		}

		/// <summary>
		/// StdOut reduces the duplication of logging and output to console
		/// </summary>
		/// <param name="msg">Message.</param>
		private static void StdOut(string msg)
		{
			Console.WriteLine(msg);
			_logger.Info(msg);
		}
	}
}
