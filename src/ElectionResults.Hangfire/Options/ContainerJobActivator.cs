using Hangfire;

namespace ElectionResults.Hangfire.Options;

public class ContainerJobActivator(IServiceProvider serviceProvider) : JobActivator
{
    public override object ActivateJob(Type type)
    {
        return serviceProvider.CreateScope().ServiceProvider.GetRequiredService(type);
    }
}