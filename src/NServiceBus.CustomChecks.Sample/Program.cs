using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.CustomChecks;

class Program
{
    static void Main()
    {
        Console.Title = "NServiceBus.CustomChecks.Sample";

        AsyncMain().GetAwaiter().GetResult();
    }

    static async Task AsyncMain()
    {
        var endpointConfiguration = new EndpointConfiguration("NServiceBus.CustomChecks.Sample");

        endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
        endpointConfiguration.UseTransport(new LearningTransport());
        endpointConfiguration.ReportCustomChecksTo("Particular.ServiceControl");

        var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();

        await endpointInstance.Stop().ConfigureAwait(false);
    }

    class MyCheck : ICustomCheck
    {
        static Random r = new Random();

        public string Category => "MyCategory";
        public string Id => "MyId";
        public TimeSpan? Interval => TimeSpan.FromSeconds(5);
        Task<CheckResult> ICustomCheck.PerformCheck()
        {
            return r.Next(2) == 0
                ? Task.FromResult(CheckResult.Pass)
                : Task.FromResult(CheckResult.Failed("reasons"));
        }
    }
}
