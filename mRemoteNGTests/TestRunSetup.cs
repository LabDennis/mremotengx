using System.Windows.Forms;
using NUnit.Framework;

// Outside any namespace, so it runs once before every test in the assembly.
[SetUpFixture]
public class TestRunSetup
{
    // As the application does at startup. Visual styles only take effect when they are enabled
    // before the first control is created, and the text box cue banner needs them, so this
    // cannot wait for whichever test happens to create a form first.
    [OneTimeSetUp]
    public void EnableVisualStyles()
    {
        Application.EnableVisualStyles();
    }

    // Nothing in a test run may wait for a click: exceptions in window messages fail the test
    // instead of opening the WinForms error dialog, and closing a tab does not ask for confirmation.
    // The setting is only changed in memory and never saved, so the user's own config is untouched.
    [OneTimeSetUp]
    public void NeverBlockOnDialogs()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        mRemoteNG.Properties.Settings.Default.ConfirmCloseConnection = (int)mRemoteNG.Config.ConfirmCloseEnum.Never;
    }
}
