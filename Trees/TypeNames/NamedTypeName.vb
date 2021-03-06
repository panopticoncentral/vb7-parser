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
''' A parse tree for a named type.
''' </summary>
Public NotInheritable Class NamedTypeName
    Inherits TypeName

    Private ReadOnly _Name As Name

    ''' <summary>
    ''' The name of the type.
    ''' </summary>
    Public ReadOnly Property Name() As Name
        Get
            Return _Name
        End Get
    End Property

    ''' <summary>
    ''' Creates a new bad named type.
    ''' </summary>
    ''' <param name="span">The location of the bad named type.</param>
    ''' <returns>A bad named type.</returns>
    Public Shared Function GetBadNamedType(ByVal span As Span) As NamedTypeName
        Return New NamedTypeName(SimpleName.GetBadSimpleName(span), span)
    End Function

    Public Overrides ReadOnly Property IsBad() As Boolean
        Get
            Return Name.IsBad
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new named type parse tree.
    ''' </summary>
    ''' <param name="name">The name of the type.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal name As Name, ByVal span As Span)
        MyBase.New(TreeType.NamedType, span)

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