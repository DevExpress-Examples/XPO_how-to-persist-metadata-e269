Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.IO
Imports DevExpress.Xpo
Imports DevExpress.Xpo.Metadata
Imports DevExpress.Xpo.DB
Imports NUnit.Framework
Imports System.Collections

Namespace PersistentMetadata
    Friend Class Program
        Shared Sub Main(ByVal args As String())
            CType(New PersistentMetadataExample(), PersistentMetadataExample).Execute()
        End Sub
    End Class

    Public Class PersistentMetadataExample
        Public Sub Execute()
            InitDataLayers()
            CreateMetadata()
            CreateDatabaseAndDefaultData()
            LoadData()
        End Sub

        Private metadataStorage, dataStorage As IDataLayer

        Private Sub InitDataLayers()
            Dim metadataFileName As String = "Metadata.xml"
            Dim dataFileName As String = "Data.mdb"

            If File.Exists(metadataFileName) Then
                File.Delete(metadataFileName)
            End If
            If File.Exists(dataFileName) Then
                File.Delete(dataFileName)
            End If

            metadataStorage = XpoDefault.GetDataLayer(InMemoryDataStore.GetConnectionString(metadataFileName), AutoCreateOption.DatabaseAndSchema)
            dataStorage = XpoDefault.GetDataLayer(AccessConnectionProvider.GetConnectionString(dataFileName), AutoCreateOption.DatabaseAndSchema)
        End Sub

        Private Sub CreateMetadata()
            Using uof As UnitOfWork = New UnitOfWork(metadataStorage)
                Dim classCustomer As PersistentClassInfo = New PersistentClassInfo(uof)
                classCustomer.Name = "Customer"

                Dim propFullName As PersistentCoreTypeMemberInfo = New PersistentCoreTypeMemberInfo(uof)
                propFullName.Name = "FullName"
                propFullName.TypeName = GetType(String).FullName
                classCustomer.OwnMembers.Add(propFullName)

                Dim propOrders As PersistentCollectionMemberInfo = New PersistentCollectionMemberInfo(uof)
                propOrders.Name = "Orders"
                classCustomer.OwnMembers.Add(propOrders)

                Dim attrCustomerOrders As PersistentAssociationAttribute = New PersistentAssociationAttribute(uof)
                attrCustomerOrders.ElementTypeName = "Order"
                propOrders.TypeAttributes.Add(attrCustomerOrders)

                Dim classOrder As PersistentClassInfo = New PersistentClassInfo(uof)
                classOrder.Name = "Order"

                Dim propOrderDate As PersistentCoreTypeMemberInfo = New PersistentCoreTypeMemberInfo(uof)
                propOrderDate.Name = "OrderDate"
                propOrderDate.TypeName = GetType(DateTime).FullName
                classOrder.OwnMembers.Add(propOrderDate)

                Dim propCustomer As PersistentReferenceMemberInfo = New PersistentReferenceMemberInfo(uof)
                propCustomer.Name = "Customer"
                propCustomer.ReferenceType = classCustomer
                classOrder.OwnMembers.Add(propCustomer)

                Dim attrOrdersCustomer As PersistentAssociationAttribute = New PersistentAssociationAttribute(uof)
                propCustomer.TypeAttributes.Add(attrOrdersCustomer)

                uof.CommitChanges()
            End Using
        End Sub

        Private Sub InitDataDictionary(ByVal dataSession As UnitOfWork)
            Using metadataSession As UnitOfWork = New UnitOfWork(metadataStorage)
                Dim classes As XPCollection(Of PersistentClassInfo) = New XPCollection(Of PersistentClassInfo)(metadataSession)
                Assert.AreEqual(2, classes.Count)
                PersistentClassInfo.FillDictionary(dataSession.Dictionary, classes)
            End Using
        End Sub

        Private ReadOnly OrderCount As Integer = 1
        Private ReadOnly OrderDate As DateTime = DateTime.Today

        Private Sub CreateDatabaseAndDefaultData()
            Using dataSession As UnitOfWork = New UnitOfWork(dataStorage)
                InitDataDictionary(dataSession)
                dataSession.UpdateSchema()

                Dim classCustomer As XPClassInfo = dataSession.GetClassInfo("", "Customer")
                Dim customer As XPBaseObject = CType(classCustomer.CreateNewObject(dataSession), XPBaseObject)
                customer.SetMemberValue("FullName", "John Doe")

                For i As Integer = 0 To OrderCount - 1
                    Dim classOrder As XPClassInfo = dataSession.GetClassInfo("", "Order")
                    Dim order As XPBaseObject = CType(classOrder.CreateNewObject(dataSession), XPBaseObject)
                    order.SetMemberValue("OrderDate", OrderDate)
                    order.SetMemberValue("Customer", customer)
                Next i
                dataSession.CommitChanges()
            End Using
        End Sub

        Private Sub LoadData()
            Using dataSession As UnitOfWork = New UnitOfWork(dataStorage)
                InitDataDictionary(dataSession)

                Dim classCustomer As XPClassInfo = dataSession.GetClassInfo("", "Customer")
                Dim customer As XPBaseObject = CType(dataSession.FindObject(classCustomer, Nothing), XPBaseObject)
                Assert.AreEqual("John Doe", customer.GetMemberValue("FullName"))

                Dim orders As IList = CType(customer.GetMemberValue("Orders"), IList)
                Assert.AreEqual(OrderCount, orders.Count)
                Assert.AreEqual(OrderDate, (CType(orders(0), XPBaseObject)).GetMemberValue("OrderDate"))
            End Using
        End Sub
    End Class

    <TestFixture()> _
    Public Class PersistentMetadataTest
        <Test()> _
        Public Sub Test()
            CType(New PersistentMetadataExample(), PersistentMetadataExample).Execute()
        End Sub
    End Class
End Namespace