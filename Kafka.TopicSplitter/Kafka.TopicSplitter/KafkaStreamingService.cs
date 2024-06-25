using System.Text;
using Avro.Generic;
using Kafka.TopicSplitter.Options;
using Microsoft.Extensions.Options;
using Serilog;
using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.SchemaRegistry.SerDes.Avro;
using Streamiz.Kafka.Net.SerDes;
using ILogger = Serilog.ILogger;

namespace Kafka.TopicSplitter;

public class KafkaStreamingService : BackgroundService
{
    private readonly ILogger _logger = Log.ForContext<KafkaStreamingService>();

    private readonly IOptions<KafkaBusOptions> _busOptions;
    private readonly IOptions<TopicSplitterOptions> _topicSplitterOptions;

    public KafkaStreamingService(IOptions<KafkaBusOptions> busOptions,
        IOptions<TopicSplitterOptions> topicSplitterOptions)
    {
        _busOptions = busOptions ?? throw new ArgumentNullException(nameof(busOptions));
        _topicSplitterOptions = topicSplitterOptions ?? throw new ArgumentNullException(nameof(topicSplitterOptions));
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new StreamConfig<StringSerDes, SchemaAvroSerDes<GenericRecord>>
        {
            ApplicationId = "kafka-topic-splitter",
            BootstrapServers = string.Join(",", _busOptions.Value.BootstrapServers),
            SchemaRegistryUrl = _busOptions.Value.SchemaRegistry.Url
        };

        var builder = new StreamBuilder();

        foreach (var topicConfig in _topicSplitterOptions.Value.Topics)
        {
            var sourceStream = builder.Stream<string, GenericRecord>(topicConfig.TopicName);
            
            sourceStream
                .Peek((key, value) =>
                    _logger.Information("Received from {TopicConfigTopicName}: {Value}",
                        topicConfig.TopicName, value))
                .To((key, value, context) =>
                {
                    var messageTypeHeader = context.Headers.FirstOrDefault(h => h.Key == "Message-Type")?.GetValueBytes();
                    if (messageTypeHeader == null)
                        return null;
                
                    var messageType = Encoding.UTF8.GetString(messageTypeHeader);
                    return $"single-{messageType}";
                }, new StringSerDes(), new SchemaAvroSerDes<GenericRecord>());
        }

        var stream = new KafkaStream(builder.Build(), config);
        await stream.StartAsync(stoppingToken);
    }
    
    // private readonly Func<Headers, string, bool> _filterMessageType = (headers, messageType) => headers
    //         .Any(h => h.Key == "Message-Type" && Encoding.UTF8.GetString(h.GetValueBytes()) == messageType);
}