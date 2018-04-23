using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo.DB;
using NUnit.Framework;
using System.Collections;

namespace PersistentMetadata {
    class Program {
        static void Main(string[] args) {
            new PersistentMetadataExample().Execute();
        }
    }	

    public class PersistentMetadataExample {
        public void Execute() {
            InitDataLayers();
            CreateMetadata();
            CreateDatabaseAndDefaultData();
            LoadData();
        }

        IDataLayer metadataStorage, dataStorage;

        void InitDataLayers() {
            string metadataFileName = "Metadata.xml";
            string dataFileName = "Data.mdb";

            if(File.Exists(metadataFileName)) File.Delete(metadataFileName);
            if(File.Exists(dataFileName)) File.Delete(dataFileName);

            metadataStorage = XpoDefault.GetDataLayer(InMemoryDataStore.GetConnectionString(metadataFileName), AutoCreateOption.DatabaseAndSchema);
            dataStorage = XpoDefault.GetDataLayer(AccessConnectionProvider.GetConnectionString(dataFileName), AutoCreateOption.DatabaseAndSchema);
        }

        void CreateMetadata() {
            using(UnitOfWork uof = new UnitOfWork(metadataStorage)) {
                PersistentClassInfo classCustomer = new PersistentClassInfo(uof);
                classCustomer.Name = "Customer";

                PersistentCoreTypeMemberInfo propFullName = new PersistentCoreTypeMemberInfo(uof);
                propFullName.Name = "FullName";
                propFullName.TypeName = typeof(string).FullName;
                classCustomer.OwnMembers.Add(propFullName);

                PersistentCollectionMemberInfo propOrders = new PersistentCollectionMemberInfo(uof);
                propOrders.Name = "Orders";
                classCustomer.OwnMembers.Add(propOrders);

                PersistentAssociationAttribute attrCustomerOrders = new PersistentAssociationAttribute(uof);
                attrCustomerOrders.ElementTypeName = "Order";
                propOrders.TypeAttributes.Add(attrCustomerOrders);

                PersistentClassInfo classOrder = new PersistentClassInfo(uof);
                classOrder.Name = "Order";

                PersistentCoreTypeMemberInfo propOrderDate = new PersistentCoreTypeMemberInfo(uof);
                propOrderDate.Name = "OrderDate";
                propOrderDate.TypeName = typeof(DateTime).FullName;
                classOrder.OwnMembers.Add(propOrderDate);

                PersistentReferenceMemberInfo propCustomer = new PersistentReferenceMemberInfo(uof);
                propCustomer.Name = "Customer";
                propCustomer.ReferenceType = classCustomer;
                classOrder.OwnMembers.Add(propCustomer);

                PersistentAssociationAttribute attrOrdersCustomer = new PersistentAssociationAttribute(uof);
                propCustomer.TypeAttributes.Add(attrOrdersCustomer);

                uof.CommitChanges();
            }
        }

        void InitDataDictionary(UnitOfWork dataSession) {
            using(UnitOfWork metadataSession = new UnitOfWork(metadataStorage)) {
                XPCollection<PersistentClassInfo> classes = new XPCollection<PersistentClassInfo>(metadataSession);
                Assert.AreEqual(2, classes.Count);
                PersistentClassInfo.FillDictionary(dataSession.Dictionary, classes);
            }
        }

        readonly int OrderCount = 1;
        readonly DateTime OrderDate = DateTime.Today;

        void CreateDatabaseAndDefaultData() {
            using(UnitOfWork dataSession = new UnitOfWork(dataStorage)) {
                InitDataDictionary(dataSession);
                dataSession.UpdateSchema();

                XPClassInfo classCustomer = dataSession.GetClassInfo("", "Customer");
                XPBaseObject customer = (XPBaseObject)classCustomer.CreateNewObject(dataSession);
                customer.SetMemberValue("FullName", "John Doe");

                for(int i = 0; i < OrderCount; i++) {
                    XPClassInfo classOrder = dataSession.GetClassInfo("", "Order");
                    XPBaseObject order = (XPBaseObject)classOrder.CreateNewObject(dataSession);
                    order.SetMemberValue("OrderDate", OrderDate);
                    order.SetMemberValue("Customer", customer);
                }
                dataSession.CommitChanges();
            }
        }

        void LoadData() {
            using(UnitOfWork dataSession = new UnitOfWork(dataStorage)) {
                InitDataDictionary(dataSession);

                XPClassInfo classCustomer = dataSession.GetClassInfo("", "Customer");
                XPBaseObject customer = (XPBaseObject)dataSession.FindObject(classCustomer, null);
                Assert.AreEqual("John Doe", customer.GetMemberValue("FullName"));

                IList orders = (IList)customer.GetMemberValue("Orders");
                Assert.AreEqual(OrderCount, orders.Count);
                Assert.AreEqual(OrderDate, ((XPBaseObject)orders[0]).GetMemberValue("OrderDate"));
            }
        }
    }

	[TestFixture]
	public class PersistentMetadataTest {
		[Test]
		public void Test() {
			new PersistentMetadataExample().Execute();
		}
	}
}