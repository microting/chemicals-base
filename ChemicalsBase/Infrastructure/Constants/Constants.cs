namespace ChemicalsBase.Infrastructure.Constants;

public static class Constants
{
    public static class WorkflowStates
    {
        public const string Created = "created";
        public const string Processed = "processed";
        public const string PreCreated = "pre_created";
        public const string NotFound = "not_found";
        public const string Removed = "removed";
        public const string FailedToSync = "failed_to_sync";
        public const string NotRemoved = "not_removed";
    }
}