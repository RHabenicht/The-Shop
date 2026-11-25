Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Public NotInheritable Class SplashForm
    Inherits Form

    Public Const DisplayMilliseconds As Integer = 6000
    Private ReadOnly displayTimer As New System.Windows.Forms.Timer()
    Private ReadOnly visibleTime As New Stopwatch()
    Private ReadOnly artwork As Image
    Private completed As Boolean

    Public Event DisplayCompleted As EventHandler

    Public Sub New()
        Text = "The Shop | Starting"
        AccessibleName = "The Shop startup splash"
        AccessibleDescription = "Updated by NightLife. Original by James Ojaste."
        FormBorderStyle = FormBorderStyle.None
        StartPosition = FormStartPosition.Manual
        AutoScaleMode = AutoScaleMode.None
        DoubleBuffered = True
        BackColor = Color.Black
        MaximizeBox = False
        MinimizeBox = False

        Using source = GetType(SplashForm).Assembly.GetManifestResourceStream("TheShop.Desktop.Splash.png")
            If source Is Nothing Then Throw New InvalidOperationException("The embedded splash artwork is missing.")
            Using original = Image.FromStream(source)
                artwork = New Bitmap(original)
            End Using
        End Using
        BackgroundImage = artwork
        BackgroundImageLayout = ImageLayout.Zoom

        Dim area = Screen.FromPoint(Cursor.Position).WorkingArea
        Dim scale = Math.Min(1100.0R / artwork.Width,
                             Math.Min(area.Width * 0.85R / artwork.Width, area.Height * 0.85R / artwork.Height))
        ClientSize = New Size(Math.Max(1, CInt(Math.Floor(artwork.Width * scale))),
                              Math.Max(1, CInt(Math.Floor(artwork.Height * scale))))
        Location = New Point(area.Left + (area.Width - Width) \ 2, area.Top + (area.Height - Height) \ 2)

        displayTimer.Interval = DisplayMilliseconds
        AddHandler displayTimer.Tick, AddressOf FinishDisplay
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        If Not completed AndAlso Not visibleTime.IsRunning Then
            visibleTime.Start()
            displayTimer.Start()
        End If
    End Sub

    Private Sub FinishDisplay(sender As Object, e As EventArgs)
        Dim remaining = DisplayMilliseconds - visibleTime.Elapsed.TotalMilliseconds
        If remaining > 0 Then
            displayTimer.Interval = Math.Max(1, CInt(Math.Ceiling(remaining)))
            Return
        End If
        displayTimer.Stop()
        completed = True
        RaiseEvent DisplayCompleted(Me, EventArgs.Empty)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            displayTimer.Dispose()
            BackgroundImage = Nothing
            If artwork IsNot Nothing Then artwork.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
