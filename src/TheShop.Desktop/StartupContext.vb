Imports System.Windows.Forms

Public NotInheritable Class StartupContext
    Inherits ApplicationContext

    Private ReadOnly splash As SplashForm
    Private editor As DesignForm

    Public Sub New()
        splash = New SplashForm()
        AddHandler splash.DisplayCompleted, AddressOf OpenEditor
        MainForm = splash
        splash.Show()
    End Sub

    Private Sub OpenEditor(sender As Object, e As EventArgs)
        RemoveHandler splash.DisplayCompleted, AddressOf OpenEditor
        editor = New DesignForm()
        MainForm = editor
        editor.Show()
        splash.Close()
        splash.Dispose()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If splash IsNot Nothing Then
                RemoveHandler splash.DisplayCompleted, AddressOf OpenEditor
                splash.Dispose()
            End If
            If editor IsNot Nothing Then editor.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
