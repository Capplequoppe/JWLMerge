namespace JWLMergeCore.BackupFileServices.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class WrongManifestVersionException : BackupFileServicesException
    {
        public WrongManifestVersionException(string filename, int expectedVersion, int foundVersion)
            : base($"Wrong manifest version found ({foundVersion}) in {filename}. Expecting {expectedVersion}")
        {
            Filename = filename;
            ExpectedVersion = expectedVersion;
            FoundVersion = foundVersion;
        }

        // Without this constructor, deserialization will fail
        protected WrongManifestVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Filename = string.Empty;
        }

        public string Filename { get; }

        public int ExpectedVersion { get; }

        public int FoundVersion { get; }

        public WrongManifestVersionException()
        {
            Filename = string.Empty;
        }

        public WrongManifestVersionException(string message) : base(message)
        {
            Filename = string.Empty;
        }

        public WrongManifestVersionException(string message, Exception innerException) : base(message, innerException)
        {
            Filename = string.Empty;
        }
    }
}