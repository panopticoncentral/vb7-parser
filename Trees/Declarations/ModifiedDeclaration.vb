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
''' A parse tree for a declaration with modifiers.
''' </summary>
Public MustInherit Class ModifiedDeclaration
    Inherits Declaration

    Private ReadOnly _Attributes As AttributeCollection
    Private ReadOnly _Modifiers As ModifierCollection

    ''' <summary>
    ''' The attributes on the declaration.
    ''' </summary>
    Public ReadOnly Property Attributes() As AttributeCollection
        Get
            Return _Attributes
        End Get
    End Property

    ''' <summary>
    ''' The modifiers on the declaration.
    ''' </summary>
    Public ReadOnly Property Modifiers() As ModifierCollection
        Get
            Return _Modifiers
        End Get
    End Property

    Protected Sub New(ByVal type As TreeType, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection, ByVal span As Span, ByVal comments As CommentCollection)
        MyBase.New(type, span, comments)

        SetParent(attributes)
        SetParent(modifiers)

        _Attributes = attributes
        _Modifiers = modifiers
    End Sub

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        AddChild(childList, Attributes)
        AddChild(childList, Modifiers)
    End Sub
End Class