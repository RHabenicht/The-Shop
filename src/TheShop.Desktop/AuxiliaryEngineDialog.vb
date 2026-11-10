Imports System.Drawing
Imports System.Windows.Forms
Imports TheShop.Design

Friend NotInheritable Class AuxiliaryEngineDialog
    Inherits Form

    Private ReadOnly engines As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 300}
    Public ReadOnly Property SelectedEngineId As String
        Get
            Return DirectCast(engines.SelectedItem, KeyValuePair(Of String, String)).Key
        End Get
    End Property

    Public Sub New(current As String, chassis As StockChassis)
        Text = "Auxiliary engine"
        StartPosition = FormStartPosition.CenterParent
        AutoSize = True
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        Dim choices As New List(Of KeyValuePair(Of String, String)) From {
            New KeyValuePair(Of String, String)(Nothing, "No auxiliary engine")
        }
        If chassis.Body.GetValueOrDefault() > 0 OrElse chassis.Hull.HasValue Then
            choices.AddRange(chassis.PowerPlants.Where(Function(engine) engine.Id <> "unpowered").
                             Select(Function(engine) New KeyValuePair(Of String, String)(engine.Id, engine.Name)))
        End If
        engines.DisplayMember = "Value"
        engines.DataSource = choices
        engines.SelectedIndex = Math.Max(0, choices.FindIndex(Function(item) String.Equals(item.Key, current, StringComparison.OrdinalIgnoreCase)))
        Dim layout As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True, .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False, .Padding = New Padding(12)}
        Controls.Add(layout)
        layout.Controls.Add(New Label With {.AutoSize = True, .Text = "The auxiliary engine operates as an alternative to the primary engine."})
        layout.Controls.Add(engines)
        Dim apply As New Button With {.Text = "Apply", .DialogResult = DialogResult.OK}
        Dim cancel As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel}
        layout.Controls.Add(apply)
        layout.Controls.Add(cancel)
        AcceptButton = apply
        CancelButton = cancel
    End Sub
End Class
