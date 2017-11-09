using System;
using NServiceBus;
using NServiceBus.CustomChecks;

class Program
{
    static void Main()
    {
	    Console.Title = "NServiceBus.CustomChecks.Sample";

        var busConfiguration = new BusConfiguration();
        busConfiguration.EndpointName("NServiceBus.CustomChecks.Sample");

        busConfiguration.UsePersistence<InMemoryPersistence>();
        busConfiguration.ReportCustomChecksTo("Particular.ServiceControl");

        using (Bus.CreateSendOnly(busConfiguration))
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }

    class MyCheck : ICustomCheck
    {
        static Random r = new Random();

        public string Category => "MyCategory";
        public string Id => "MyId";
        public TimeSpan? Interval => TimeSpan.FromSeconds(5);
        public CheckResult PerformCheck()
        {
            return r.Next(2) == 0
                ? CheckResult.Pass
                : CheckResult.Failed("reasons");
        }
    }
}
