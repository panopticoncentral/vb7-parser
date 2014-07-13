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
''' A parse tree for a block declaration.
''' </summary>
Public MustInherit Class BlockDeclaration
    Inherits ModifiedDeclaration

    Private ReadOnly _KeywordLocation As Location
    Private ReadOnly _Name As SimpleName
    Private ReadOnly _Declarations As DeclarationCollection
    Private ReadOnly _EndStatement As EndBlockDeclaration

    ''' <summary>
    ''' The location of the keyword.
    ''' </summary>
    Public ReadOnly Property KeywordLocation() As Location
        Get
            Return _KeywordLocation
        End Get
    End Property

    ''' <summary>
    ''' The name of the declaration.
    ''' </summary>
    Public ReadOnly Property Name() As SimpleName
        Get
            Return _Name
        End Get
    End Property

    ''' <summary>
    ''' The declarations in the block.
    ''' </summary>
    Public ReadOnly Property Declarations() As DeclarationCollection
        Get
            Return _Declarations
        End Get
    End Property

    ''' <summary>
    ''' The End statement for the block.
    ''' </summary>
    Public ReadOnly Property EndStatement() As EndBlockDeclaration
        Get
            Return _EndStatement
        End Get
    End Property

    Protected Sub New(ByVal type As TreeType, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection, ByVal keywordLocation As Location, ByVal name As SimpleName, ByVal declarations As DeclarationCollection, ByVal endStatement As EndBlockDeclaration, ByVal span As Span, ByVal comments As CommentCollection)
        MyBase.New(type, attributes, modifiers, span, comments)

        Debug.Assert(type = TreeType.ClassDeclaration OrElse type = TreeType.ModuleDeclaration OrElse _
                     type = TreeType.InterfaceDeclaration OrElse type = TreeType.StructureDeclaration OrElse _
                     type = TreeType.EnumDeclaration)

        If name Is Nothing Then
            Throw New ArgumentNullException("name")
        End If

        SetParent(name)
        SetParent(declarations)
        SetParent(endStatement)

        _KeywordLocation = keywordLocation
        _Name = name
        _Declarations = declarations
        _EndStatement = endStatement
    End Sub

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        MyBase.GetChildTrees(childList)

        AddChild(childList, Name)
        AddChild(childList, Declarations)
        AddChild(childList, EndStatement)
    End Sub
End Class