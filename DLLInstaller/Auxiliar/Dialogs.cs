using System.Windows.Forms;

namespace DLLInstaller.Auxiliar
{
    public class Dialogs
    {
        public static void Error(string message, bool addToLog = false)
        {
            if (addToLog)
                Log.Add("ERROR " + message);
            MessageBox.Show(message, "DLLInstaller - ERROR", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }

        public static void Message(string message, bool addToLog = false)
        {
            if (addToLog)
                Log.Add(message);
            MessageBox.Show(message, "DLLInstaller", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
