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
''' The root class of all trees.
''' </summary>
Public MustInherit Class Tree
    Private ReadOnly _Type As TreeType
    Private ReadOnly _Span As Span
    Private _Parent As Tree
    Private _Children As TreeCollection

    ''' <summary>
    ''' The type of the tree.
    ''' </summary>
    Public ReadOnly Property Type() As TreeType
        Get
            Return _Type
        End Get
    End Property

    ''' <summary>
    ''' The location of the tree.
    ''' </summary>
    ''' <remarks>
    ''' The span ends at the first character beyond the tree
    ''' </remarks>
    Public ReadOnly Property Span() As Span
        Get
            Return _Span
        End Get
    End Property

    ''' <summary>
    ''' The parent of the tree. Nothing if the root tree.
    ''' </summary>
    Public ReadOnly Property Parent() As Tree
        Get
            Return _Parent
        End Get
    End Property

    ''' <summary>
    ''' The children of the tree.
    ''' </summary>
    Public ReadOnly Property Children() As TreeCollection
        Get
            If _Children Is Nothing Then
                Dim ChildList As ArrayList = New ArrayList

                GetChildTrees(ChildList)
                _Children = New TreeCollection(ChildList)
            End If

            Return _Children
        End Get
    End Property

    ''' <summary>
    ''' Whether the tree is 'bad'.
    ''' </summary>
    Public Overridable ReadOnly Property IsBad() As Boolean
        Get
            Return False
        End Get
    End Property

    Protected Sub New(ByVal type As TreeType, ByVal span As Span)
        Debug.Assert(type >= TreeType.SyntaxError AndAlso type <= TreeType.File)
        _Type = type
        _Span = span
    End Sub

    Protected Sub SetParent(ByVal child As Tree)
        If Not child Is Nothing Then
            child._Parent = Me
        End If
    End Sub

    Protected Sub SetParents(ByVal children() As Tree)
        If Not children Is Nothing Then
            For Each Child As Tree In children
                SetParent(Child)
            Next
        End If
    End Sub

    Protected Shared Sub AddChild(ByVal childList As ArrayList, ByVal child As Tree)
        If Not child Is Nothing Then
            childList.Add(child)
        End If
    End Sub

    Protected Shared Sub AddChildren(ByVal childList As ArrayList, ByVal children() As Tree)
        If Not children Is Nothing Then
            childList.AddRange(children)
        End If
    End Sub

    Protected Overridable Sub GetChildTrees(ByVal childList As ArrayList)
        ' By default, trees have no children
    End Sub
End Class