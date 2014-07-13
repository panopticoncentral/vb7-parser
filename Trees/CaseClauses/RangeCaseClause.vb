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
''' A parse tree for a case clause that compares against a range of values.
''' </summary>
Public NotInheritable Class RangeCaseClause
    Inherits CaseClause

    Private ReadOnly _LowerExpression As Expression
    Private ReadOnly _ToLocation As Location
    Private ReadOnly _UpperExpression As Expression

    ''' <summary>
    ''' The lower value of the range.
    ''' </summary>
    Public ReadOnly Property LowerExpression() As Expression
        Get
            Return _LowerExpression
        End Get
    End Property

    ''' <summary>
    ''' The location of the 'To'.
    ''' </summary>
    Public ReadOnly Property ToLocation() As Location
        Get
            Return _ToLocation
        End Get
    End Property

    ''' <summary>
    ''' The upper value of the range.
    ''' </summary>
    Public ReadOnly Property UpperExpression() As Expression
        Get
            Return _UpperExpression
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new range case clause parse tree.
    ''' </summary>
    ''' <param name="lowerExpression">The lower value of the range.</param>
    ''' <param name="toLocation">The location of the 'To'.</param>
    ''' <param name="upperExpression">The upper value of the range.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal lowerExpression As Expression, ByVal toLocation As Location, ByVal upperExpression As Expression, ByVal span As Span)
        MyBase.New(TreeType.RangeCaseClause, span)

        If lowerExpression Is Nothing Then
            Throw New ArgumentNullException("lowerExpression")
        End If

        SetParent(lowerExpression)
        SetParent(upperExpression)

        _LowerExpression = lowerExpression
        _ToLocation = toLocation
        _UpperExpression = upperExpression
    End Sub

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        AddChild(childList, LowerExpression)
        AddChild(childList, UpperExpression)
    End Sub
End Class