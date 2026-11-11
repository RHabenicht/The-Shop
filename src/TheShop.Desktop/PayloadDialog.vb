Imports System.ComponentModel
Imports System.Drawing
Imports System.Text.Json
Imports System.Windows.Forms
Imports TheShop.Design

Friend NotInheritable Class PayloadDialog
    Inherits Form
    Private ReadOnly payloadRows As BindingList(Of VehiclePayloadItem)
    Private ReadOnly settingRows As BindingList(Of EquipmentSetting)
    Public ReadOnly Property Payload As List(Of VehiclePayloadItem)
        Get
            Return If(payloadRows.Count = 0, Nothing, payloadRows.ToList())
        End Get
    End Property
    Public ReadOnly Property Settings As List(Of EquipmentSetting)
        Get
            Return If(settingRows.Count = 0, Nothing, settingRows.ToList())
        End Get
    End Property
    Public Sub New(currentPayload As List(Of VehiclePayloadItem), currentSettings As List(Of EquipmentSetting))
        Text = "Payload and individual equipment"
        ClientSize = New Size(1000, 540)
        MinimumSize = New Size(760, 420)
        StartPosition = FormStartPosition.CenterParent
        payloadRows = New BindingList(Of VehiclePayloadItem)(JsonSerializer.Deserialize(Of List(Of VehiclePayloadItem))(JsonSerializer.Serialize(If(currentPayload, New List(Of VehiclePayloadItem)))))
        settingRows = New BindingList(Of EquipmentSetting)(JsonSerializer.Deserialize(Of List(Of EquipmentSetting))(JsonSerializer.Serialize(If(currentSettings, New List(Of EquipmentSetting)))))
        Dim frame As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(10)}
        frame.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        frame.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        frame.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Controls.Add(frame)
        frame.Controls.Add(New Label With {.Text = "Payload includes weapons, ammunition, crew, tools and carried drones. Enter actual unit weights and list prices. Mount IDs match catalog IDs; instances start at 1.", .AutoSize = True, .MaximumSize = New Size(940, 0)}, 0, 0)
        Dim tabs As New TabControl With {.Dock = DockStyle.Fill}
        Dim payloadPage As New TabPage("Payload / weapons")
        Dim settingsPage As New TabPage("Individual equipment")
        tabs.TabPages.Add(payloadPage)
        tabs.TabPages.Add(settingsPage)
        Dim payloadGrid = MakeGrid("payloadGrid", payloadRows)
        payloadPage.Controls.Add(payloadGrid)
        Dim settingGrid = MakeGrid("equipmentSettingsGrid", settingRows)
        settingsPage.Controls.Add(settingGrid)
        Dim instructions As New Label With {.Dock = DockStyle.Top, .Height = 45, .Text = "Component: Arm strength, Recoil or Amplifier. Target: catalog mount ID for Recoil, or device name for Amplifier. Arm settings use the mechanical-arm instances. Use individual settings instead of a uniform enhancement."}
        settingsPage.Controls.Add(instructions)
        frame.Controls.Add(tabs, 0, 1)
        Dim actions As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}
        Dim applyButton As New Button With {.Text = "Apply", .AutoSize = True}
        AddHandler applyButton.Click, Sub(sender, e)
                                          payloadGrid.EndEdit()
                                          settingGrid.EndEdit()
                                          BindingContext(payloadRows).EndCurrentEdit()
                                          BindingContext(settingRows).EndCurrentEdit()
                                          DialogResult = DialogResult.OK
                                      End Sub
        Dim cancelButton As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .AutoSize = True}
        actions.Controls.Add(applyButton)
        actions.Controls.Add(cancelButton)
        frame.Controls.Add(actions, 0, 2)
        CancelButton = cancelButton
    End Sub
    Private Shared Function MakeGrid(name As String, data As Object) As DataGridView
        Dim grid As New DataGridView With {.Name = name, .Dock = DockStyle.Fill, .AutoGenerateColumns = False,
            .AllowUserToAddRows = True, .AllowUserToDeleteRows = True, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells}
        Dim rowType = If(TypeOf data Is BindingList(Of VehiclePayloadItem), GetType(VehiclePayloadItem), GetType(EquipmentSetting))
        For Each field As PropertyDescriptor In TypeDescriptor.GetProperties(rowType)
            Dim column As DataGridViewColumn
            If field.Name = "WeaponClass" Then
                Dim choices As New DataGridViewComboBoxColumn()
                choices.Items.Add("")
                choices.Items.AddRange(VehiclePayload.WeaponClasses.Cast(Of Object)().ToArray())
                column = choices
            Else
                column = New DataGridViewTextBoxColumn()
            End If
            column.Name = field.Name
            column.HeaderText = field.Name
            column.DataPropertyName = field.Name
            grid.Columns.Add(column)
        Next
        grid.DataSource = data
        Return grid
    End Function
End Class
