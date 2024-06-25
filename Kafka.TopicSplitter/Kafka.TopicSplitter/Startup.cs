using Confluent.SchemaRegistry;
using Kafka.TopicSplitter.Options;
using Kafka.TopicSplitter.Producers;

namespace Kafka.TopicSplitter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(c =>
                new CachedSchemaRegistryClient(
                    Configuration.GetSection(KafkaBusOptions.Section).Get<KafkaBusOptions>()?.SchemaRegistry));
            
            services.Configure<KafkaBusOptions>(Configuration.GetSection(KafkaBusOptions.Section));
            services.AddOptions<TopicSplitterOptions>()
                .Bind(Configuration)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            
            // services.AddHostedService<OrderProducer>();
            services.AddHostedService<KafkaStreamingService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure()
        {
        }
    }
}
