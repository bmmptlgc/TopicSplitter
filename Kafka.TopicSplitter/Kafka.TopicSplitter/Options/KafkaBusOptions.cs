using System.ComponentModel.DataAnnotations;
using Confluent.SchemaRegistry;

namespace Kafka.TopicSplitter.Options
{
    public class KafkaBusOptions
    {
        public const string Section = "Kafka";

        public List<string> BootstrapServers { get; set; } = new();

        [Required]
        public SchemaRegistryConfig SchemaRegistry { get; set; }
    }
}