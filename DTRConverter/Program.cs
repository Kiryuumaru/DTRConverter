using System.Text;

namespace DTRConverter
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NzA0NzY2QDMyMzAyZTMyMmUzMGluanRUNzNFdlI0SmNQRFpPNkZhblhGczh4NGFCUjNjVEJUak5SOUp1WWs9");
            Application.Run(new MainForm());
        }
    }
}