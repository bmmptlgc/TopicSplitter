using System.Text;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.TopicSplitter.Contracts.Events.Order;
using Kafka.TopicSplitter.Options;
using Microsoft.Extensions.Options;
using Serilog;
using Headers = Confluent.Kafka.Headers;
using ILogger = Serilog.ILogger;

namespace Kafka.TopicSplitter.Producers
{
    public class OrderProducer : BackgroundService
    {
        private readonly ILogger _logger = Log.ForContext<OrderProducer>();
        
        private readonly IOptions<KafkaBusOptions> _busOptions;
        private readonly CachedSchemaRegistryClient _schemaRegistry;

        public OrderProducer(IOptions<KafkaBusOptions> busOptions, CachedSchemaRegistryClient schemaRegistry)
        {
            _busOptions = busOptions ?? throw new ArgumentNullException(nameof(busOptions));
            _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ProduceOrderCreatedAsync(cancellationToken);
                await ProduceCompletedAsync(cancellationToken);
            }
            catch
            {
                _logger.Information("Stopping");
            }
        }
        
        private async Task ProduceOrderCreatedAsync(CancellationToken cancellationToken)
        {
            var producerConfig = GetProducerConfig();

            using var producer =
                new ProducerBuilder<string, OrderCreated>(producerConfig)
                    .SetValueSerializer(new AvroSerializer<OrderCreated>(_schemaRegistry, SerializerConfig))
                    .Build();
                
            Console.WriteLine($"{producer.Name} producing on topic orders.");

            var orderCreated = new OrderCreated
            {
                Id = Guid.NewGuid(),
                Source = "TopicSplitter",
                SourceId = "TopicSplitter",
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 3
            };

            await producer
                .ProduceAsync(
                    "orders",
                    new Message<string, OrderCreated>
                    {
                        Key = Guid.NewGuid().ToString(),
                        Value = orderCreated,
                        Headers = new Headers{
                            { "Message-Type", Encoding.ASCII.GetBytes(typeof(OrderCreated).FullName!) }
                        }
                    }, cancellationToken)
                .ContinueWith(task =>
                {
                    if (!task.IsFaulted)
                    {
                        Console.WriteLine($"produced to: {task.Result.TopicPartitionOffset}");
                        return;
                    }
                        
                    Console.WriteLine($"error producing message: {task.Exception?.InnerException}");
                }, cancellationToken);
        }

        private async Task ProduceCompletedAsync(CancellationToken cancellationToken)
        {
            var producerConfig = GetProducerConfig();

            using var producer =
                new ProducerBuilder<string, OrderCompleted>(producerConfig)
                    .SetValueSerializer(new AvroSerializer<OrderCompleted>(_schemaRegistry, SerializerConfig))
                    .Build();
                
            Console.WriteLine($"{producer.Name} producing on topic orders.");

            var orderCompleted = new OrderCompleted()
            {
                Id = Guid.NewGuid(),
                Source = "TopicSplitter",
                SourceId = "TopicSplitter",
                OrderId = Guid.NewGuid()
            };

            await producer
                .ProduceAsync(
                    "orders",
                    new Message<string, OrderCompleted>
                    {
                        Key = Guid.NewGuid().ToString(),
                        Value = orderCompleted,
                        Headers = new Headers{
                            { "Message-Type", Encoding.ASCII.GetBytes(typeof(OrderCompleted).FullName!) }
                        }
                    }, cancellationToken)
                .ContinueWith(task =>
                {
                    if (!task.IsFaulted)
                    {
                        Console.WriteLine($"produced to: {task.Result.TopicPartitionOffset}");
                        return;
                    }
                        
                    Console.WriteLine($"error producing message: {task.Exception?.InnerException}");
                }, cancellationToken);
        }
        
        private ProducerConfig GetProducerConfig()
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = string.Join(",", _busOptions.Value.BootstrapServers),
            };
            return producerConfig;
        }
        
        private static readonly AvroSerializerConfig SerializerConfig = new()
        {
            AutoRegisterSchemas = true,
            SubjectNameStrategy = SubjectNameStrategy.Record
        };
    }
}
