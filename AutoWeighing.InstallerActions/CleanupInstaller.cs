
using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;

namespace AutoWeighing.InstallerActions
{
    [RunInstaller(true)]
    public class CleanupInstaller : Installer
    {
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);

            try
            {
                // ------------------------------------
                // 1. Delete ProgramData application folder
                // ------------------------------------

                string programDataFolder = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "Headers Software Solutions Pvt Ltd",
                    "Auto Weighing and Printing V3"
                );

                DeleteFolder(programDataFolder);


                // ------------------------------------
                // 2. Delete PRN FILE from installation folder
                // ------------------------------------

                // Get installation directory from CustomActionData
                string installPath = Context.Parameters["targetdir"];

                if (!string.IsNullOrWhiteSpace(installPath))
                {
                    installPath = installPath.Trim('"');

                    string prnFolder = Path.Combine(
                        installPath,
                        "PRN FILE"
                    );

                    DeleteFolder(prnFolder);
                }
                else
                {
                    Context.LogMessage(
                        "Installation directory not received. PRN FILE cleanup skipped.");
                }
            }
            catch (Exception ex)
            {
                Context.LogMessage(
                    "Cleanup failed: " + ex.ToString());

                throw new InstallException(
                    "Unable to delete application data folders.",
                    ex);
            }
        }


        // ------------------------------------
        // Common folder deletion method
        // ------------------------------------

        private void DeleteFolder(string folderPath)
        {
            Context.LogMessage(
                "Checking folder: " + folderPath);

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);

                Context.LogMessage(
                    "Successfully deleted: " + folderPath);
            }
            else
            {
                Context.LogMessage(
                    "Folder does not exist: " + folderPath);
            }
        }
    }
}
