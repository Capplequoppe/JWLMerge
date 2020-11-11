namespace JWLMergeCore.BackupFileServices.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class WrongDatabaseVersionException : BackupFileServicesException
    {
        public WrongDatabaseVersionException(string filename, int expectedVersion, int foundVersion)
            : base($"Wrong database version found ({foundVersion}) in {filename}. Expecting {expectedVersion}")
        {
            Filename = filename;
            ExpectedVersion = expectedVersion;
            FoundVersion = foundVersion;
        }

        // Without this constructor, deserialization will fail
        protected WrongDatabaseVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Filename = string.Empty;
        }

        public string Filename { get; }

        public int ExpectedVersion { get; }

        public int FoundVersion { get; }

        public WrongDatabaseVersionException()
        {
            Filename = string.Empty;
        }

        public WrongDatabaseVersionException(string message) : base(message)
        {
            Filename = string.Empty;
        }

        public WrongDatabaseVersionException(string message, Exception innerException) : base(message, innerException)
        {
            Filename = string.Empty;
        }
    }
}