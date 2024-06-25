namespace Kafka.TopicSplitter.Contracts.Events
{
    public interface IVersionedIntegrationEvent
    {
        Guid Id { get; }
        
        DateTime CreatedAt { get; }
        
        string Source { get; }
        
        string SourceId { get; }
        
        long Version { get; }
    }
}
