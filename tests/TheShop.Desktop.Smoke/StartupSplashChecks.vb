Imports System.Drawing
Imports System.Security.Cryptography
Imports TheShop.Desktop

Friend Module StartupSplashChecks
    Public Sub Run()
        Dim resource = GetType(SplashForm).Assembly.GetManifestResourceStream("TheShop.Desktop.Splash.png")
        If resource Is Nothing Then Throw New InvalidOperationException("The credited splash artwork was not embedded.")
        Using source = resource
            Dim hash = Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant()
            If hash <> "8e6046dfaa23c799d44203608c91aa47bcba3e8b4fa9fdc720457d40bbf15cb8" Then
                Throw New InvalidOperationException("The embedded splash artwork does not match the approved image with both credits.")
            End If
            source.Position = 0
            Using artwork = Image.FromStream(source)
                If artwork.Width <> 1672 OrElse artwork.Height <> 941 Then
                    Throw New InvalidOperationException("The splash artwork dimensions changed.")
                End If
            End Using
        End Using
        Console.WriteLine("Credited splash artwork: embedded PNG and approved image hash verified.")
    End Sub
End Module
