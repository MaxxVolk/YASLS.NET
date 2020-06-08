namespace YASLS.NETServer
{
  partial class ProjectInstaller
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.spiProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
      this.siInstaller = new System.ServiceProcess.ServiceInstaller();
      // 
      // spiProcessInstaller
      // 
      this.spiProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
      this.spiProcessInstaller.Password = null;
      this.spiProcessInstaller.Username = null;
      // 
      // siInstaller
      // 
      this.siInstaller.Description = "Message routing and management solution.";
      this.siInstaller.DisplayName = "YASLS .NET Server";
      this.siInstaller.ServiceName = "YASLServer";
      this.siInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
      // 
      // ProjectInstaller
      // 
      this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.spiProcessInstaller,
            this.siInstaller});

    }

    #endregion

    private System.ServiceProcess.ServiceProcessInstaller spiProcessInstaller;
    private System.ServiceProcess.ServiceInstaller siInstaller;
  }
}