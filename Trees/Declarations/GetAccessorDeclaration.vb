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
''' A parse tree for a Get property accessor.
''' </summary>
Public NotInheritable Class GetAccessorDeclaration
    Inherits ModifiedDeclaration

    Private ReadOnly _GetLocation As Location
    Private ReadOnly _Statements As StatementCollection
    Private ReadOnly _EndStatement As EndBlockDeclaration

    ''' <summary>
    ''' The location of the 'Get'.
    ''' </summary>
    Public ReadOnly Property GetLocation() As Location
        Get
            Return _GetLocation
        End Get
    End Property

    ''' <summary>
    ''' The statements in the accessor.
    ''' </summary>
    Public ReadOnly Property Statements() As StatementCollection
        Get
            Return _Statements
        End Get
    End Property

    ''' <summary>
    ''' The End declaration for the accessor.
    ''' </summary>
    Public ReadOnly Property EndStatement() As EndBlockDeclaration
        Get
            Return _EndStatement
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new parse tree for a Get property accessor.
    ''' </summary>
    ''' <param name="attributes">The attributes for the parse tree.</param>
    ''' <param name="modifiers">The modifiers for the parse tree.</param>
    ''' <param name="getLocation">The location of the 'Get'.</param>
    ''' <param name="statements">The statements in the declaration.</param>
    ''' <param name="endStatement">The end block declaration, if any.</param>
    ''' <param name="span">The location of the parse tree.</param>
    ''' <param name="comments">The comments for the parse tree.</param>
    Public Sub New(ByVal attributes As AttributeCollection, ByVal modifiers As ModifierCollection, ByVal getLocation As Location, ByVal statements As StatementCollection, ByVal endStatement As EndBlockDeclaration, ByVal span As Span, ByVal comments As CommentCollection)
        MyBase.New(TreeType.GetDeclaration, attributes, modifiers, span, comments)

        SetParent(statements)
        SetParent(endStatement)

        _GetLocation = getLocation
        _Statements = statements
        _EndStatement = endStatement
    End Sub

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        MyBase.GetChildTrees(childList)

        AddChild(childList, Statements)
        AddChild(childList, EndStatement)
    End Sub
End Class