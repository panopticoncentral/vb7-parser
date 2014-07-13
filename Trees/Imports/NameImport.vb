'
' Visual Basic .NET Parser
'
' Copyright (C) 2004, Microsoft Corporation. All rights reserved.
'
' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
' EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
' MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
'

''' <summary>
''' A parse tree for an Imports statement for a name.
''' </summary>
Public NotInheritable Class NameImport
    Inherits Import

    Private ReadOnly _Name As Name

    ''' <summary>
    ''' The imported name.
    ''' </summary>
    Public ReadOnly Property Name() As Name
        Get
            Return _Name
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new name import parse tree.
    ''' </summary>
    ''' <param name="name">The name to import.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal name As Name, ByVal span As Span)
        MyBase.New(TreeType.NameImport, span)

        If name Is Nothing Then
            Throw New ArgumentNullException("name")
        End If

        SetParent(name)

        _Name = name
    End Sub

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        AddChild(childList, Name)
    End Sub
End Class
