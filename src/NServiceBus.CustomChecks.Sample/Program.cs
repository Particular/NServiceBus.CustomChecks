using System;
using NServiceBus;
using NServiceBus.CustomChecks;

class Program
{
    static void Main()
    {
        var busConfiguration = new BusConfiguration();

        busConfiguration.UsePersistence<InMemoryPersistence>();
        busConfiguration.ReportCustomChecksTo("Particular.ServiceControl");

        using (Bus.CreateSendOnly(busConfiguration))
        {
            Console.Out.WriteLine("Press a key to quit bus");
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
