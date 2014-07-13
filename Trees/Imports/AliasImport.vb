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
''' A parse tree for an Imports statement that aliases a type or namespace.
''' </summary>
Public NotInheritable Class AliasImport
    Inherits Import

    Private ReadOnly _Name As SimpleName
    Private ReadOnly _EqualsLocation As Location
    Private ReadOnly _AliasedName As Name

    ''' <summary>
    ''' The alias name.
    ''' </summary>
    Public ReadOnly Property Name() As SimpleName
        Get
            Return _Name
        End Get
    End Property

    ''' <summary>
    ''' The location of the '='.
    ''' </summary>
    Public ReadOnly Property EqualsLocation() As Location
        Get
            Return _EqualsLocation
        End Get
    End Property

    ''' <summary>
    ''' The name being aliased.
    ''' </summary>
    Public ReadOnly Property AliasedName() As Name
        Get
            Return _AliasedName
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new aliased import parse tree.
    ''' </summary>
    ''' <param name="name">The name of the alias.</param>
    ''' <param name="equalsLocation">The location of the '='.</param>
    ''' <param name="aliasedName">The name being aliased.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal name As SimpleName, ByVal equalsLocation As Location, ByVal aliasedName As Name, ByVal span As Span)
        MyBase.New(TreeType.AliasImport, span)

        If aliasedName Is Nothing Then
            Throw New ArgumentNullException("aliasedName")
        End If

        If name Is Nothing Then
            Throw New ArgumentNullException("name")
        End If

        SetParent(name)
        SetParent(aliasedName)

        _Name = name
        _EqualsLocation = equalsLocation
        _AliasedName = aliasedName
    End Sub

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        MyBase.GetChildTrees(childList)

        AddChild(childList, AliasedName)
    End Sub
End Class