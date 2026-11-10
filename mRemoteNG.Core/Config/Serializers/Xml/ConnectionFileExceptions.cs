using mRemoteNG.Core.Security;

namespace mRemoteNG.Core.Config.Serializers.Xml
{
    /// <summary>
    /// Thrown when a connection file is protected by a master password and no password,
    /// or the wrong one, was supplied. Callers should prompt the user and retry.
    /// </summary>
    public class ConnectionFilePasswordException : EncryptionException
    {
        public bool PasswordWasSupplied { get; }

        public ConnectionFilePasswordException(bool passwordWasSupplied)
            : base(passwordWasSupplied
                ? "The password for this connection file is incorrect."
                : "This connection file is protected by a password.")
        {
            PasswordWasSupplied = passwordWasSupplied;
        }
    }

    /// <summary>Thrown when a connection file was written by a newer, unsupported version.</summary>
    public class ConnectionFileVersionException : Exception
    {
        public double FileVersion { get; }

        public ConnectionFileVersionException(double fileVersion, double maxSupportedVersion)
            : base($"Connection file format version {fileVersion} is not supported (highest supported version is {maxSupportedVersion}).")
        {
            FileVersion = fileVersion;
        }
    }

    /// <summary>
    /// Thrown when a connection file asks for more key derivation iterations than this version
    /// allows, which would make opening it take hours.
    /// </summary>
    public class ConnectionFileKdfIterationsException : Exception
    {
        public long Iterations { get; }

        public ConnectionFileKdfIterationsException(long iterations, int maxIterations)
            : base($"Connection file asks for {iterations:N0} key derivation iterations (the maximum is {maxIterations:N0}).")
        {
            Iterations = iterations;
        }
    }

    /// <summary>The encryption settings a connection file was stored with.</summary>
    public sealed record ConnectionFileEncryption(
        BlockCipherEngines Engine = BlockCipherEngines.AES,
        BlockCipherModes Mode = BlockCipherModes.GCM,
        int KeyDerivationIterations = 1000,
        bool FullFileEncryption = false);
}
