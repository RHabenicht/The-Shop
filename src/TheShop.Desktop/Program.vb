Imports System.Windows.Forms

Module Program
    <STAThread>
    Sub Main()
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New DesignForm())
    End Sub
End Module
