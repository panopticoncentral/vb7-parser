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
''' A parse tree for a Sub, Function or constructor declaration.
''' </summary>
Public MustInherit Class MethodDeclaration
    Inherits SignatureDeclaration

    Private ReadOnly _ImplementsList As NameCollection
    Private ReadOnly _HandlesList As NameCollection
    Private ReadOnly _Statements As StatementCollection
    Private ReadOnly _EndStatement As EndBlockDeclaration

    ''' <summary>
    ''' The list of implemented members.
    ''' </summary>
    Public ReadOnly Property ImplementsList() As NameCollection
        Get
            Return _ImplementsList
        End Get
    End Property

    ''' <summary>
    ''' The events that the declaration handles.
    ''' </summary>
    Public ReadOnly Property HandlesList() As NameCollection
        Get
            Return _HandlesList
        End Get
    End Property

    ''' <summary>
    ''' The statements in the declaration.
    ''' </summary>
    Public ReadOnly Property Statements() As StatementCollection
        Get
            Return _Statements
        End Get
    End Property

    ''' <summary>
    ''' The end block declaration, if any.
    ''' </summary>
    Public ReadOnly Property EndStatement() As EndBlockDeclaration
        Get
            Return _EndStatement
        End Get
    End Property

    Protected Sub New(ByVal type As TreeType, ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection, ByVal keywordLocation As Location, ByVal name As SimpleName, ByVal parameters As ParameterCollection, ByVal asLocation As Location, ByVal resultTypeAttributes As AttributeCollection, ByVal resultType As TypeName, ByVal implementsList As NameCollection, ByVal handlesList As NameCollection, ByVal statements As StatementCollection, ByVal endStatement As EndBlockDeclaration, ByVal span As Span, ByVal comments As CommentCollection)
        MyBase.New(type, attributes, modifiers, keywordLocation, name, parameters, asLocation, resultTypeAttributes, resultType, span, comments)

        Debug.Assert(type = TreeType.SubDeclaration OrElse type = TreeType.FunctionDeclaration OrElse type = TreeType.ConstructorDeclaration)
        SetParent(statements)
        SetParent(endStatement)
        SetParent(handlesList)
        SetParent(implementsList)

        _ImplementsList = implementsList
        _HandlesList = handlesList
        _Statements = statements
        _EndStatement = endStatement
    End Sub

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        MyBase.GetChildTrees(childList)
        AddChild(childList, ImplementsList)
        AddChild(childList, HandlesList)
        AddChild(childList, Statements)
        AddChild(childList, EndStatement)
    End Sub
End Class