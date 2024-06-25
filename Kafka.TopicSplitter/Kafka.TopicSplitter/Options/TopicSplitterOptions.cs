namespace Kafka.TopicSplitter.Options;

public class TopicSplitterOptions
{
    public List<TopicConfig> Topics { get; set; }
}

public class TopicConfig
{
    public string TopicName { get; set; }
    public List<string> MessageTypes { get; set; }
}