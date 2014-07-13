'
' Visual Basic .NET Parser
'
' Copyright (C) 2004, Microsoft Corporation. All rights reserved.
'
' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
' EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
' MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
'

Imports System.IO
Imports System.Text
Imports System.Xml
Imports VBParser

Public Class Main
    Inherits Form

    Const WinDiffPath As String = "windiff.exe"
    Const NotepadPath As String = "C:\Windows\notepad.exe"

    Private TestHarness As TestHarness = New TestHarness

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call

    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents MainMenu As System.Windows.Forms.MainMenu
    Friend WithEvents FileMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents FileExitMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents TextLoadMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents OpenFileDialog As System.Windows.Forms.OpenFileDialog
    Friend WithEvents TextClearMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents TestMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents TextMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents StatusBar As System.Windows.Forms.StatusBar
    Friend WithEvents TestParseFileMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents TestScanMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents OriginalTextBox As System.Windows.Forms.RichTextBox
    Friend WithEvents AfterTextBox As System.Windows.Forms.RichTextBox
    Friend WithEvents TestsTreeView As System.Windows.Forms.TreeView
    Friend WithEvents VSplitter As System.Windows.Forms.Splitter
    Friend WithEvents TextPanel As System.Windows.Forms.Panel
    Friend WithEvents HSplitter As System.Windows.Forms.Splitter
    Friend WithEvents SeparatorMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents TestAllMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents ParseExpressionMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents ParseStatementMenuItem As System.Windows.Forms.MenuItem
    Friend WithEvents ParseDeclarationMenuItem As System.Windows.Forms.MenuItem
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(Main))
        Me.MainMenu = New System.Windows.Forms.MainMenu
        Me.FileMenuItem = New System.Windows.Forms.MenuItem
        Me.FileExitMenuItem = New System.Windows.Forms.MenuItem
        Me.TextMenuItem = New System.Windows.Forms.MenuItem
        Me.TextLoadMenuItem = New System.Windows.Forms.MenuItem
        Me.TextClearMenuItem = New System.Windows.Forms.MenuItem
        Me.TestMenuItem = New System.Windows.Forms.MenuItem
        Me.TestScanMenuItem = New System.Windows.Forms.MenuItem
        Me.TestParseFileMenuItem = New System.Windows.Forms.MenuItem
        Me.ParseExpressionMenuItem = New System.Windows.Forms.MenuItem
        Me.ParseStatementMenuItem = New System.Windows.Forms.MenuItem
        Me.SeparatorMenuItem = New System.Windows.Forms.MenuItem
        Me.TestAllMenuItem = New System.Windows.Forms.MenuItem
        Me.OpenFileDialog = New System.Windows.Forms.OpenFileDialog
        Me.StatusBar = New System.Windows.Forms.StatusBar
        Me.TestsTreeView = New System.Windows.Forms.TreeView
        Me.VSplitter = New System.Windows.Forms.Splitter
        Me.TextPanel = New System.Windows.Forms.Panel
        Me.AfterTextBox = New System.Windows.Forms.RichTextBox
        Me.HSplitter = New System.Windows.Forms.Splitter
        Me.OriginalTextBox = New System.Windows.Forms.RichTextBox
        Me.ParseDeclarationMenuItem = New System.Windows.Forms.MenuItem
        Me.TextPanel.SuspendLayout()
        Me.SuspendLayout()
        '
        'MainMenu
        '
        Me.MainMenu.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.FileMenuItem, Me.TextMenuItem, Me.TestMenuItem})
        '
        'FileMenuItem
        '
        Me.FileMenuItem.Index = 0
        Me.FileMenuItem.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.FileExitMenuItem})
        Me.FileMenuItem.Text = "&File"
        '
        'FileExitMenuItem
        '
        Me.FileExitMenuItem.Index = 0
        Me.FileExitMenuItem.Text = "&Exit"
        '
        'TextMenuItem
        '
        Me.TextMenuItem.Index = 1
        Me.TextMenuItem.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.TextLoadMenuItem, Me.TextClearMenuItem})
        Me.TextMenuItem.Text = "&Text"
        '
        'TextLoadMenuItem
        '
        Me.TextLoadMenuItem.Index = 0
        Me.TextLoadMenuItem.Text = "&Load"
        '
        'TextClearMenuItem
        '
        Me.TextClearMenuItem.Index = 1
        Me.TextClearMenuItem.Text = "&Clear"
        '
        'TestMenuItem
        '
        Me.TestMenuItem.Index = 2
        Me.TestMenuItem.MenuItems.AddRange(New System.Windows.Forms.MenuItem() {Me.TestScanMenuItem, Me.TestParseFileMenuItem, Me.ParseExpressionMenuItem, Me.ParseStatementMenuItem, Me.ParseDeclarationMenuItem, Me.SeparatorMenuItem, Me.TestAllMenuItem})
        Me.TestMenuItem.Text = "T&est"
        '
        'TestScanMenuItem
        '
        Me.TestScanMenuItem.Index = 0
        Me.TestScanMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlS
        Me.TestScanMenuItem.Text = "&Scan"
        '
        'TestParseFileMenuItem
        '
        Me.TestParseFileMenuItem.Index = 1
        Me.TestParseFileMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF
        Me.TestParseFileMenuItem.Text = "Parse &File"
        '
        'ParseExpressionMenuItem
        '
        Me.ParseExpressionMenuItem.Index = 2
        Me.ParseExpressionMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlE
        Me.ParseExpressionMenuItem.Text = "Parse &Expression"
        '
        'ParseStatementMenuItem
        '
        Me.ParseStatementMenuItem.Index = 3
        Me.ParseStatementMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlT
        Me.ParseStatementMenuItem.Text = "Parse &Statement"
        '
        'SeparatorMenuItem
        '
        Me.SeparatorMenuItem.Index = 5
        Me.SeparatorMenuItem.Text = "-"
        '
        'TestAllMenuItem
        '
        Me.TestAllMenuItem.Index = 6
        Me.TestAllMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlA
        Me.TestAllMenuItem.Text = "&All"
        '
        'OpenFileDialog
        '
        Me.OpenFileDialog.DefaultExt = "vb"
        Me.OpenFileDialog.Filter = "Basic files|*.vb|All files|*.*"
        Me.OpenFileDialog.Title = "Open File"
        '
        'StatusBar
        '
        Me.StatusBar.Location = New System.Drawing.Point(0, 467)
        Me.StatusBar.Name = "StatusBar"
        Me.StatusBar.Size = New System.Drawing.Size(680, 22)
        Me.StatusBar.TabIndex = 0
        Me.StatusBar.Text = "Ready"
        '
        'TestsTreeView
        '
        Me.TestsTreeView.Dock = System.Windows.Forms.DockStyle.Left
        Me.TestsTreeView.ImageIndex = -1
        Me.TestsTreeView.Location = New System.Drawing.Point(0, 0)
        Me.TestsTreeView.Name = "TestsTreeView"
        Me.TestsTreeView.SelectedImageIndex = -1
        Me.TestsTreeView.Size = New System.Drawing.Size(160, 467)
        Me.TestsTreeView.TabIndex = 1
        '
        'VSplitter
        '
        Me.VSplitter.Location = New System.Drawing.Point(160, 0)
        Me.VSplitter.Name = "VSplitter"
        Me.VSplitter.Size = New System.Drawing.Size(3, 467)
        Me.VSplitter.TabIndex = 2
        Me.VSplitter.TabStop = False
        '
        'TextPanel
        '
        Me.TextPanel.Controls.Add(Me.AfterTextBox)
        Me.TextPanel.Controls.Add(Me.HSplitter)
        Me.TextPanel.Controls.Add(Me.OriginalTextBox)
        Me.TextPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TextPanel.Location = New System.Drawing.Point(163, 0)
        Me.TextPanel.Name = "TextPanel"
        Me.TextPanel.Size = New System.Drawing.Size(517, 467)
        Me.TextPanel.TabIndex = 3
        '
        'AfterTextBox
        '
        Me.AfterTextBox.AcceptsTab = True
        Me.AfterTextBox.Dock = System.Windows.Forms.DockStyle.Fill
        Me.AfterTextBox.Font = New System.Drawing.Font("Verdana", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.AfterTextBox.Location = New System.Drawing.Point(0, 235)
        Me.AfterTextBox.Name = "AfterTextBox"
        Me.AfterTextBox.Size = New System.Drawing.Size(517, 232)
        Me.AfterTextBox.TabIndex = 2
        Me.AfterTextBox.Text = ""
        '
        'HSplitter
        '
        Me.HSplitter.Dock = System.Windows.Forms.DockStyle.Top
        Me.HSplitter.Location = New System.Drawing.Point(0, 232)
        Me.HSplitter.Name = "HSplitter"
        Me.HSplitter.Size = New System.Drawing.Size(517, 3)
        Me.HSplitter.TabIndex = 1
        Me.HSplitter.TabStop = False
        '
        'OriginalTextBox
        '
        Me.OriginalTextBox.AcceptsTab = True
        Me.OriginalTextBox.Dock = System.Windows.Forms.DockStyle.Top
        Me.OriginalTextBox.Font = New System.Drawing.Font("Verdana", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.OriginalTextBox.Location = New System.Drawing.Point(0, 0)
        Me.OriginalTextBox.Name = "OriginalTextBox"
        Me.OriginalTextBox.Size = New System.Drawing.Size(517, 232)
        Me.OriginalTextBox.TabIndex = 0
        Me.OriginalTextBox.Text = ""
        '
        'ParseDeclarationMenuItem
        '
        Me.ParseDeclarationMenuItem.Index = 4
        Me.ParseDeclarationMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlD
        Me.ParseDeclarationMenuItem.Text = "Parse &Declaration"
        '
        'Main
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(680, 489)
        Me.Controls.Add(Me.TextPanel)
        Me.Controls.Add(Me.VSplitter)
        Me.Controls.Add(Me.TestsTreeView)
        Me.Controls.Add(Me.StatusBar)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Menu = Me.MainMenu
        Me.Name = "Main"
        Me.Text = "Parse Test"
        Me.TextPanel.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub FileExit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles FileExitMenuItem.Click
        Me.Close()
    End Sub

    Private Sub TextLoad_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles TextLoadMenuItem.Click
        If OpenFileDialog.ShowDialog() = DialogResult.OK Then
            OriginalTextBox.Clear()
            AfterTextBox.Clear()
            OriginalTextBox.LoadFile(OpenFileDialog.FileName, RichTextBoxStreamType.PlainText)
        End If
    End Sub

    Private Sub TextClear_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles TextClearMenuItem.Click
        OriginalTextBox.Clear()
        AfterTextBox.Clear()
    End Sub

    Private Sub DisplayTree(ByVal Tree As Tree, ByVal ErrorTable As ArrayList)
        Dim AfterText As String
        Dim StringWriter As StringWriter = New StringWriter
        Dim Writer As XmlTextWriter = New XmlTextWriter(StringWriter)
        Dim Serializer As XmlTreeSerializer = New XmlTreeSerializer(Writer)

        Writer.Formatting = Formatting.Indented
        Serializer.Serialize(Tree)

        If Not Tree Is Nothing Then
            AfterText = StringWriter.ToString()
        End If

        If ErrorTable.Count > 0 Then
            AfterText &= ControlChars.CrLf & "### Errors ###" & ControlChars.CrLf

            For Each SyntaxError As SyntaxError In ErrorTable
                OriginalTextBox.Select(CInt(SyntaxError.Span.Start.Index), CInt(SyntaxError.Span.Finish.Index - SyntaxError.Span.Start.Index))
                OriginalTextBox.SelectionColor = Color.Red
                AfterText &= SyntaxError.ToString() & ControlChars.CrLf
            Next
        End If

        OriginalTextBox.Select(0, 0)
        AfterTextBox.Text = AfterText
    End Sub

    Private Sub Main_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim Root As TreeNode

        TestsTreeView.BeginUpdate()

        Root = TestsTreeView.Nodes.Add("Root")

        For ScenarioType As ScenarioType = 0 To ScenarioType.Max
            Dim TypeNode As TreeNode = Root.Nodes.Add(ScenarioType.ToString())
            TypeNode.Tag = ScenarioType

            For Each Scenario As Scenario In TestHarness.Scenarios(ScenarioType)
                Dim ScenarioNode As TreeNode = TypeNode.Nodes.Add(Scenario.Name)
                Dim Index As Integer = 1

                ScenarioNode.Tag = Scenario

                For Each Test As Test In Scenario.Tests
                    Dim TestNode As TreeNode = ScenarioNode.Nodes.Add("Test #" & Index)
                    TestNode.Tag = Test
                    Index += 1
                Next
            Next
        Next

        TestsTreeView.EndUpdate()
    End Sub

    Private Sub TestsTreeView_AfterSelect(ByVal sender As Object, ByVal e As System.Windows.Forms.TreeViewEventArgs) Handles TestsTreeView.AfterSelect
        If Not e.Node.Tag Is Nothing Then
            If TypeOf e.Node.Tag Is Test Then
                Dim Test As Test = CType(e.Node.Tag, Test)

                OriginalTextBox.Text = Test.Code

                If Color.op_Equality(e.Node.ForeColor, Color.Red) Then
                    Dim OriginalPath As String = Path.GetTempPath() & "original.txt"
                    Dim AfterPath As String = Path.GetTempPath() & "after.txt"
                    Dim OriginalWriter As New StreamWriter(New FileStream(OriginalPath, FileMode.Create))
                    Dim AfterWriter As New StreamWriter(New FileStream(AfterPath, FileMode.Create))
                    Dim ScenarioType As ScenarioType = CType(e.Node.Parent.Parent.Tag, ScenarioType)
                    Dim ActualResult, ErrorResult As String

                    OriginalWriter.WriteLine(Test.Result)
                    OriginalWriter.Write(Test.Errors)
                    OriginalWriter.Close()

                    TestHarness.RunSingleTest(ScenarioType, Test, ActualResult, ErrorResult)

                    AfterWriter.WriteLine(ActualResult)
                    AfterWriter.Write(ErrorResult)
                    AfterWriter.Close()

                    Shell(WinDiffPath & " " & OriginalPath & " " & AfterPath, AppWinStyle.MaximizedFocus)
                End If
            End If
        End If
    End Sub

    Private Sub TestAllMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TestAllMenuItem.Click
        Dim RootNode As TreeNode = TestsTreeView.Nodes(0)
        Dim RootSucceeded As Boolean = True

        Cursor = Cursors.WaitCursor
        RootNode.Expand()

        For Each ScenarioTypeNode As TreeNode In RootNode.Nodes
            Dim ScenarioType As ScenarioType = CType(ScenarioTypeNode.Tag, ScenarioType)
            Dim ScenarioTypeSucceeded As Boolean = True

            ScenarioTypeNode.Expand()

            For Each ScenarioNode As TreeNode In ScenarioTypeNode.Nodes
                Dim Scenario As Scenario = CType(ScenarioNode.Tag, Scenario)
                Dim AddedResults As Boolean = False
                Dim ScenarioSucceeded As Boolean = True

                For Each TestNode As TreeNode In ScenarioNode.Nodes
                    Dim Succeeded As Boolean
                    Dim Test As Test = CType(TestNode.Tag, Test)

                    Succeeded = TestHarness.RunTest(ScenarioType, Test, AddedResults)

                    If Succeeded Then
                        TestNode.ForeColor = Color.Green
                    Else
                        TestNode.ForeColor = Color.Red
                        ScenarioSucceeded = False
                    End If
                Next

                If ScenarioSucceeded Then
                    ScenarioNode.ForeColor = Color.Green
                Else
                    ScenarioNode.ForeColor = Color.Red
                    ScenarioTypeSucceeded = False
                End If

                If AddedResults Then
                    Dim ResultsPath As String = Path.GetTempPath() & Scenario.Name & ".xml"
                    Dim ResultsXmlWriter As XmlTextWriter = New XmlTextWriter(ResultsPath, Encoding.Unicode)

                    ResultsXmlWriter.Formatting = Formatting.Indented

                    Scenario.Document.WriteTo(ResultsXmlWriter)
                    ResultsXmlWriter.Close()

                    Shell(NotepadPath & " " & ResultsPath, AppWinStyle.MaximizedFocus)
                End If
            Next

            If ScenarioTypeSucceeded Then
                ScenarioTypeNode.ForeColor = Color.Green
            Else
                ScenarioTypeNode.ForeColor = Color.Red
                RootSucceeded = False
            End If
        Next

        If RootSucceeded Then
            RootNode.ForeColor = Color.Green
        Else
            RootNode.ForeColor = Color.Red
        End If

        Cursor = Cursors.Default
    End Sub

    Private Sub ParseExpressionMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ParseExpressionMenuItem.Click
        Dim Scanner As Scanner = New Scanner(OriginalTextBox.Text)
        Dim Parser As Parser = New Parser
        Dim ErrorTable As ArrayList = New ArrayList

        DisplayTree(Parser.ParseExpression(Scanner, ErrorTable), ErrorTable)
    End Sub

    Private Sub ParseFileMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TestParseFileMenuItem.Click
        Dim Scanner As Scanner = New Scanner(OriginalTextBox.Text)
        Dim Parser As Parser = New Parser
        Dim ErrorTable As ArrayList = New ArrayList

        DisplayTree(Parser.ParseFile(Scanner, ErrorTable), ErrorTable)
    End Sub

    Private Sub ScanMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TestScanMenuItem.Click
        Dim Scanner As Scanner = New Scanner(OriginalTextBox.Text)
        Dim StringWriter As StringWriter = New StringWriter
        Dim Writer As XmlTextWriter = New XmlTextWriter(StringWriter)
        Dim Serializer As XmlTokenSerializer = New XmlTokenSerializer(Writer)

        Writer.Formatting = Formatting.Indented
        Serializer.Serialize(Scanner.ReadToEnd())
        AfterTextBox.Text = StringWriter.ToString()
    End Sub

    Private Sub ParseStatementMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ParseStatementMenuItem.Click
        Dim Scanner As Scanner = New Scanner(OriginalTextBox.Text)
        Dim Parser As Parser = New Parser
        Dim ErrorTable As ArrayList = New ArrayList

        DisplayTree(Parser.ParseStatement(Scanner, ErrorTable), ErrorTable)
    End Sub

    Private Sub ParseDeclarationMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ParseDeclarationMenuItem.Click
        Dim Scanner As Scanner = New Scanner(OriginalTextBox.Text)
        Dim Parser As Parser = New Parser
        Dim ErrorTable As ArrayList = New ArrayList

        DisplayTree(Parser.ParseDeclaration(Scanner, ErrorTable), ErrorTable)
    End Sub
End Class
