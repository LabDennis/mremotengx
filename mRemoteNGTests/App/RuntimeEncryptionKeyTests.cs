using System.Linq;
using mRemoteNG.App;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Connection;
using mRemoteNG.Security;
using mRemoteNG.Security.Factories;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.App
{
    // The settings key (Runtime.EncryptionKey) must stay independent of connection-file passwords.
    [NonParallelizable]
    public class RuntimeEncryptionKeyTests
    {
        private const string DefaultKey = "mR3m";
        private const string FilePassword = "file-secret";
        private const string MasterPassword = "master-secret";

        [TearDown]
        public void Teardown() => Runtime.ClearMasterPasswordSession();

        [Test]
        public void LoadingProtectedFile_KeepsDefaultSettingsKey()
        {
            Runtime.ClearMasterPasswordSession();

            LoadProtectedFile();

            Assert.That(Runtime.EncryptionKey.ConvertToUnsecureString(), Is.EqualTo(DefaultKey));
        }

        [Test]
        public void LoadingProtectedFile_KeepsMasterPasswordKey()
        {
            Runtime.SetMasterPasswordSession(MasterPassword.ConvertToSecureString());

            LoadProtectedFile();

            Assert.That(Runtime.EncryptionKey.ConvertToUnsecureString(), Is.EqualTo(MasterPassword));
        }

        [Test]
        public void ClearingMasterPasswordSession_RestoresDefaultKey()
        {
            Runtime.SetMasterPasswordSession(MasterPassword.ConvertToSecureString());

            Runtime.ClearMasterPasswordSession();

            Assert.That(Runtime.EncryptionKey.ConvertToUnsecureString(), Is.EqualTo(DefaultKey));
        }

        [Test]
        public void ResetEncryptionKey_WithoutMasterPassword_UsesDefaultKey()
        {
            Runtime.SetEncryptionKey(FilePassword);

            Runtime.ResetEncryptionKey();

            Assert.That(Runtime.EncryptionKey.ConvertToUnsecureString(), Is.EqualTo(DefaultKey));
        }

        private static void LoadProtectedFile()
        {
            ConnectionTreeModel model = new();
            RootNodeInfo root = new(RootNodeType.Connection) { PasswordString = FilePassword };
            root.AddChild(new ConnectionInfo { Name = "con1", Hostname = "host1" });
            model.AddRootNode(root);

            ICryptographyProvider crypto = new CryptoProviderFactory(BlockCipherEngines.AES, BlockCipherModes.GCM).Build();
            XmlConnectionNodeSerializer29 nodeSerializer = new(crypto, FilePassword.ConvertToSecureString(), new SaveFilter());
            string xml = new XmlConnectionsSerializer(crypto, nodeSerializer).Serialize(model);

            ConnectionTreeModel loaded = new XmlConnectionsDeserializer(() => FilePassword.ConvertToSecureString()).Deserialize(xml);
            Assert.That(loaded.GetRecursiveChildList().Any(c => c.Name == "con1"), Is.True);
        }
    }
}
